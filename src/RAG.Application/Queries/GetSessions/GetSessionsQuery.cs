using MediatR;
using FluentValidation;
using RAG.Application.Commands.SendMessage;

namespace RAG.Application.Queries.GetSessions;

/// <summary>
/// Query to get all chat sessions
/// </summary>
public record GetSessionsQuery : IRequest<IEnumerable<ChatSessionDto>>
{
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 50;
    public string? SearchTerm { get; init; }
}

public class GetSessionsQueryValidator : AbstractValidator<GetSessionsQuery>
{
    public GetSessionsQueryValidator()
    {
        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Skip must be non-negative");

        RuleFor(x => x.Take)
            .GreaterThan(0)
            .LessThanOrEqualTo(100)
            .WithMessage("Take must be between 1 and 100");
    }
}

public class GetSessionsQueryHandler : IRequestHandler<GetSessionsQuery, IEnumerable<ChatSessionDto>>
{
    private readonly IChatSessionRepository _repository;
    private readonly ILogger<GetSessionsQueryHandler> _logger;

    public GetSessionsQueryHandler(IChatSessionRepository repository, ILogger<GetSessionsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<ChatSessionDto>> Handle(GetSessionsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting chat sessions with skip: {Skip}, take: {Take}", request.Skip, request.Take);

        try
        {
            var sessions = await _repository.GetAllAsync(cancellationToken);
            
            var filtered = sessions.AsQueryable();
            
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                filtered = filtered.Where(s => 
                    s.Title.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    s.Messages.Any(m => m.Content.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)));
            }

            return filtered
                .OrderByDescending(s => s.UpdatedAt)
                .Skip(request.Skip)
                .Take(request.Take)
                .Select(MapToDto)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat sessions");
            throw;
        }
    }

    private static ChatSessionDto MapToDto(RAG.Domain.Entities.ChatSession session)
    {
        return new ChatSessionDto
        {
            Id = session.Id,
            Title = session.Title,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            Messages = session.Messages.Select(m => new ChatMessageDto
            {
                Id = m.Id,
                Content = m.Content,
                Role = m.Role,
                CreatedAt = m.CreatedAt,
                Sources = m.SearchContext?.Sources.Select(s => new SourceReference
                {
                    Id = s.Id,
                    Title = s.Title,
                    Excerpt = s.Snippet,
                    Relevance = s.Relevance
                }).ToList()
            }).ToList()
        };
    }
}

/// <summary>
/// Query to get a specific session by ID
/// </summary>
public record GetSessionQuery : IRequest<ChatSessionDto?>
{
    public string SessionId { get; init; } = string.Empty;
}

public class GetSessionQueryValidator : AbstractValidator<GetSessionQuery>
{
    public GetSessionQueryValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("Session ID is required");
    }
}

public class GetSessionQueryHandler : IRequestHandler<GetSessionQuery, ChatSessionDto?>
{
    private readonly IChatSessionRepository _repository;
    private readonly ILogger<GetSessionQueryHandler> _logger;

    public GetSessionQueryHandler(IChatSessionRepository repository, ILogger<GetSessionQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ChatSessionDto?> Handle(GetSessionQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting session {SessionId}", request.SessionId);

        try
        {
            var session = await _repository.GetByIdAsync(request.SessionId, cancellationToken);
            
            if (session == null)
                return null;

            return new ChatSessionDto
            {
                Id = session.Id,
                Title = session.Title,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt,
                Messages = session.Messages.Select(m => new ChatMessageDto
                {
                    Id = m.Id,
                    Content = m.Content,
                    Role = m.Role,
                    CreatedAt = m.CreatedAt,
                    Sources = m.SearchContext?.Sources.Select(s => new SourceReference
                    {
                        Id = s.Id,
                        Title = s.Title,
                        Excerpt = s.Snippet,
                        Relevance = s.Relevance
                    }).ToList()
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session {SessionId}", request.SessionId);
            throw;
        }
    }
}

// DTOs
public record ChatSessionDto
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<ChatMessageDto> Messages { get; init; } = new();
}

public record ChatMessageDto
{
    public string Id { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public List<SourceReference>? Sources { get; init; }
}

public record SourceReference
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Excerpt { get; init; } = string.Empty;
    public double Relevance { get; init; }
    public string Source { get; init; } = string.Empty;
}
