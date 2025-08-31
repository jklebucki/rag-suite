using MediatR;
using FluentValidation;
using RAG.Application.Services;

namespace RAG.Application.Commands.SendMessage;

/// <summary>
/// Command for sending a message in a chat session with RAG
/// </summary>
public record SendMessageCommand : IRequest<SendMessageResult>
{
    public string SessionId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool UseRAG { get; init; } = true;
    public bool UseBusinessProcessPlugin { get; init; } = false;
    public bool UseOraclePlugin { get; init; } = false;
}

public record SendMessageResult
{
    public string MessageId { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public IEnumerable<SourceReference> Sources { get; init; } = Array.Empty<SourceReference>();
    public string Model { get; init; } = string.Empty;
    public TimeSpan ProcessingTime { get; init; }
}

public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("Session ID is required");

        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Message cannot be empty")
            .MaximumLength(2000)
            .WithMessage("Message cannot exceed 2000 characters");
    }
}

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, SendMessageResult>
{
    private readonly ISemanticKernelService _semanticKernelService;
    private readonly IChatSessionRepository _chatSessionRepository;
    private readonly ILogger<SendMessageCommandHandler> _logger;

    public SendMessageCommandHandler(
        ISemanticKernelService semanticKernelService,
        IChatSessionRepository chatSessionRepository,
        ILogger<SendMessageCommandHandler> logger)
    {
        _semanticKernelService = semanticKernelService;
        _chatSessionRepository = chatSessionRepository;
        _logger = logger;
    }

    public async Task<SendMessageResult> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing message for session {SessionId}", request.SessionId);

        var startTime = DateTime.UtcNow;

        try
        {
            // Get or create session
            var session = await _chatSessionRepository.GetByIdAsync(request.SessionId, cancellationToken)
                ?? throw new InvalidOperationException($"Session {request.SessionId} not found");

            // Add user message
            var userMessage = session.AddMessage(request.Message, "user");

            // Generate AI response using Semantic Kernel
            var ragResponse = await _semanticKernelService.GenerateResponseAsync(
                request.Message, 
                request.SessionId, 
                cancellationToken);

            // Add AI message with search context
            var aiMessage = session.AddMessage(ragResponse.Content, "assistant");
            if (ragResponse.Sources.Any())
            {
                var searchContext = new Domain.Entities.SearchContext
                {
                    Query = request.Message,
                    ResultCount = ragResponse.Sources.Count(),
                    SearchDuration = ragResponse.ProcessingTime.TotalMilliseconds,
                    Sources = ragResponse.Sources.Select(s => new Domain.Entities.DocumentReference
                    {
                        Id = s.Id,
                        Title = s.Title,
                        Snippet = s.Excerpt,
                        Relevance = s.Relevance
                    }).ToList()
                };
                aiMessage.AttachSearchContext(searchContext);
            }

            // Save session
            await _chatSessionRepository.UpdateAsync(session, cancellationToken);

            var processingTime = DateTime.UtcNow - startTime;

            return new SendMessageResult
            {
                MessageId = aiMessage.Id,
                Content = ragResponse.Content,
                Sources = ragResponse.Sources,
                Model = ragResponse.Model,
                ProcessingTime = processingTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for session {SessionId}", request.SessionId);
            throw;
        }
    }
}

// Repository interface (to be implemented in Infrastructure)
public interface IChatSessionRepository
{
    Task<Domain.Entities.ChatSession?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Domain.Entities.ChatSession> CreateAsync(Domain.Entities.ChatSession session, CancellationToken cancellationToken = default);
    Task UpdateAsync(Domain.Entities.ChatSession session, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Domain.Entities.ChatSession>> GetAllAsync(CancellationToken cancellationToken = default);
}
