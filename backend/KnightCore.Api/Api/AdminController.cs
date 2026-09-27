using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using KnightCore.Application;
using KnightCore.Domain;
using KnightCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KnightCore.Api;

public sealed record FeatureEdit([Required,RegularExpression("^[a-z][a-z0-9-]{0,49}$")]string Code,[Required,StringLength(100)]string Name,[Required,StringLength(400)]string Description,[Range(0,10000000)]decimal UnitPrice,[Required] string PriceMode,[Range(1,100)]int MaxQuantity,List<string> Requires);
public sealed record PackageEdit(int Version,[Required,StringLength(100)]string Name,[Required,StringLength(400)]string Summary,[Range(0,10000000)]decimal BasePrice,[Required,StringLength(120)]string Delivery,[MinLength(1),MaxLength(20)]List<string> Included,[MaxLength(30)]List<FeatureEdit> Features);
[ApiController,Route("api/v1/admin"),Authorize(Roles="Admin")]
public sealed class AdminController(AppDbContext db,QuoteService quotes) : ControllerBase
{
    private string UserId=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken ct)=>Ok(new{orders=await db.Orders.CountAsync(ct),requests=await db.CustomRequests.CountAsync(x=>x.Status=="Submitted",ct),customers=await db.Users.CountAsync(ct),failedJobs=await db.Jobs.CountAsync(x=>x.State=="FailedNeedsAttention",ct)});
    [HttpGet("orders")]
    public async Task<IActionResult> Orders([FromQuery]int page=1,CancellationToken ct=default)
    {
        page=Math.Clamp(page,1,10000);
        var count=await db.Orders.CountAsync(ct);
        var rows=await (from o in db.Orders.AsNoTracking() join u in db.Users on o.CustomerId equals u.Id orderby o.CreatedUtc descending select new{order=o,email=u.Email,name=u.DisplayName}).Skip((page-1)*30).Take(30).ToListAsync(ct);
        return Ok(new{items=rows.Select(x=>new{x.order.Id,x.order.Number,x.order.Status,x.order.CreatedUtc,x.email,x.name,snapshot=Json.Read<CommercialSnapshot>(x.order.SnapshotJson)}),total=count,page,pageSize=30});
    }
    [HttpGet("custom-requests")]
    public async Task<IActionResult> Requests(CancellationToken ct)
    {
        var rows=await (from r in db.CustomRequests.AsNoTracking() join u in db.Users on r.CustomerId equals u.Id orderby r.CreatedUtc descending select new{request=r,u.Email,u.DisplayName}).Take(100).ToListAsync(ct);
        return Ok(rows.Select(x=>new{x.request.Id,x.request.PackageName,x.request.Requirements,x.request.Status,x.request.QuoteId,x.request.CreatedUtc,email=x.Email,customerName=x.DisplayName,price=Json.Read<PriceResult>(x.request.PriceSnapshotJson)}));
    }
    [HttpPost("custom-requests/{id:guid}/offer")]
    public async Task<IActionResult> Offer(Guid id,OfferInput input,CancellationToken ct)=>Ok(StoreController.QuoteView(await quotes.OfferAsync(id,input,UserId,ct)));
    [HttpPatch("orders/{id:guid}/status")]
    public async Task<IActionResult> Status(Guid id,StatusInput input,CancellationToken ct)
    {
        var order=await db.Orders.SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new ApiException(404,"NOT_FOUND","Order not found.");
        if(order.ConcurrencyToken!=input.ConcurrencyToken)throw new ApiException(412,"STALE_ORDER","The order changed. Refresh before updating.");
        if(!OrderLifecycle.CanMove(order.Status,input.Status))throw new ApiException(422,"INVALID_TRANSITION","This status transition is not allowed.");
        if(!string.IsNullOrWhiteSpace(input.DeliveryUrl)&&(!Uri.TryCreate(input.DeliveryUrl,UriKind.Absolute,out var uri)||uri.Scheme!="https"))throw new ApiException(422,"INVALID_DELIVERY_URL","Use an HTTPS delivery URL.");
        order.Status=input.Status;order.ConcurrencyToken=Guid.NewGuid();
        if(!string.IsNullOrWhiteSpace(input.DeliveryUrl))order.DeliveryUrl=input.DeliveryUrl;
        db.OrderHistory.Add(new(){OrderId=id,Status=input.Status,Note=input.Note.Trim(),ActorId=UserId});
        db.Audit.Add(new(){ActorId=UserId,Action="OrderStatusChanged",EntityId=id.ToString(),Details=input.Status});
        await db.SaveChangesAsync(ct);
        return Ok(new{order.Status,order.ConcurrencyToken});
    }
    [HttpPost("packages/{id}/publish")]
    public async Task<IActionResult> Publish(string id,PackageEdit input,CancellationToken ct)
    {
        var p=await db.Packages.Include(x=>x.Versions.Where(v=>v.Current)).SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new ApiException(404,"NOT_FOUND","Package not found.");
        var current=p.Versions.Single();
        if(current.Version!=input.Version)throw new ApiException(409,"CATALOG_CHANGED","The package was updated. Refresh it before publishing.");
        if(input.Included.Any(x=>string.IsNullOrWhiteSpace(x)||x.Length>160))throw new ApiException(422,"INVALID_FEATURE","Included feature descriptions must contain 1–160 characters.");
        if(input.Features.Select(x=>x.Code).Distinct().Count()!=input.Features.Count)throw new ApiException(422,"DUPLICATE_FEATURE","Feature codes must be unique.");
        var map=input.Features.ToDictionary(x=>x.Code);
        foreach(var f in input.Features)
        {
            if(f.PriceMode is not ("Fixed" or "PerUnit"))throw new ApiException(422,"INVALID_MODE","Choose Fixed or PerUnit pricing.");
            if(f.Requires.Any(x=>!map.ContainsKey(x)||x==f.Code))throw new ApiException(422,"INVALID_DEPENDENCY","Each dependency must be another feature in this package.");
            var visiting=new HashSet<string>();
            void Visit(string code){if(!visiting.Add(code))throw new ApiException(422,"CYCLIC_DEPENDENCY","Feature dependencies cannot form a cycle.");foreach(var r in map[code].Requires)Visit(r);visiting.Remove(code);}
            Visit(f.Code);
        }
        p.Name=input.Name.Trim();p.Summary=input.Summary.Trim();current.Current=false;
        var version=new PackageVersion{Id=Guid.NewGuid().ToString("N"),PackageId=p.Id,Version=current.Version+1,BasePrice=input.BasePrice,Delivery=input.Delivery,IncludedJson=Json.Write(input.Included)};
        version.Features=input.Features.Select(f=>new FeatureOffering{Id=Guid.NewGuid().ToString("N"),PackageVersionId=version.Id,Code=f.Code,Name=f.Name,Description=f.Description,PriceMode=f.PriceMode,UnitPrice=f.UnitPrice,MaxQuantity=f.PriceMode=="Fixed"?1:f.MaxQuantity,RequiresJson=Json.Write(f.Requires)}).ToList();
        db.PackageVersions.Add(version);
        db.Audit.Add(new(){ActorId=UserId,Action="PackagePublished",EntityId=p.Id,Details=$"Version {version.Version}"});
        await db.SaveChangesAsync(ct);
        return Ok(new{version=version.Version});
    }
    [HttpGet("jobs")]
    public async Task<IActionResult> Jobs(CancellationToken ct)=>Ok(await db.Jobs.AsNoTracking().Where(x=>x.State=="FailedNeedsAttention").Select(x=>new{x.Id,x.Kind,x.State,x.Attempts,x.LastError}).Take(100).ToListAsync(ct));
    [HttpPost("jobs/{id:guid}/retry")]
    public async Task<IActionResult> Retry(Guid id,CancellationToken ct)
    {
        var job=await db.Jobs.SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new ApiException(404,"NOT_FOUND","Job not found.");
        if(job.State!="FailedNeedsAttention")throw new ApiException(409,"JOB_NOT_FAILED","Only failed jobs can be retried.");
        job.State="Pending";job.Attempts=0;job.AvailableUtc=DateTime.UtcNow;job.LeaseToken=null;job.LeaseUntilUtc=null;
        if(job.Kind=="InvoicePdf"){var invoiceId=Json.Read<Guid>(job.PayloadJson);var invoice=await db.Invoices.SingleAsync(x=>x.Id==invoiceId,ct);invoice.PdfStatus="Pending";}
        db.Audit.Add(new(){ActorId=UserId,Action="JobRetried",EntityId=id.ToString()});await db.SaveChangesAsync(ct);
        return Ok(new{message="Retry scheduled."});
    }
}

