namespace RAG.Security.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string resetLink);
}