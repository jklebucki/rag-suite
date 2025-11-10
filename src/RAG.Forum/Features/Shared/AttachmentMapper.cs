using System.Text;
using RAG.Forum.Domain;

namespace RAG.Forum.Features.Shared;

public static class AttachmentMapper
{
    public const int MaxAttachments = 5;
    public const int MaxAttachmentSizeBytes = 5 * 1024 * 1024; // 5 MB

    public static bool TryCreateAttachments(
        IEnumerable<ForumAttachmentUpload>? uploads,
        Guid threadId,
        Guid? postId,
        DateTime createdAt,
        out List<ForumAttachment> attachments,
        out Dictionary<string, string[]> errors)
    {
        attachments = new List<ForumAttachment>();
        errors = new Dictionary<string, string[]>();

        if (uploads is null)
        {
            return true;
        }

        var uploadList = uploads.ToList();
        if (uploadList.Count > MaxAttachments)
        {
            errors["attachments"] = new[]
            {
                $"You can upload up to {MaxAttachments} attachments per message."
            };
            return false;
        }

        var errorMessages = new List<string>();

        foreach (var upload in uploadList)
        {
            if (string.IsNullOrWhiteSpace(upload.FileName))
            {
                errorMessages.Add("Attachment file name cannot be empty.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(upload.ContentType))
            {
                errorMessages.Add($"Attachment '{upload.FileName}' must include a content type.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(upload.DataBase64))
            {
                errorMessages.Add($"Attachment '{upload.FileName}' does not contain any data.");
                continue;
            }

            byte[] data;
            try
            {
                data = Convert.FromBase64String(upload.DataBase64);
            }
            catch (FormatException)
            {
                errorMessages.Add($"Attachment '{upload.FileName}' is not a valid Base64 string.");
                continue;
            }

            if (data.Length == 0)
            {
                errorMessages.Add($"Attachment '{upload.FileName}' does not contain any data.");
                continue;
            }

            if (data.Length > MaxAttachmentSizeBytes)
            {
                errorMessages.Add($"Attachment '{upload.FileName}' exceeds the maximum size of {FormatBytes(MaxAttachmentSizeBytes)}.");
                continue;
            }

            attachments.Add(new ForumAttachment
            {
                Id = Guid.NewGuid(),
                ThreadId = threadId,
                PostId = postId,
                FileName = upload.FileName.Trim(),
                ContentType = upload.ContentType.Trim(),
                Size = data.LongLength,
                Data = data,
                CreatedAt = createdAt
            });
        }

        if (errorMessages.Count > 0)
        {
            errors["attachments"] = errorMessages.ToArray();
            attachments.Clear();
            return false;
        }

        return true;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        var units = new[] { "KB", "MB", "GB", "TB" };
        double size = bytes;
        var unitIndex = -1;

        do
        {
            size /= 1024;
            unitIndex++;
        } while (size >= 1024 && unitIndex < units.Length - 1);

        return $"{size:0.#} {units[unitIndex]}";
    }
}

