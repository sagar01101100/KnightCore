using System.Net;
using System.Net.Mail;
using KnightCore.Application;
namespace KnightCore.Infrastructure;
public sealed class LocalEmailSender(IConfiguration config,IWebHostEnvironment environment) : IEmailSender
{
    public async Task SendAsync(EmailMessage message,CancellationToken ct)
    {
        var path=Path.Combine(environment.ContentRootPath,config["DataDirectory"]??"data","mailbox");
        Directory.CreateDirectory(path);
        await File.WriteAllTextAsync(Path.Combine(path,Guid.NewGuid().ToString("N")+".html"),$"<meta charset=\"utf-8\"><h1>{WebUtility.HtmlEncode(message.Subject)}</h1><p>To: {WebUtility.HtmlEncode(message.To)}</p>"+message.Html,ct);
    }
}
public sealed class SmtpEmailSender(IConfiguration config) : IEmailSender
{
    public async Task SendAsync(EmailMessage message,CancellationToken ct)
    {
        using var smtp=new SmtpClient(config["Email:Host"],config.GetValue("Email:Port",587)){EnableSsl=true,Credentials=new NetworkCredential(config["Email:Username"],config["Email:Password"])};
        using var mail=new MailMessage(config["Email:From"]!,message.To,message.Subject,message.Html){IsBodyHtml=true};
        await smtp.SendMailAsync(mail,ct);
    }
}

