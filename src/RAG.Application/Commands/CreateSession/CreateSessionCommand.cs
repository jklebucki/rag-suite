using MediatR;
using FluentValidation;
using RAG.Application.Commands.SendMessage;
using RAG.Application.DTOs;
using RAG.Domain.Entities;
using RAG.Domain.ValueObjects;
using RAG.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace RAG.Application.Commands.CreateSession;

/// <summary>
/// Command to create a new chat session
/// </summary>
public record CreateSessionCommand : IRequest<ChatSessionDto>
{
    public string? Title { get; init; }
}

public class CreateSessionCommandValidator : AbstractValidator<CreateSessionCommand>
{
    public CreateSessionCommandValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(100)
            .WithMessage("Title cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Title));
    }
}

public class CreateSessionCommandHandler : IRequestHandler<CreateSessionCommand, ChatSessionDto>
{
    private readonly IChatSessionRepository _repository;
    private readonly ILogger<CreateSessionCommandHandler> _logger;

    public CreateSessionCommandHandler(IChatSessionRepository repository, ILogger<CreateSessionCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ChatSessionDto> Handle(CreateSessionCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new chat session with title: {Title}", request.Title);

        try
        {
            var session = new RAG.Domain.Entities.ChatSession(request.Title ?? "New Conversation");
            
            await _repository.CreateAsync(session, cancellationToken);

            return new ChatSessionDto
            {
                Id = session.Id,
                Title = session.Title,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt,
                Messages = new List<ChatMessageDto>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chat session");
            throw;
        }
    }
}

/// <summary>
/// Command to delete a chat session
/// </summary>
public record DeleteSessionCommand : IRequest<bool>
{
    public string SessionId { get; init; } = string.Empty;
}

public class DeleteSessionCommandValidator : AbstractValidator<DeleteSessionCommand>
{
    public DeleteSessionCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("Session ID is required");
    }
}

public class DeleteSessionCommandHandler : IRequestHandler<DeleteSessionCommand, bool>
{
    private readonly IChatSessionRepository _repository;
    private readonly ILogger<DeleteSessionCommandHandler> _logger;

    public DeleteSessionCommandHandler(IChatSessionRepository repository, ILogger<DeleteSessionCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteSessionCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting session {SessionId}", request.SessionId);

        try
        {
            var session = await _repository.GetByIdAsync(request.SessionId, cancellationToken);
            
            if (session == null)
            {
                _logger.LogWarning("Session {SessionId} not found for deletion", request.SessionId);
                return false;
            }

            await _repository.DeleteAsync(request.SessionId, cancellationToken);
            
            _logger.LogInformation("Successfully deleted session {SessionId}", request.SessionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session {SessionId}", request.SessionId);
            throw;
        }
    }
}

/// <summary>
/// Command to update session title
/// </summary>
public record UpdateSessionTitleCommand : IRequest<ChatSessionDto>
{
    public string SessionId { get; init; } = string.Empty;
    public string NewTitle { get; init; } = string.Empty;
}

public class UpdateSessionTitleCommandValidator : AbstractValidator<UpdateSessionTitleCommand>
{
    public UpdateSessionTitleCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("Session ID is required");

        RuleFor(x => x.NewTitle)
            .NotEmpty()
            .WithMessage("New title is required")
            .MaximumLength(100)
            .WithMessage("Title cannot exceed 100 characters");
    }
}

public class UpdateSessionTitleCommandHandler : IRequestHandler<UpdateSessionTitleCommand, ChatSessionDto>
{
    private readonly IChatSessionRepository _repository;
    private readonly ILogger<UpdateSessionTitleCommandHandler> _logger;

    public UpdateSessionTitleCommandHandler(IChatSessionRepository repository, ILogger<UpdateSessionTitleCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ChatSessionDto> Handle(UpdateSessionTitleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating title for session {SessionId} to: {NewTitle}", request.SessionId, request.NewTitle);

        try
        {
            var session = await _repository.GetByIdAsync(request.SessionId, cancellationToken)
                ?? throw new InvalidOperationException($"Session {request.SessionId} not found");

            session.UpdateTitle(request.NewTitle);
            await _repository.UpdateAsync(session, cancellationToken);

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
                    CreatedAt = m.CreatedAt
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session title for {SessionId}", request.SessionId);
            throw;
        }
    }
}
