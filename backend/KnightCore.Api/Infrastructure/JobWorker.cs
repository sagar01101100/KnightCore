using System.Security.Cryptography;
using KnightCore.Application;
using Microsoft.EntityFrameworkCore;

namespace KnightCore.Infrastructure;
public sealed class JobWorker(IServiceScopeFactory scopes,ILogger<JobWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            try{if(!await RunOneAsync(stoppingToken))await Task.Delay(1200,stoppingToken);}
            catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested){break;}
            catch(Exception e){logger.LogError("Background processing failed: {ErrorType}",e.GetType().Name);await Task.Delay(2000,stoppingToken);}
        }
    }
    private async Task<bool> RunOneAsync(CancellationToken ct)
    {
        using var scope=scopes.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now=DateTime.UtcNow;
        var id=await db.Jobs.AsNoTracking().Where(x=>(x.State=="Pending"||x.State=="RetryScheduled"||x.State=="Processing"&&x.LeaseUntilUtc<now)&&x.AvailableUtc<=now).OrderBy(x=>x.AvailableUtc).Select(x=>(Guid?)x.Id).FirstOrDefaultAsync(ct);
        if(id is null)return false;
        var token=Guid.NewGuid();
        var claimed=await db.Jobs.Where(x=>x.Id==id&&(x.State=="Pending"||x.State=="RetryScheduled"||x.State=="Processing"&&x.LeaseUntilUtc<now))
            .ExecuteUpdateAsync(s=>s.SetProperty(x=>x.State,"Processing").SetProperty(x=>x.LeaseToken,token).SetProperty(x=>x.LeaseUntilUtc,now.AddMinutes(2)).SetProperty(x=>x.Attempts,x=>x.Attempts+1),ct);
        if(claimed!=1)return true;
        var job=await db.Jobs.SingleAsync(x=>x.Id==id&&x.LeaseToken==token,ct);
        try
        {
            using var bounded=CancellationTokenSource.CreateLinkedTokenSource(ct);bounded.CancelAfter(TimeSpan.FromSeconds(75));var workToken=bounded.Token;
            if(job.Kind=="InvoicePdf")
            {
                var invoiceId=Json.Read<Guid>(job.PayloadJson);
                var invoice=await db.Invoices.SingleAsync(x=>x.Id==invoiceId,workToken);
                var snapshot=Json.Read<CommercialSnapshot>(invoice.SnapshotJson);
                var key=invoice.Id.ToString("N")+"/v1.pdf";
                var storage=scope.ServiceProvider.GetRequiredService<IInvoiceStorage>();
                await storage.SaveAsync(key,scope.ServiceProvider.GetRequiredService<IInvoiceRenderer>().Render(invoice,snapshot),workToken);
                var bytes=await storage.ReadAsync(key,workToken);
                invoice.ObjectKey=key;invoice.Sha256=Convert.ToHexString(SHA256.HashData(bytes));invoice.PdfStatus="Ready";
                var customerId=await db.Orders.Where(x=>x.Id==invoice.OrderId).Select(x=>x.CustomerId).SingleAsync(workToken);
                var email=await db.Users.Where(x=>x.Id==customerId).Select(x=>x.Email).SingleAsync(workToken);
                var dedup="invoice-email:"+invoice.Id;
                if(!await db.Jobs.AnyAsync(x=>x.DeduplicationKey==dedup,workToken))
                    db.Jobs.Add(new(){Kind="Email",DeduplicationKey=dedup,PayloadJson=Json.Write(new EmailMessage(email!,"Your KnightCore order is confirmed","<p>Your order is confirmed and its invoice is ready. Sign in to your KnightCore dashboard to download it and track your project.</p>"))});
            }
            else if(job.Kind=="Email")await scope.ServiceProvider.GetRequiredService<IEmailSender>().SendAsync(Json.Read<EmailMessage>(job.PayloadJson),workToken);
            else throw new InvalidOperationException("Unknown job type.");
            job.State="Succeeded";job.LeaseUntilUtc=null;
            await db.SaveChangesAsync(ct);
        }
        catch(Exception e) when(!ct.IsCancellationRequested)
        {
            db.ChangeTracker.Clear();
            var current=await db.Jobs.SingleOrDefaultAsync(x=>x.Id==id&&x.LeaseToken==token,ct);
            if(current is null)return true;
            current.LastError=e.GetType().Name;
            current.State=current.Attempts>=5?"FailedNeedsAttention":"RetryScheduled";
            current.AvailableUtc=DateTime.UtcNow.AddSeconds(Math.Pow(2,current.Attempts)*3+Random.Shared.Next(3));
            current.LeaseUntilUtc=null;
            if(current.State=="FailedNeedsAttention"&&current.Kind=="InvoicePdf")
            {
                var invoiceId=Json.Read<Guid>(current.PayloadJson);
                var invoice=await db.Invoices.SingleAsync(x=>x.Id==invoiceId,ct);invoice.PdfStatus="FailedNeedsAttention";
            }
            await db.SaveChangesAsync(ct);
            logger.LogWarning("Job {JobId} requires retry or review ({ErrorType})",id,e.GetType().Name);
        }
        return true;
    }
}

