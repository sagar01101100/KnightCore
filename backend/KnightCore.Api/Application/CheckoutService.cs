using System.Data;
using System.Security.Cryptography;
using System.Text;
using KnightCore.Domain;
using KnightCore.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KnightCore.Application;

public sealed class CheckoutService(AppDbContext db)
{
    public async Task<Order> PlaceAsync(string userId,OrderInput input,string key,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(key)||key.Length>128)throw new ApiException(422,"REQUEST_KEY_REQUIRED","A valid checkout request key is required.");
        var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{input.QuoteId:D}|{input.QuoteVersion}|{input.AcceptedTermsVersion}")));
        for(var attempt=0;attempt<4;attempt++)
        {
            try
            {
                return await db.Database.CreateExecutionStrategy().ExecuteAsync(async()=>
                {
                    db.ChangeTracker.Clear();
                    var old=await db.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(x=>x.CustomerId==userId&&x.Key==key,ct);
                    if(old is not null)
                    {
                        if(old.RequestHash!=hash)throw new ApiException(409,"IDEMPOTENCY_CONFLICT","This request key was used for different checkout details.");
                        return await db.Orders.AsNoTracking().SingleAsync(x=>x.Id==old.OrderId&&x.CustomerId==userId,ct);
                    }
                    await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
                    var quote=await db.Quotes.SingleOrDefaultAsync(x=>x.Id==input.QuoteId&&x.CustomerId==userId,ct)
                        ??throw new ApiException(404,"QUOTE_NOT_FOUND","Quote not found.");
                    var existing=await db.Orders.AsNoTracking().SingleOrDefaultAsync(x=>x.QuoteId==quote.Id&&x.CustomerId==userId,ct);
                    if(existing is not null)
                    {
                        db.IdempotencyRecords.Add(new(){CustomerId=userId,Key=key,RequestHash=hash,OrderId=existing.Id});
                        await db.SaveChangesAsync(ct);
                        await transaction.CommitAsync(ct);
                        return existing;
                    }
                    if(quote.Status!="Offered"||quote.Version!=input.QuoteVersion)throw new ApiException(409,"QUOTE_CHANGED","The quote has changed. Review its current version.");
                    if(quote.ExpiresUtc<=DateTime.UtcNow)throw new ApiException(409,"QUOTE_EXPIRED","This quote has expired. Request a new quote.");
                    var snapshot=Json.Read<CommercialSnapshot>(quote.SnapshotJson);
                    if(input.AcceptedTermsVersion!=snapshot.TermsVersion)throw new ApiException(409,"TERMS_CHANGED","Please review the quoted terms before ordering.");
                    quote.Status="Accepted";quote.ConcurrencyToken=Guid.NewGuid();
                    var order=new Order{CustomerId=userId,QuoteId=quote.Id,QuoteVersion=quote.Version,SnapshotJson=quote.SnapshotJson};
                    order.Number=$"KC-{DateTime.UtcNow:yyMMdd}-{order.Id.ToString("N")[..8].ToUpperInvariant()}";
                    order.History.Add(new(){OrderId=order.Id,Status="Confirmed",Note="Scope and price accepted. Your project is confirmed.",ActorId=userId});
                    db.Orders.Add(order);
                    var year=DateTime.UtcNow.Year;
                    var series=await db.InvoiceSeries.SingleOrDefaultAsync(x=>x.Year==year,ct);
                    if(series is null){series=new(){Year=year};db.InvoiceSeries.Add(series);}
                    var number=series.NextNumber++;series.ConcurrencyToken=Guid.NewGuid();
                    var invoice=new Invoice{OrderId=order.Id,Number=$"KC-INV-{year}-{number:D6}",SnapshotJson=quote.SnapshotJson};
                    db.Invoices.Add(invoice);
                    db.Jobs.Add(new(){Kind="InvoicePdf",DeduplicationKey=$"invoice:{invoice.Id}:1",PayloadJson=Json.Write(invoice.Id)});
                    db.IdempotencyRecords.Add(new(){CustomerId=userId,Key=key,RequestHash=hash,OrderId=order.Id});
                    db.Audit.Add(new(){ActorId=userId,Action="OrderConfirmed",EntityId=order.Id.ToString(),Details=$"Quote {quote.Id} version {quote.Version}"});
                    await db.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                    return order;
                });
            }
            catch(DbUpdateException) when(attempt<3){db.ChangeTracker.Clear();await Task.Delay(40*(attempt+1),ct);}
        }
        throw new ApiException(409,"CHECKOUT_CONFLICT","Another checkout is being processed. Retry with the same request key.");
    }
}

