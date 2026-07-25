using RAG.DocumentProcessing.Abstractions;
using RAG.Orchestrator.Api.Features.Chat.Documents;
using RAG.Orchestrator.Api.Models;

namespace RAG.Tests.Orchestrator;

public class ChatDocumentMarkdownTests
{
    [Fact]
    public void AppendToMessage_WhenDocumentContainsTable_PreservesCanonicalMarkdown()
    {
        var markdown = "| Product | Licenses |\n| --- | ---: |\n| ERP | 13 |";
        var document = CreateDocument(markdown);

        var result = ChatDocumentMarkdown.AppendToMessage("What does the document contain?", [document]);

        Assert.StartsWith("What does the document contain?", result, StringComparison.Ordinal);
        Assert.Contains("=== USER ATTACHED FILES ===", result, StringComparison.Ordinal);
        Assert.Contains(markdown, result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildExportMarkdown_WithSingleDocument_ReturnsOriginalMarkdownWithoutTransformation()
    {
        var markdown = "| Product | Licenses |\n| --- | ---: |\n| ERP | 13 |";

        var result = ChatDocumentMarkdown.BuildExportMarkdown([CreateDocument(markdown)]);

        Assert.Equal(markdown, result);
        Assert.Equal("contract.docx", ChatDocumentMarkdown.GetExportFileName([CreateDocument(markdown)], GeneratedArtifactFormat.Docx));
    }

    private static ChatDocument CreateDocument(string markdown)
    {
        return new ChatDocument
        {
            Id = "document-1",
            UserMessageId = "message-1",
            FileName = "contract.pdf",
            ContentType = "application/pdf",
            Markdown = markdown,
            SizeBytes = 128,
            TokenCount = 16,
            CreatedAt = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc)
        };
    }
}
