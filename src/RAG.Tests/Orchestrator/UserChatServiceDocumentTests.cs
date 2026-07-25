using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Moq;
using RAG.Abstractions.Search;
using RAG.DocumentProcessing.Abstractions;
using RAG.Orchestrator.Api.Data;
using RAG.Orchestrator.Api.Features.Chat;
using RAG.Orchestrator.Api.Features.Chat.Attachments;
using RAG.Orchestrator.Api.Features.Chat.Artifacts;
using RAG.Orchestrator.Api.Features.Chat.Prompting;
using RAG.Orchestrator.Api.Features.Chat.SessionManagement;
using RAG.Orchestrator.Api.Localization;
using RAG.Orchestrator.Api.Models;
using RAG.Orchestrator.Api.Services;
using RAG.Security.Data;

namespace RAG.Tests.Orchestrator;

public class UserChatServiceDocumentTests : IDisposable
{
    private const string UserId = "user-1";
    private const string SessionId = "session-1";

    private readonly ChatDbContext _chatDbContext;
    private readonly SecurityDbContext _securityDbContext;

    public UserChatServiceDocumentTests()
    {
        _chatDbContext = new ChatDbContext(new DbContextOptionsBuilder<ChatDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        _securityDbContext = new SecurityDbContext(new DbContextOptionsBuilder<SecurityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        _chatDbContext.ChatSessions.Add(new ChatSession
        {
            Id = SessionId,
            UserId = UserId,
            Title = "Test chat",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _chatDbContext.SaveChanges();
    }

    public void Dispose()
    {
        _securityDbContext.Dispose();
        _chatDbContext.Dispose();
    }

    [Fact]
    public async Task SendUserMultilingualMessageAsync_WhenUserRequestsDocumentContent_PersistsAndReturnsCanonicalMarkdownWithoutLlm()
    {
        var markdown = "| Product | Seats |\n| --- | ---: |\n| ERP | 13 |";
        var attachments = CreateAttachmentService(CreatePreparedAttachments(markdown));
        var llmService = new Mock<ILlmService>(MockBehavior.Strict);
        var service = CreateService(attachments.Object, llmService.Object, new Mock<IGeneratedArtifactService>(MockBehavior.Strict).Object);

        var response = await service.SendUserMultilingualMessageAsync(
            UserId,
            SessionId,
            new MultilingualChatRequest
            {
                Message = "Pokaż zawartość dokumentu",
                Language = "pl",
                ResponseLanguage = "pl",
                UseDocumentSearch = false,
                AttachmentIds = ["draft-1"]
            });

        Assert.Contains(markdown, response.Response, StringComparison.Ordinal);
        var document = await _chatDbContext.ChatDocuments.SingleAsync();
        Assert.Equal(markdown, document.Markdown);
        Assert.Equal(response.UserMessageId, document.UserMessageId);
        Assert.DoesNotContain(llmService.Invocations, invocation => invocation.Method.Name == nameof(ILlmService.ChatWithHistoryAsync));
        attachments.Verify(service => service.CommitMessageAttachmentsAsync(UserId, SessionId, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendUserMultilingualMessageAsync_WhenUserCorrectsOcrDocumentAndRequestsDocx_UsesLlmResultForArtifact()
    {
        const string ocrMarkdown = "# Contract\n\nTeh custmer has 13 seats.";
        const string correctedMarkdown = "# Contract\n\nThe customer has 13 seats.";
        const string llmResponse = "<generated_artifact format=\"docx\" filename=\"corrected-contract.docx\"># Contract\n\nThe customer has 13 seats.</generated_artifact>";
        const string downloadLink = "[Download corrected-contract.docx](/api/user-chat/artifacts/artifact-1/download)";
        var attachments = CreateAttachmentService(CreatePreparedAttachments(ocrMarkdown));
        var llmService = new Mock<ILlmService>(MockBehavior.Strict);
        llmService
            .Setup(service => service.ChatWithHistoryAsync(
                It.IsAny<IEnumerable<LlmChatMessage>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmUserContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);
        var artifactService = new Mock<IGeneratedArtifactService>(MockBehavior.Strict);
        artifactService
            .Setup(service => service.CreateAsync(
                GeneratedArtifactFormat.Docx,
                "contract.docx",
                correctedMarkdown,
                UserId,
                SessionId,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArtifactGenerationResult(downloadLink, true));
        var service = CreateService(attachments.Object, llmService.Object, artifactService.Object, isOllama: true);

        var response = await service.SendUserMultilingualMessageAsync(
            UserId,
            SessionId,
            new MultilingualChatRequest
            {
                Message = "Popraw tekst wynikający z załącznika, bo ma błędy językowe, i zapisz do DOCX.",
                Language = "pl",
                ResponseLanguage = "pl",
                UseDocumentSearch = false,
                AttachmentIds = ["draft-1"]
            });

        Assert.Contains(correctedMarkdown, response.Response, StringComparison.Ordinal);
        Assert.Contains(downloadLink, response.Response, StringComparison.Ordinal);
        Assert.DoesNotContain("<generated_artifact", response.Response, StringComparison.OrdinalIgnoreCase);
        var invocation = Assert.Single(llmService.Invocations);
        Assert.Contains(ocrMarkdown, Assert.IsType<string>(invocation.Arguments[1]), StringComparison.Ordinal);
        Assert.Contains("Return only the complete transformed document in Markdown", Assert.IsType<string>(invocation.Arguments[1]), StringComparison.Ordinal);
        artifactService.VerifyAll();
    }

    [Fact]
    public async Task SendUserMultilingualMessageAsync_WhenHistoryContainsDocument_InjectsCanonicalMarkdownIntoLlmHistory()
    {
        const string markdown = "| Product | Seats |\n| --- | ---: |\n| ERP | 13 |";
        _chatDbContext.ChatMessages.Add(new ChatMessage
        {
            Id = "historic-message",
            SessionId = SessionId,
            Role = "user",
            Content = "Analyze the document",
            Timestamp = DateTime.UtcNow.AddMinutes(-1)
        });
        _chatDbContext.ChatDocuments.Add(new ChatDocument
        {
            Id = "historic-document",
            UserMessageId = "historic-message",
            FileName = "contract.pdf",
            ContentType = "application/pdf",
            Markdown = markdown,
            SizeBytes = 128,
            TokenCount = 16,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1)
        });
        await _chatDbContext.SaveChangesAsync();

        var attachments = CreateAttachmentService(CreatePreparedAttachments());
        var llmService = new Mock<ILlmService>();
        llmService
            .Setup(service => service.ChatWithHistoryAsync(
                It.IsAny<IEnumerable<LlmChatMessage>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LlmUserContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("The document has 13 seats.");
        var artifactService = new Mock<IGeneratedArtifactService>();
        artifactService
            .Setup(service => service.ProcessAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArtifactGenerationResult("The document has 13 seats.", false));
        var service = CreateService(attachments.Object, llmService.Object, artifactService.Object, isOllama: true);

        await service.SendUserMultilingualMessageAsync(
            UserId,
            SessionId,
            new MultilingualChatRequest
            {
                Message = "How many seats are there?",
                Language = "en",
                ResponseLanguage = "en",
                UseDocumentSearch = false
            });

        var invocation = llmService.Invocations.Single(item => item.Method.Name == nameof(ILlmService.ChatWithHistoryAsync));
        var history = Assert.IsAssignableFrom<IEnumerable<LlmChatMessage>>(invocation.Arguments[0]).ToArray();
        var historicMessage = Assert.Single(history);
        Assert.Contains(markdown, historicMessage.Content, StringComparison.Ordinal);
    }

    private UserChatService CreateService(
        IChatAttachmentService attachmentService,
        ILlmService llmService,
        IGeneratedArtifactService artifactService,
        bool isOllama = false)
    {
        var languageService = new Mock<ILanguageService>();
        languageService.Setup(service => service.DetectLanguage(It.IsAny<string>())).Returns("en");
        languageService.Setup(service => service.NormalizeLanguage(It.IsAny<string>())).Returns<string>(language => language);
        languageService.Setup(service => service.GetLocalizedErrorMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>())).Returns("Processing failed");

        var settingsService = new Mock<IGlobalSettingsService>();
        settingsService.Setup(service => service.GetLlmSettingsAsync()).ReturnsAsync(new LlmSettings { IsOllama = isOllama });

        var promptBuilder = new Mock<IPromptBuilder>();
        promptBuilder
            .Setup(service => service.BuildMultilingualContextualPrompt(It.IsAny<PromptContext>()))
            .Returns<PromptContext>(context => context.UserMessage);

        return new UserChatService(
            _chatDbContext,
            _securityDbContext,
            new Kernel(),
            new Mock<ISearchService>(MockBehavior.Strict).Object,
            languageService.Object,
            NullLogger<UserChatService>.Instance,
            new ConfigurationBuilder().Build(),
            llmService,
            settingsService.Object,
            new Mock<ISessionManager>(MockBehavior.Strict).Object,
            promptBuilder.Object,
            attachmentService,
            artifactService,
            new Mock<IArtifactSessionCleanupService>(MockBehavior.Strict).Object);
    }

    private static Mock<IChatAttachmentService> CreateAttachmentService(PreparedChatAttachments preparedAttachments)
    {
        var attachmentService = new Mock<IChatAttachmentService>();
        attachmentService
            .Setup(service => service.PrepareForMessageAsync(UserId, SessionId, It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(preparedAttachments);
        attachmentService
            .Setup(service => service.CommitMessageAttachmentsAsync(UserId, SessionId, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        attachmentService
            .Setup(service => service.GetContextAsync(UserId, SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatContextUsageResponse?)null);
        return attachmentService;
    }

    private static PreparedChatAttachments CreatePreparedAttachments(string? markdown = null)
    {
        var files = markdown == null
            ? Array.Empty<ChatAttachmentFile>()
            :
            [
                new ChatAttachmentFile(
                    "draft-1",
                    "contract.pdf",
                    "application/pdf",
                    128,
                    16,
                    markdown,
                    PageCount: 1,
                    Provider: "docling")
            ];
        return new PreparedChatAttachments(
            files,
            files.Sum(file => file.TokenCount),
            new ChatContextUsageResponse(0, 9600, 0, false, 0, 12000, Array.Empty<ChatAttachmentDraft>()));
    }
}
