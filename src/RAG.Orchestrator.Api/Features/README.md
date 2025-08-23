# Features Architecture

This directory implements a **Vertical Slice Architecture** (also known as Feature-based organization) for the RAG Orchestrator API. Each feature is self-contained and includes all the necessary components.

## Structure

```
Features/
├── Analytics/          # Analytics and usage tracking
│   ├── AnalyticsEndpoints.cs   # API endpoints
│   ├── AnalyticsModels.cs      # Request/Response models
│   └── AnalyticsService.cs     # Business logic
├── Chat/              # Chat sessions and messaging
│   ├── ChatEndpoints.cs       # API endpoints
│   ├── ChatModels.cs          # Request/Response models
│   └── ChatService.cs         # Business logic
├── Plugins/           # Plugin management
│   ├── PluginEndpoints.cs     # API endpoints
│   ├── PluginModels.cs        # Request/Response models
│   └── PluginService.cs       # Business logic
└── Search/            # Document search functionality
    ├── SearchEndpoints.cs     # API endpoints
    ├── SearchModels.cs        # Request/Response models
    └── SearchService.cs       # Business logic
```

## Benefits of This Architecture

### 1. **High Cohesion, Low Coupling**
- Each feature contains all related functionality
- Minimal dependencies between features
- Easy to understand and modify individual features

### 2. **Scalability**
- Easy to add new features without affecting existing ones
- Each feature can be developed independently
- Supports team collaboration on different features

### 3. **Maintainability**
- Clear separation of concerns
- Easy to locate functionality
- Consistent structure across features

### 4. **Testability**
- Each service has a clear interface
- Easy to mock dependencies
- Isolated unit testing per feature

## Conventions

### Naming
- **Endpoints**: `{Feature}Endpoints.cs` with static `Map{Feature}Endpoints` method
- **Models**: `{Feature}Models.cs` containing all DTOs for the feature
- **Services**: `{Feature}Service.cs` with corresponding `I{Feature}Service` interface

### Registration
- All services are registered in `Extensions/ServiceCollectionExtensions.cs`
- All endpoints are mapped in `Program.cs`

### API Grouping
- Each feature uses route groups (e.g., `/api/search`, `/api/chat`)
- Consistent OpenAPI tags and documentation
- Proper HTTP status codes and responses

## Adding New Features

1. Create a new folder under `Features/`
2. Add the three core files: `{Feature}Endpoints.cs`, `{Feature}Models.cs`, `{Feature}Service.cs`
3. Register the service in `ServiceCollectionExtensions.cs`
4. Map the endpoints in `Program.cs`

## Example Feature Structure

```csharp
// Models
public record FeatureRequest(string Parameter);
public record FeatureResponse(string Result);

// Service Interface
public interface IFeatureService
{
    Task<FeatureResponse> ProcessAsync(FeatureRequest request);
}

// Service Implementation
public class FeatureService : IFeatureService
{
    public Task<FeatureResponse> ProcessAsync(FeatureRequest request)
    {
        // Business logic here
    }
}

// Endpoints
public static class FeatureEndpoints
{
    public static IEndpointRouteBuilder MapFeatureEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/feature")
            .WithTags("Feature")
            .WithOpenApi();

        group.MapPost("/", async (FeatureRequest request, IFeatureService service) =>
        {
            var response = await service.ProcessAsync(request);
            return Results.Ok(response);
        });

        return endpoints;
    }
}
```
