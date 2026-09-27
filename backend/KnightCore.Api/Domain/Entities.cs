using Microsoft.AspNetCore.Identity;

namespace KnightCore.Domain;

public sealed class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
}
public sealed class Segment
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
    public int SortOrder { get; set; }
}
public sealed class SoftwarePackage
{
    public string Id { get; set; } = "";
    public string SegmentId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Accent { get; set; } = "blue";
    public bool Published { get; set; } = true;
    public List<PackageVersion> Versions { get; set; } = [];
}
public sealed class PackageVersion
{
    public string Id { get; set; } = "";
    public string PackageId { get; set; } = "";
    public int Version { get; set; }
    public decimal BasePrice { get; set; }
    public string IncludedJson { get; set; } = "[]";
    public string Delivery { get; set; } = "2–3 weeks";
    public bool Current { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public List<FeatureOffering> Features { get; set; } = [];
}
public sealed class FeatureOffering
{
    public string Id { get; set; } = "";
    public string PackageVersionId { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string PriceMode { get; set; } = "Fixed";
    public decimal UnitPrice { get; set; }
    public int MaxQuantity { get; set; } = 1;
    public string RequiresJson { get; set; } = "[]";
}
public sealed class Configuration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CustomerId { get; set; } = "";
    public string SelectionJson { get; set; } = "";
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
public sealed class BillingProfile
{
    public string CustomerId { get; set; } = "";
    public string DetailsJson { get; set; } = "";
}
public sealed class CustomRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CustomerId { get; set; } = "";
    public string PackageName { get; set; } = "";
    public string SelectionJson { get; set; } = "";
    public string PriceSnapshotJson { get; set; } = "";
    public string BillingJson { get; set; } = "";
    public string Requirements { get; set; } = "";
    public string Status { get; set; } = "Submitted";
    public Guid? QuoteId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
public sealed class Quote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CustomerId { get; set; } = "";
    public int Version { get; set; } = 1;
    public string Status { get; set; } = "Offered";
    public string SnapshotJson { get; set; } = "";
    public DateTime ExpiresUtc { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
}
public sealed class QuoteRevision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuoteId { get; set; }
    public int Version { get; set; }
    public string SnapshotJson { get; set; } = "";
    public DateTime ExpiresUtc { get; set; }
}
public sealed class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CustomerId { get; set; } = "";
    public Guid QuoteId { get; set; }
    public int QuoteVersion { get; set; }
    public string Number { get; set; } = "";
    public string SnapshotJson { get; set; } = "";
    public string Status { get; set; } = "Confirmed";
    public string? DeliveryUrl { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public List<OrderHistory> History { get; set; } = [];
}
public sealed class OrderHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public string Status { get; set; } = "";
    public string Note { get; set; } = "";
    public string ActorId { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
public sealed class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public string Number { get; set; } = "";
    public string SnapshotJson { get; set; } = "";
    public string PdfStatus { get; set; } = "Pending";
    public string? ObjectKey { get; set; }
    public string? Sha256 { get; set; }
    public DateTime IssuedUtc { get; set; } = DateTime.UtcNow;
    public DateTime DueUtc { get; set; } = DateTime.UtcNow.AddDays(7);
}
public sealed class InvoiceSeries
{
    public int Year { get; set; }
    public long NextNumber { get; set; } = 1;
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
}
public sealed class IdempotencyRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CustomerId { get; set; } = "";
    public string Key { get; set; } = "";
    public string RequestHash { get; set; } = "";
    public Guid OrderId { get; set; }
}
public sealed class BackgroundJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Kind { get; set; } = "";
    public string DeduplicationKey { get; set; } = "";
    public string PayloadJson { get; set; } = "";
    public string State { get; set; } = "Pending";
    public int Attempts { get; set; }
    public DateTime AvailableUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LeaseUntilUtc { get; set; }
    public Guid? LeaseToken { get; set; }
    public string? LastError { get; set; }
}
public sealed class AuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ActorId { get; set; } = "";
    public string Action { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string Details { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
