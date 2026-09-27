using System.Security.Claims;
using KnightCore.Application;
using KnightCore.Domain;
using KnightCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KnightCore.Api;

[ApiController,Route("api/v1")]
public sealed class StoreController(AppDbContext db,IPriceCalculator pricing,QuoteService quotes,CheckoutService checkout,IOptions<BillingOptions> billing,IInvoiceStorage storage) : ControllerBase
{
    private string UserId=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [HttpGet("catalog")]
    public async Task<IActionResult> Catalog(CancellationToken ct)
    {
        var segments=await db.Segments.AsNoTracking().OrderBy(x=>x.SortOrder).ToListAsync(ct);
        var packages=await db.Packages.AsNoTracking().Include(x=>x.Versions.Where(v=>v.Current)).ThenInclude(x=>x.Features).Where(x=>x.Published).ToListAsync(ct);
        return Ok(new{segments,packages=packages.Select(CatalogView),evaluation=billing.Value.Evaluation,termsVersion=billing.Value.TermsVersion});
    }
    internal static object CatalogView(SoftwarePackage p)
    {
        var v=p.Versions.Single(x=>x.Current);
        return new{p.Id,p.SegmentId,p.Name,p.Summary,p.Accent,versionId=v.Id,version=v.Version,basePrice=v.BasePrice,included=Json.Read<string[]>(v.IncludedJson),v.Delivery,features=v.Features.Select(f=>new{f.Id,f.Code,f.Name,f.Description,f.PriceMode,f.UnitPrice,f.MaxQuantity,requires=Json.Read<string[]>(f.RequiresJson)})};
    }
    [HttpPost("estimates"),EnableRateLimiting("estimate")]
    public async Task<IActionResult> Estimate(Selection selection,CancellationToken ct)=>Ok(await pricing.CalculateAsync(selection,ct));
    [Authorize,HttpPost("configurations")]
    public async Task<IActionResult> SaveConfiguration(Selection selection,CancellationToken ct)
    {
        await pricing.CalculateAsync(selection,ct);
        var record=new Configuration{CustomerId=UserId,SelectionJson=Json.Write(selection)};
        db.Configurations.Add(record);await db.SaveChangesAsync(ct);
        return Ok(new{record.Id});
    }
    [Authorize,HttpGet("billing-profile")]
    public async Task<IActionResult> BillingProfile(CancellationToken ct)
    {
        var row=await db.BillingProfiles.AsNoTracking().SingleOrDefaultAsync(x=>x.CustomerId==UserId,ct);
        return Ok(new{billing=row is null?null:Json.Read<BillingDetails>(row.DetailsJson)});
    }
    [Authorize,HttpPost("quotes")]
    public async Task<IActionResult> CreateQuote(CreateQuoteRequest request,CancellationToken ct)=>Ok(QuoteView(await quotes.CreateAsync(UserId,request,ct)));
    [Authorize,HttpGet("quotes/{id:guid}")]
    public async Task<IActionResult> GetQuote(Guid id,CancellationToken ct)
    {
        var quote=await db.Quotes.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id&&x.CustomerId==UserId,ct)??throw new ApiException(404,"NOT_FOUND","Quote not found.");
        return Ok(QuoteView(quote));
    }
    internal static object QuoteView(Quote q)=>new{q.Id,q.Version,q.Status,q.ExpiresUtc,q.CreatedUtc,snapshot=Json.Read<CommercialSnapshot>(q.SnapshotJson)};
    [Authorize,HttpPost("custom-requests")]
    public async Task<IActionResult> Custom(CustomRequestInput input,CancellationToken ct)
    {
        var price=await pricing.CalculateAsync(input.Selection,ct);
        var r=new CustomRequest{CustomerId=UserId,PackageName=price.PackageName,SelectionJson=Json.Write(input.Selection),PriceSnapshotJson=Json.Write(price),BillingJson=Json.Write(input.Billing),Requirements=input.Requirements.Trim()};
        db.CustomRequests.Add(r);await db.SaveChangesAsync(ct);
        return Ok(new{r.Id,r.Status});
    }
    [Authorize,HttpGet("custom-requests")]
    public async Task<IActionResult> Requests(CancellationToken ct)
    {
        var requests=await db.CustomRequests.AsNoTracking().Where(x=>x.CustomerId==UserId).OrderByDescending(x=>x.CreatedUtc).Take(100).ToListAsync(ct);
        return Ok(requests.Select(x=>new{x.Id,x.PackageName,x.Requirements,x.Status,x.QuoteId,x.CreatedUtc}));
    }
    [Authorize,HttpPost("orders"),EnableRateLimiting("checkout")]
    public async Task<IActionResult> Place(OrderInput input,CancellationToken ct)
    {
        var order=await checkout.PlaceAsync(UserId,input,Request.Headers["Idempotency-Key"].ToString(),ct);
        var invoice=await db.Invoices.AsNoTracking().SingleAsync(x=>x.OrderId==order.Id,ct);
        return StatusCode(201,new{orderId=order.Id,invoiceId=invoice.Id,orderNumber=order.Number,status=order.Status,invoicePdfStatus=invoice.PdfStatus});
    }
    [Authorize,HttpGet("orders")]
    public async Task<IActionResult> Orders([FromQuery]int page=1,CancellationToken ct=default)
    {
        page=Math.Clamp(page,1,10000);
        var query=db.Orders.AsNoTracking().Where(x=>x.CustomerId==UserId);
        var total=await query.CountAsync(ct);
        var rows=await query.OrderByDescending(x=>x.CreatedUtc).ThenBy(x=>x.Id).Skip((page-1)*20).Take(20).ToListAsync(ct);
        return Ok(new{items=rows.Select(x=>new{x.Id,x.Number,x.Status,x.CreatedUtc,snapshot=Json.Read<CommercialSnapshot>(x.SnapshotJson)}),total,page,pageSize=20});
    }
    [Authorize,HttpGet("orders/{id:guid}")]
    public async Task<IActionResult> Order(Guid id,CancellationToken ct)
    {
        var order=await db.Orders.AsNoTracking().Include(x=>x.History).SingleOrDefaultAsync(x=>x.Id==id&&(x.CustomerId==UserId||User.IsInRole("Admin")),ct)
            ??throw new ApiException(404,"NOT_FOUND","Order not found.");
        var invoice=await db.Invoices.AsNoTracking().SingleAsync(x=>x.OrderId==id,ct);
        return Ok(new{order.Id,order.Number,order.Status,order.CreatedUtc,order.ConcurrencyToken,order.DeliveryUrl,
            snapshot=Json.Read<CommercialSnapshot>(order.SnapshotJson),
            history=order.History.OrderBy(x=>x.CreatedUtc).Select(x=>new{x.Status,x.Note,x.CreatedUtc}),
            invoice=new{invoice.Id,invoice.Number,invoice.PdfStatus,invoice.IssuedUtc,invoice.DueUtc},
            allowedStatuses=User.IsInRole("Admin")?OrderLifecycle.Next(order.Status):[]});
    }
    [Authorize,HttpGet("invoices/{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id,CancellationToken ct)
    {
        var invoice=await db.Invoices.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id,ct);
        if(invoice is null||!await db.Orders.AnyAsync(x=>x.Id==invoice.OrderId&&(x.CustomerId==UserId||User.IsInRole("Admin")),ct))
            throw new ApiException(404,"NOT_FOUND","Invoice not found.");
        if(invoice.PdfStatus!="Ready"||invoice.ObjectKey is null)throw new ApiException(409,"INVOICE_PENDING","Your invoice is still being prepared.");
        Response.Headers.CacheControl="private, no-store";
        return File(await storage.ReadAsync(invoice.ObjectKey,ct),"application/pdf",invoice.Number+".pdf");
    }
}

