namespace RAG.Security.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string resetLink);
    Task SendForumReplyNotificationAsync(string email, string threadTitle, string replyAuthor, string threadLink, string messagePreview);
}