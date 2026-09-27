using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Security.Claims;
using KnightCore.Application;
using KnightCore.Domain;
using KnightCore.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;

namespace KnightCore.Api;

public sealed record RegisterInput([Required,StringLength(80,MinimumLength=2)]string Name,[Required,EmailAddress]string Email,[Required,StringLength(128,MinimumLength=10)]string Password);
public sealed record LoginInput([Required,EmailAddress]string Email,[Required]string Password);
public sealed record ConfirmInput([Required]string UserId,[Required]string Token);
public sealed record ForgotInput([Required,EmailAddress]string Email);
public sealed record ResetInput([Required]string UserId,[Required]string Token,[Required,StringLength(128,MinimumLength=10)]string Password);

[ApiController,Route("api/v1/auth"),EnableRateLimiting("auth")]
public sealed class AuthController(UserManager<AppUser> users,SignInManager<AppUser> signIn,AppDbContext db,IConfiguration config,IWebHostEnvironment environment,IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("csrf")]
    public IActionResult Csrf()=>Ok(new{token=antiforgery.GetAndStoreTokens(HttpContext).RequestToken});
    [HttpGet("session")]
    public async Task<IActionResult> Session()
    {
        if(User.Identity?.IsAuthenticated!=true)return Ok(new{user=(object?)null});
        var user=await users.GetUserAsync(User);
        if(user is null)return Ok(new{user=(object?)null});
        return Ok(new{user=new{user.Id,name=user.DisplayName,email=user.Email,roles=await users.GetRolesAsync(user)}});
    }
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterInput input)
    {
        var email=input.Email.Trim().ToLowerInvariant();
        var user=new AppUser{UserName=email,Email=email,DisplayName=input.Name.Trim()};
        var result=await users.CreateAsync(user,input.Password);
        if(!result.Succeeded)return BadRequest(new{code="REGISTRATION_FAILED",message=string.Join(" ",result.Errors.Select(x=>x.Description))});
        await users.AddToRoleAsync(user,"Customer");
        var token=WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(await users.GenerateEmailConfirmationTokenAsync(user)));
        var url=$"{config["PublicOrigin"]?.TrimEnd('/')}/confirm-email?userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(token)}";
        db.Jobs.Add(new(){Kind="Email",DeduplicationKey=$"confirm:{user.Id}:{Guid.NewGuid()}",PayloadJson=Json.Write(new EmailMessage(email,"Confirm your KnightCore email",$"<p>Confirm your email to place an order.</p><p><a href=\"{WebUtility.HtmlEncode(url)}\">Confirm email</a></p>"))});
        await db.SaveChangesAsync();
        return Ok(new{message="Check your email to confirm your account.",developmentConfirmationUrl=environment.IsDevelopment()?url:null});
    }
    [HttpPost("confirm-email")]
    public async Task<IActionResult> Confirm(ConfirmInput input)
    {
        var user=await users.FindByIdAsync(input.UserId);
        if(user is null)throw new ApiException(400,"INVALID_LINK","This confirmation link is invalid.");
        string token;
        try{token=Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(input.Token));}
        catch(FormatException){throw new ApiException(400,"INVALID_LINK","This confirmation link is invalid.");}
        var result=await users.ConfirmEmailAsync(user,token);
        if(!result.Succeeded)throw new ApiException(400,"INVALID_LINK","This confirmation link is invalid or expired.");
        return Ok(new{message="Email confirmed. You can now sign in."});
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginInput input)
    {
        var user=await users.FindByEmailAsync(input.Email.Trim());
        if(user is null)throw new ApiException(401,"LOGIN_FAILED","Email or password is incorrect.");
        var result=await signIn.PasswordSignInAsync(user,input.Password,false,true);
        if(result.IsNotAllowed)throw new ApiException(403,"EMAIL_UNCONFIRMED","Please confirm your email before signing in.");
        if(result.IsLockedOut)throw new ApiException(429,"ACCOUNT_LOCKED","Too many attempts. Try again in a few minutes.");
        if(!result.Succeeded)throw new ApiException(401,"LOGIN_FAILED","Email or password is incorrect.");
        return Ok(new{user=new{user.Id,name=user.DisplayName,email=user.Email,roles=await users.GetRolesAsync(user)}});
    }
    [Authorize,HttpPost("logout")]
    public async Task<IActionResult> Logout(){await signIn.SignOutAsync();return Ok(new{message="Signed out."});}
    [HttpPost("forgot-password")]
    public async Task<IActionResult> Forgot(ForgotInput input)
    {
        var user=await users.FindByEmailAsync(input.Email.Trim());
        string? url=null;
        if(user is not null&&user.EmailConfirmed)
        {
            var token=WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(await users.GeneratePasswordResetTokenAsync(user)));
            url=$"{config["PublicOrigin"]?.TrimEnd('/')}/reset-password?userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(token)}";
            db.Jobs.Add(new(){Kind="Email",DeduplicationKey=$"reset:{Guid.NewGuid()}",PayloadJson=Json.Write(new EmailMessage(user.Email!,"Reset your KnightCore password",$"<p><a href=\"{WebUtility.HtmlEncode(url)}\">Reset password</a></p>"))});
            await db.SaveChangesAsync();
        }
        return Ok(new{message="If this account exists, a reset link has been sent.",developmentResetUrl=environment.IsDevelopment()?url:null});
    }
    [HttpPost("reset-password")]
    public async Task<IActionResult> Reset(ResetInput input)
    {
        var user=await users.FindByIdAsync(input.UserId)??throw new ApiException(400,"INVALID_LINK","The reset link is invalid.");
        string token;
        try{token=Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(input.Token));}
        catch(FormatException){throw new ApiException(400,"INVALID_LINK","The reset link is invalid.");}
        var result=await users.ResetPasswordAsync(user,token,input.Password);
        if(!result.Succeeded)throw new ApiException(400,"RESET_FAILED",string.Join(" ",result.Errors.Select(x=>x.Description)));
        return Ok(new{message="Password updated. Sign in with your new password."});
    }
}

