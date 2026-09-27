using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using KnightCore.Application;
using KnightCore.Domain;
using KnightCore.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder=WebApplication.CreateBuilder(args);
var dataPath=Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath,builder.Configuration["DataDirectory"]??"data"));
Directory.CreateDirectory(dataPath);
var keyPath=Path.Combine(dataPath,"keys");Directory.CreateDirectory(keyPath);
if(!OperatingSystem.IsWindows())File.SetUnixFileMode(keyPath,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
var provider=builder.Configuration["Database:Provider"]??"SqlServer";
if(!builder.Environment.IsDevelopment()&&!builder.Environment.IsEnvironment("Testing"))
{
    if(provider!="SqlServer")throw new InvalidOperationException("Production requires the configured SQL Server provider.");
    if(builder.Configuration["Email:Provider"]!="Smtp")throw new InvalidOperationException("Configure SMTP delivery before running in Production.");
    if(!Uri.TryCreate(builder.Configuration["PublicOrigin"],UriKind.Absolute,out var origin)||origin.Scheme!="https")throw new InvalidOperationException("Production PublicOrigin must use HTTPS.");
}
builder.WebHost.ConfigureKestrel(o=>o.Limits.MaxRequestBodySize=128*1024);
builder.Services.AddDbContext<AppDbContext>(o=>{
    if(provider=="Sqlite")o.UseSqlite(builder.Configuration.GetConnectionString("Default"));
    else o.UseSqlServer(builder.Configuration.GetConnectionString("Default"),sql=>sql.EnableRetryOnFailure());
});
builder.Services.AddIdentity<AppUser,IdentityRole>(o=>{
    o.User.RequireUniqueEmail=true;
    o.Password.RequiredLength=10;o.Password.RequireNonAlphanumeric=false;
    o.SignIn.RequireConfirmedEmail=true;
    o.Lockout.MaxFailedAccessAttempts=5;o.Lockout.DefaultLockoutTimeSpan=TimeSpan.FromMinutes(10);
}).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(o=>{
    o.Cookie.Name="KnightCore.Session";o.Cookie.HttpOnly=true;o.Cookie.SameSite=SameSiteMode.Lax;
    o.Cookie.SecurePolicy=builder.Environment.IsDevelopment()||builder.Environment.IsEnvironment("Testing")?CookieSecurePolicy.SameAsRequest:CookieSecurePolicy.Always;
    o.ExpireTimeSpan=TimeSpan.FromHours(8);o.SlidingExpiration=true;
    o.Events.OnRedirectToLogin=c=>{c.Response.StatusCode=401;return Task.CompletedTask;};
    o.Events.OnRedirectToAccessDenied=c=>{c.Response.StatusCode=403;return Task.CompletedTask;};
});
builder.Services.AddDataProtection().SetApplicationName("KnightCore").PersistKeysToFileSystem(new DirectoryInfo(keyPath));
builder.Services.Configure<SecurityStampValidatorOptions>(o=>o.ValidationInterval=TimeSpan.FromMinutes(2));
builder.Services.AddAntiforgery(o=>{o.HeaderName="X-XSRF-TOKEN";o.Cookie.Name="KnightCore.Csrf";o.Cookie.HttpOnly=true;o.Cookie.SameSite=SameSiteMode.Strict;o.Cookie.SecurePolicy=builder.Environment.IsDevelopment()||builder.Environment.IsEnvironment("Testing")?CookieSecurePolicy.SameAsRequest:CookieSecurePolicy.Always;});
builder.Services.AddControllers().AddJsonOptions(o=>o.JsonSerializerOptions.Converters.Add(new DecimalStringConverter()));
builder.Services.Configure<ApiBehaviorOptions>(o=>o.InvalidModelStateResponseFactory=c=>new BadRequestObjectResult(new{code="VALIDATION_FAILED",message="Please check the highlighted fields.",errors=c.ModelState.Where(x=>x.Value?.Errors.Count>0).ToDictionary(x=>x.Key,x=>x.Value!.Errors.Select(e=>e.ErrorMessage).ToArray())}));
builder.Services.Configure<BillingOptions>(builder.Configuration.GetSection("Billing"));
if(builder.Configuration.GetValue<decimal>("Billing:TaxRate") is <0 or >1)throw new InvalidOperationException("TaxRate must be between 0 and 1.");
builder.Services.AddScoped<IPriceCalculator,PriceCalculator>();builder.Services.AddScoped<QuoteService>();builder.Services.AddScoped<CheckoutService>();
builder.Services.AddSingleton<IPricingStrategy,FixedPricing>();builder.Services.AddSingleton<IPricingStrategy,PerUnitPricing>();
builder.Services.AddSingleton<IInvoiceRenderer,PdfInvoiceRenderer>();
if(builder.Configuration["InvoiceStorage:Provider"]=="Azure")builder.Services.AddSingleton<IInvoiceStorage,AzureInvoiceStorage>();
else builder.Services.AddSingleton<IInvoiceStorage,LocalInvoiceStorage>();
if(builder.Configuration["Email:Provider"]=="Smtp")builder.Services.AddSingleton<IEmailSender,SmtpEmailSender>();
else builder.Services.AddSingleton<IEmailSender,LocalEmailSender>();
builder.Services.AddHostedService<JobWorker>();
builder.Services.AddRateLimiter(o=>{
    o.RejectionStatusCode=429;
    o.OnRejected=async(c,ct)=>{c.HttpContext.Response.Headers.RetryAfter="60";await c.HttpContext.Response.WriteAsJsonAsync(new{code="RATE_LIMITED",message="Too many requests. Please try again shortly."},ct);};
    foreach(var (name,limit) in new[]{("auth",80),("estimate",120),("checkout",30)})
        o.AddPolicy(name,c=>RateLimitPartition.GetFixedWindowLimiter(c.User.FindFirstValue(ClaimTypes.NameIdentifier)??c.Connection.RemoteIpAddress?.ToString()??"anonymous",_=>new FixedWindowRateLimiterOptions{PermitLimit=limit,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));
});
var app=builder.Build();
await using(var scope=app.Services.CreateAsyncScope())
{
    var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if(args.Contains("--initialize"))
    {
        if(provider=="Sqlite")await db.Database.EnsureCreatedAsync();else await db.Database.MigrateAsync();
        await SeedData.InitializeAsync(scope.ServiceProvider,builder.Configuration);
        return;
    }
    if(builder.Environment.IsDevelopment()||builder.Environment.IsEnvironment("Testing"))
    {
        if(provider=="Sqlite")await db.Database.EnsureCreatedAsync();else await db.Database.MigrateAsync();
        await SeedData.InitializeAsync(scope.ServiceProvider,builder.Configuration);
    }
}
app.Use(async(context,next)=>{
    context.Response.Headers.XContentTypeOptions="nosniff";
    context.Response.Headers["Referrer-Policy"]="strict-origin-when-cross-origin";
    if(context.Request.Path.StartsWithSegments("/api"))context.Response.Headers.CacheControl="no-store";
    try{await next();}
    catch(ApiException e){context.Response.StatusCode=e.Status;await context.Response.WriteAsJsonAsync(new{code=e.Code,message=e.Message,correlationId=context.TraceIdentifier});}
    catch(DbUpdateConcurrencyException){context.Response.StatusCode=409;await context.Response.WriteAsJsonAsync(new{code="CONCURRENT_UPDATE",message="This record changed. Refresh and try again."});}
    catch(Exception e){app.Logger.LogError("Request failed ({ErrorType}), reference {TraceId}",e.GetType().Name,context.TraceIdentifier);context.Response.StatusCode=500;await context.Response.WriteAsJsonAsync(new{code="SERVER_ERROR",message="Something went wrong. Please try again.",correlationId=context.TraceIdentifier});}
});
app.UseAuthentication();app.UseAuthorization();app.UseRateLimiter();
app.Use(async(context,next)=>{
    if(context.Request.Path.StartsWithSegments("/api")&&context.Request.Method is not ("GET" or "HEAD" or "OPTIONS"))
    {
        try{await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context);}
        catch(AntiforgeryValidationException){context.Response.StatusCode=400;await context.Response.WriteAsJsonAsync(new{code="CSRF_FAILED",message="Your session needs refreshing. Reload the page and try again."});return;}
    }
    await next();
});
app.MapControllers();
app.MapGet("/api/v1/health",async(AppDbContext db,CancellationToken ct)=>await db.Database.CanConnectAsync(ct)?Results.Ok(new{status="healthy"}):Results.StatusCode(503));
app.Run();
public partial class Program {}
public sealed class DecimalStringConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)=>reader.TokenType==JsonTokenType.String?decimal.Parse(reader.GetString()!,System.Globalization.CultureInfo.InvariantCulture):reader.GetDecimal();
    public override void Write(Utf8JsonWriter writer,decimal value,JsonSerializerOptions options)=>writer.WriteStringValue(value.ToString("0.################",System.Globalization.CultureInfo.InvariantCulture));
}

