using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;

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
        var smtpSettings = _configuration.GetSection("SmtpSettings");
        var smtpHost = smtpSettings["Host"];
        var smtpPort = int.Parse(smtpSettings["Port"] ?? "587");
        var smtpUsername = smtpSettings["Username"];
        var smtpPassword = Environment.GetEnvironmentVariable("AI_ASSISTANT_EMAIL_PASS") ?? smtpSettings["Password"];
        var fromEmail = smtpSettings["FromEmail"] ?? "noreply@yourapp.com";

        if (string.IsNullOrEmpty(smtpPassword))
        {
            throw new InvalidOperationException("SMTP password is not configured. Please set the AI_ASSISTANT_EMAIL_PASS environment variable.");
        }

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUsername, smtpPassword),
            EnableSsl = true
        };

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
}