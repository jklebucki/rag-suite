using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RAG.Application.Commands.SendMessage;
using RAG.Application.Plugins;
using RAG.Application.Services;
using RAG.Domain.Services;
using RAG.Infrastructure.Oracle;
using RAG.Infrastructure.Persistence;
using RAG.Infrastructure.SemanticKernel;
using MediatR;
using FluentValidation;

namespace RAG.Infrastructure;

/// <summary>
/// Infrastructure layer dependency injection configuration
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add domain services
        services.AddScoped<IChatDomainService, ChatDomainService>();
        services.AddScoped<ISearchDomainService, SearchDomainService>();

        // Add repositories
        services.AddScoped<IChatSessionRepository, InMemoryChatSessionRepository>();
        
        // Add external services
        services.AddHttpClient<IElasticsearchService, ElasticsearchServiceAdapter>();
        services.AddScoped<IOracleService, OracleService>();

        // Add Semantic Kernel
        services.AddSemanticKernel(configuration);

        // Add MediatR
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(typeof(RAG.Application.Commands.SendMessage.SendMessageCommand).Assembly);
        });

        // Add FluentValidation
        services.AddValidatorsFromAssembly(typeof(RAG.Application.Commands.SendMessage.SendMessageCommand).Assembly);

        // Add logging
        services.AddLogging();

        return services;
    }
}

/// <summary>
/// Extension for application layer registration
/// </summary>
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register application services
        services.AddScoped<ISemanticKernelService, RAG.Infrastructure.SemanticKernel.SemanticKernelService>();
        
        // Add validation behavior
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        
        return services;
    }
}

/// <summary>
/// MediatR validation pipeline behavior
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators, 
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
        {
            _logger.LogWarning("Validation failed for {RequestType}: {Errors}", 
                typeof(TRequest).Name, 
                string.Join(", ", failures.Select(f => f.ErrorMessage)));

            throw new ValidationException(failures);
        }

        return await next();
    }
}

/// <summary>
/// Custom validation exception
/// </summary>
public class ValidationException : Exception
{
    public IEnumerable<FluentValidation.Results.ValidationFailure> Errors { get; }

    public ValidationException(IEnumerable<FluentValidation.Results.ValidationFailure> errors)
        : base("One or more validation failures have occurred.")
    {
        Errors = errors;
    }

    public override string Message
    {
        get
        {
            return base.Message + " " + string.Join(" ", Errors.Select(e => e.ErrorMessage));
        }
    }
}
