using KnightCore.Domain;
using KnightCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KnightCore.Application;

public sealed class QuoteService(AppDbContext db,IPriceCalculator pricing,IOptions<BillingOptions> options)
{
    public CommercialSnapshot Snapshot(PriceResult price,BillingDetails billing,string scope="",decimal customAmount=0,string? delivery=null)
    {
        var settings=options.Value;
        var lines=price.Lines.ToList();
        if(!string.IsNullOrWhiteSpace(scope)) lines.Add(new("custom","Custom development",scope,1,customAmount,customAmount));
        var subtotal=lines.Sum(x=>x.Amount);
        var tax=Math.Round(subtotal*settings.TaxRate,2,MidpointRounding.AwayFromZero);
        return new(price.PackageName,price.PackageVersionId,price.Included,lines,billing,
            new(settings.SellerName,settings.SellerAddress,settings.SellerEmail,settings.SellerTaxId),
            subtotal,settings.TaxRate,tax,subtotal+tax,"INR",delivery??price.Delivery,settings.TermsVersion,scope,settings.Evaluation);
    }
    public async Task<Quote> CreateAsync(string userId,CreateQuoteRequest request,CancellationToken ct)
    {
        var price=await pricing.CalculateAsync(request.Selection,ct);
        var quote=new Quote{CustomerId=userId,SnapshotJson=Json.Write(Snapshot(price,request.Billing)),ExpiresUtc=DateTime.UtcNow.AddMinutes(30)};
        db.Quotes.Add(quote);
        db.QuoteRevisions.Add(new(){QuoteId=quote.Id,Version=1,SnapshotJson=quote.SnapshotJson,ExpiresUtc=quote.ExpiresUtc});
        var billing=await db.BillingProfiles.FindAsync([userId],ct);
        if(billing is null)db.BillingProfiles.Add(new(){CustomerId=userId,DetailsJson=Json.Write(request.Billing)});
        else billing.DetailsJson=Json.Write(request.Billing);
        await db.SaveChangesAsync(ct);
        return quote;
    }
    public async Task<Quote> OfferAsync(Guid requestId,OfferInput offer,string adminId,CancellationToken ct)
    {
        var request=await db.CustomRequests.SingleOrDefaultAsync(x=>x.Id==requestId,ct)
            ??throw new ApiException(404,"NOT_FOUND","Request not found.");
        var price=Json.Read<PriceResult>(request.PriceSnapshotJson);
        var quote=request.QuoteId is {} qid?await db.Quotes.SingleAsync(x=>x.Id==qid,ct):null;
        if(quote?.Status=="Accepted")throw new ApiException(409,"QUOTE_ACCEPTED","This quote is already accepted.");
        if(quote is null){quote=new(){CustomerId=request.CustomerId};db.Quotes.Add(quote);request.QuoteId=quote.Id;}
        else {quote.Version++;quote.ConcurrencyToken=Guid.NewGuid();}
        quote.Status="Offered";
        quote.SnapshotJson=Json.Write(Snapshot(price,Json.Read<BillingDetails>(request.BillingJson),offer.Scope,offer.CustomAmount,offer.Delivery));
        quote.ExpiresUtc=DateTime.UtcNow.AddDays(offer.ValidDays);
        db.QuoteRevisions.Add(new(){QuoteId=quote.Id,Version=quote.Version,SnapshotJson=quote.SnapshotJson,ExpiresUtc=quote.ExpiresUtc});
        request.Status="Quoted";
        db.Audit.Add(new(){ActorId=adminId,Action="QuoteOffered",EntityId=quote.Id.ToString(),Details=$"Version {quote.Version}"});
        var email=await db.Users.Where(x=>x.Id==request.CustomerId).Select(x=>x.Email).SingleAsync(ct);
        db.Jobs.Add(new(){Kind="Email",DeduplicationKey=$"quote:{quote.Id}:{quote.Version}",PayloadJson=Json.Write(new EmailMessage(email!,"Your KnightCore quote is ready","<p>Your custom quote is ready. Sign in to KnightCore to review its scope, price and validity.</p>"))});
        await db.SaveChangesAsync(ct);
        return quote;
    }
}
