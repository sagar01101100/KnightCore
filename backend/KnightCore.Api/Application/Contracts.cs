using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace KnightCore.Application;

public static class Json
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    public static string Write<T>(T value) => JsonSerializer.Serialize(value, Options);
    public static T Read<T>(string value) => JsonSerializer.Deserialize<T>(value, Options)!;
}
public sealed record SelectionItem([Required] string FeatureId, [Range(1,100)] int Quantity);
public sealed record Selection([Required] string PackageVersionId, [MaxLength(30)] List<SelectionItem> Items);
public sealed record BillingDetails(
    [Required, StringLength(120,MinimumLength=2)] string BusinessName,
    [Required, StringLength(250,MinimumLength=5)] string Address,
    [Required, StringLength(80)] string City,
    [Required, StringLength(80)] string State,
    [Required, RegularExpression(@"^\d{6}$")] string PostalCode,
    [StringLength(30)] string? TaxId);
public sealed record CreateQuoteRequest(Selection Selection, BillingDetails Billing);
public sealed record CustomRequestInput(Selection Selection, BillingDetails Billing,
    [Required, StringLength(8000,MinimumLength=10)] string Requirements);
public sealed record OrderInput(Guid QuoteId, int QuoteVersion, [Required] string AcceptedTermsVersion);
public sealed record OfferInput(
    [Required,StringLength(8000,MinimumLength=10)] string Scope,
    [Range(0,10000000)] decimal CustomAmount,
    [Required,StringLength(120)] string Delivery,
    [Range(1,30)] int ValidDays);
public sealed record StatusInput([Required] string Status, Guid ConcurrencyToken,
    [StringLength(1000)] string Note, [StringLength(1000)] string? DeliveryUrl);
public sealed record LineItem(string Code, string Name, string Description, int Quantity, decimal UnitPrice, decimal Amount);
public sealed record PriceResult(string PackageVersionId, string PackageName, List<string> Included,
    List<LineItem> Lines, decimal Subtotal, string Delivery);
public sealed record SellerSnapshot(string Name, string Address, string Email, string? TaxId);
public sealed record CommercialSnapshot(string PackageName, string PackageVersionId, List<string> Included,
    List<LineItem> Lines, BillingDetails Billing, SellerSnapshot Seller, decimal Subtotal, decimal TaxRate,
    decimal TaxAmount, decimal Total, string Currency, string Delivery, string TermsVersion,
    string Scope, bool Evaluation);
public sealed class ApiException(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}
public sealed class BillingOptions
{
    public string SellerName { get; set; } = "KnightCore";
    public string SellerAddress { get; set; } = "Configure your registered business address";
    public string SellerEmail { get; set; } = "billing@example.invalid";
    public string? SellerTaxId { get; set; }
    public decimal TaxRate { get; set; }
    public bool Evaluation { get; set; } = true;
    public string TermsVersion { get; set; } = "knightcore-v1";
}
public interface IPriceCalculator { Task<PriceResult> CalculateAsync(Selection selection, CancellationToken ct); }
public interface IPricingStrategy { string Mode { get; } decimal Calculate(decimal price, int quantity); }
public sealed class FixedPricing : IPricingStrategy
{
    public string Mode => "Fixed";
    public decimal Calculate(decimal price, int quantity) => quantity == 1 ? price : throw new ApiException(422,"INVALID_QUANTITY","This feature can only be selected once.");
}
public sealed class PerUnitPricing : IPricingStrategy
{
    public string Mode => "PerUnit";
    public decimal Calculate(decimal price, int quantity) => price * quantity;
}
public interface IInvoiceRenderer { byte[] Render(KnightCore.Domain.Invoice invoice, CommercialSnapshot snapshot); }
public interface IInvoiceStorage
{
    Task SaveAsync(string key, byte[] data, CancellationToken ct);
    Task<byte[]> ReadAsync(string key, CancellationToken ct);
}
public sealed record EmailMessage(string To, string Subject, string Html);
public interface IEmailSender { Task SendAsync(EmailMessage message, CancellationToken ct); }

