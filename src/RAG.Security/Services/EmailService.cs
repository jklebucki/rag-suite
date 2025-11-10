using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace RAG.Security.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetLink)
    {
        using var client = CreateSmtpClient(out var fromEmail);
        var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail),
            Subject = "Reset Your Password",
            Body = $"Click the following link to reset your password: {resetLink}",
            IsBodyHtml = false
        };

        mailMessage.To.Add(email);

        await client.SendMailAsync(mailMessage);
    }

    public async Task SendForumReplyNotificationAsync(string email, string threadTitle, string replyAuthor, string threadLink, string messagePreview)
    {
        using var client = CreateSmtpClient(out var fromEmail);
        var subject = $"New reply in \"{threadTitle}\"";
        var body = $"Hello,\n\n" +
                   $"A new reply was posted by {replyAuthor} in the thread \"{threadTitle}\".\n\n" +
                   $"Preview:\n{messagePreview}\n\n" +
                   $"View the discussion: {threadLink}\n\n" +
                   $"— RAG Suite Forum";

        var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        mailMessage.To.Add(email);

        await client.SendMailAsync(mailMessage);
    }

    private SmtpClient CreateSmtpClient(out string fromEmail)
    {
        var smtpSettings = _configuration.GetSection("SmtpSettings");
        var smtpHost = smtpSettings["Host"];
        var smtpPort = int.Parse(smtpSettings["Port"] ?? "587");
        var smtpUsername = smtpSettings["Username"];
        var smtpPassword = Environment.GetEnvironmentVariable("AI_ASSISTANT_EMAIL_PASS") ?? smtpSettings["Password"];
        fromEmail = smtpSettings["FromEmail"] ?? "noreply@yourapp.com";

        if (string.IsNullOrEmpty(smtpPassword))
        {
            throw new InvalidOperationException("SMTP password is not configured. Please set the AI_ASSISTANT_EMAIL_PASS environment variable.");
        }

        return new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUsername, smtpPassword),
            EnableSsl = true
        };
    }
}