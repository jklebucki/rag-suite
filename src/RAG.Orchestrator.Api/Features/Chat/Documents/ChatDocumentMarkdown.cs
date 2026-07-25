using RAG.DocumentProcessing.Abstractions;
using RAG.Orchestrator.Api.Features.Chat.Attachments;
using RAG.Orchestrator.Api.Models;

namespace RAG.Orchestrator.Api.Features.Chat.Documents;

public static class ChatDocumentMarkdown
{
    public static string AppendToMessage(string message, IEnumerable<ChatDocument> documents)
    {
        var documentArray = documents.ToArray();
        if (documentArray.Length == 0)
        {
            return message;
        }

        var attachmentContext = ChatAttachmentService.BuildAttachmentsPromptBlock(documentArray.Select(document => new ChatAttachmentFile(
            document.Id,
            document.FileName,
            document.ContentType,
            document.SizeBytes,
            document.TokenCount,
            document.Markdown,
            Status: "ready",
            Progress: 100,
            PageCount: document.PageCount,
            Provider: document.Provider,
            UploadedAt: document.CreatedAt)));

        return string.IsNullOrWhiteSpace(message)
            ? attachmentContext
            : $"{message}\n\n{attachmentContext}";
    }

    public static string BuildDisplayResponse(IEnumerable<ChatDocument> documents)
    {
        var documentArray = documents.ToArray();
        return string.Join(
            "\n\n---\n\n",
            documentArray.Select(document => $"## OCR: {document.FileName}\n\n{document.Markdown.Trim()}"));
    }

    public static string BuildExportMarkdown(IEnumerable<ChatDocument> documents)
    {
        var documentArray = documents.ToArray();
        if (documentArray.Length == 1)
        {
            return documentArray[0].Markdown;
        }

        return string.Join(
            "\n\n---\n\n",
            documentArray.Select(document => $"# {document.FileName}\n\n{document.Markdown.Trim()}"));
    }

    public static string GetExportFileName(IEnumerable<ChatDocument> documents, GeneratedArtifactFormat format)
    {
        var documentArray = documents.ToArray();
        var baseName = documentArray.Length == 1
            ? Path.GetFileNameWithoutExtension(documentArray[0].FileName)
            : "ocr-results";
        var extension = format == GeneratedArtifactFormat.Docx ? ".docx" : ".txt";

        return $"{baseName}{extension}";
    }
}
