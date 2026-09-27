using KnightCore.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KnightCore.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Segment> Segments => Set<Segment>();
    public DbSet<SoftwarePackage> Packages => Set<SoftwarePackage>();
    public DbSet<PackageVersion> PackageVersions => Set<PackageVersion>();
    public DbSet<FeatureOffering> Features => Set<FeatureOffering>();
    public DbSet<Configuration> Configurations => Set<Configuration>();
    public DbSet<BillingProfile> BillingProfiles => Set<BillingProfile>();
    public DbSet<CustomRequest> CustomRequests => Set<CustomRequest>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteRevision> QuoteRevisions => Set<QuoteRevision>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderHistory> OrderHistory => Set<OrderHistory>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceSeries> InvoiceSeries => Set<InvoiceSeries>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<BackgroundJob> Jobs => Set<BackgroundJob>();
    public DbSet<AuditEntry> Audit => Set<AuditEntry>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Segment>().Property(x=>x.Id).HasMaxLength(80);
        b.Entity<SoftwarePackage>().Property(x=>x.Id).HasMaxLength(100);
        b.Entity<SoftwarePackage>().HasOne<Segment>().WithMany().HasForeignKey(x=>x.SegmentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SoftwarePackage>().HasMany(x=>x.Versions).WithOne().HasForeignKey(x=>x.PackageId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<PackageVersion>().HasIndex(x=>new{x.PackageId,x.Version}).IsUnique();
        b.Entity<PackageVersion>().HasMany(x=>x.Features).WithOne().HasForeignKey(x=>x.PackageVersionId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<FeatureOffering>().HasIndex(x=>new{x.PackageVersionId,x.Code}).IsUnique();
        b.Entity<BillingProfile>().HasKey(x=>x.CustomerId);
        b.Entity<Quote>().Property(x=>x.ConcurrencyToken).IsConcurrencyToken();
        b.Entity<QuoteRevision>().HasIndex(x=>new{x.QuoteId,x.Version}).IsUnique();
        b.Entity<QuoteRevision>().HasOne<Quote>().WithMany().HasForeignKey(x=>x.QuoteId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Order>().Property(x=>x.ConcurrencyToken).IsConcurrencyToken();
        b.Entity<Order>().HasIndex(x=>x.QuoteId).IsUnique();
        b.Entity<Order>().HasIndex(x=>x.Number).IsUnique();
        b.Entity<Order>().HasIndex(x=>new{x.CustomerId,x.CreatedUtc});
        b.Entity<Order>().HasOne<Quote>().WithMany().HasForeignKey(x=>x.QuoteId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Order>().HasMany(x=>x.History).WithOne().HasForeignKey(x=>x.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Invoice>().HasIndex(x=>x.OrderId).IsUnique();
        b.Entity<Invoice>().HasIndex(x=>x.Number).IsUnique();
        b.Entity<Invoice>().HasOne<Order>().WithOne().HasForeignKey<Invoice>(x=>x.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<InvoiceSeries>().HasKey(x=>x.Year);
        b.Entity<InvoiceSeries>().Property(x=>x.ConcurrencyToken).IsConcurrencyToken();
        b.Entity<IdempotencyRecord>().Property(x=>x.Key).HasMaxLength(128);
        b.Entity<IdempotencyRecord>().HasIndex(x=>new{x.CustomerId,x.Key}).IsUnique();
        b.Entity<BackgroundJob>().Property(x=>x.DeduplicationKey).HasMaxLength(200);
        b.Entity<BackgroundJob>().HasIndex(x=>x.DeduplicationKey).IsUnique();
        b.Entity<BackgroundJob>().Property(x=>x.LeaseToken).IsConcurrencyToken();
        b.Entity<BackgroundJob>().HasIndex(x=>new{x.State,x.AvailableUtc});
        b.Entity<Quote>().HasIndex(x=>new{x.CustomerId,x.CreatedUtc});
        b.Entity<CustomRequest>().HasIndex(x=>new{x.CustomerId,x.CreatedUtc});
        b.Entity<Configuration>().HasOne<AppUser>().WithMany().HasForeignKey(x=>x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<BillingProfile>().HasOne<AppUser>().WithMany().HasForeignKey(x=>x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Quote>().HasOne<AppUser>().WithMany().HasForeignKey(x=>x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Order>().HasOne<AppUser>().WithMany().HasForeignKey(x=>x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<CustomRequest>().HasOne<AppUser>().WithMany().HasForeignKey(x=>x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        foreach(var entity in b.Model.GetEntityTypes())
            foreach(var p in entity.GetProperties())
                if(p.ClrType==typeof(decimal)){p.SetPrecision(19);p.SetScale(2);}
    }
}
