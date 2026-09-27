using KnightCore.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KnightCore.Application;

public sealed class PriceCalculator(AppDbContext db, IEnumerable<IPricingStrategy> strategies) : IPriceCalculator
{
    public async Task<PriceResult> CalculateAsync(Selection selection, CancellationToken ct)
    {
        if(selection.Items is null || selection.Items.Count>30) throw new ApiException(422,"INVALID_SELECTION","Select up to 30 features.");
        var v=await db.PackageVersions.Include(x=>x.Features).AsNoTracking().SingleOrDefaultAsync(x=>x.Id==selection.PackageVersionId,ct)
            ?? throw new ApiException(404,"PACKAGE_NOT_FOUND","This package version is unavailable.");
        var p=await db.Packages.AsNoTracking().SingleAsync(x=>x.Id==v.PackageId,ct);
        if(!p.Published || !v.Current) throw new ApiException(409,"CATALOG_CHANGED","This package was updated. Refresh your selection before requesting a quote.");
        if(selection.Items.Select(x=>x.FeatureId).Distinct().Count()!=selection.Items.Count)
            throw new ApiException(422,"DUPLICATE_FEATURE","A feature can only appear once in a configuration.");
        var selected=new List<(KnightCore.Domain.FeatureOffering Feature,int Quantity)>();
        foreach(var item in selection.Items)
        {
            var f=v.Features.SingleOrDefault(x=>x.Id==item.FeatureId) ?? throw new ApiException(422,"INVALID_FEATURE","A selected feature does not belong to this package.");
            if(item.Quantity<1 || item.Quantity>f.MaxQuantity) throw new ApiException(422,"INVALID_QUANTITY",$"Choose 1–{f.MaxQuantity} for {f.Name}.");
            selected.Add((f,item.Quantity));
        }
        var codes=selected.Select(x=>x.Feature.Code).ToHashSet();
        foreach(var (f,q) in selected)
            foreach(var required in Json.Read<List<string>>(f.RequiresJson))
                if(!codes.Contains(required))
                    throw new ApiException(422,"DEPENDENCY_REQUIRED",$"{f.Name} also requires {v.Features.Single(x=>x.Code==required).Name}.");
        var lines=new List<LineItem>{new("base",p.Name,"Base package with included features",1,v.BasePrice,v.BasePrice)};
        foreach(var (f,q) in selected)
        {
            var strategy=strategies.SingleOrDefault(x=>x.Mode==f.PriceMode) ?? throw new ApiException(422,"QUOTE_REQUIRED","This feature requires a custom quote.");
            lines.Add(new(f.Code,f.Name,f.Description,q,f.UnitPrice,strategy.Calculate(f.UnitPrice,q)));
        }
        return new(v.Id,p.Name,Json.Read<List<string>>(v.IncludedJson),lines,lines.Sum(x=>x.Amount),v.Delivery);
    }
}

