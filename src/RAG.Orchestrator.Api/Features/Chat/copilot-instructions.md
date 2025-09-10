# RAG Suite - AI Agent Instructions

## Architecture Overview

RAG Suite is a monorepo implementing **Retrieval-Augmented Generation** using .NET 8, Semantic Kernel, BGE-M3 embeddings, and Elasticsearch. The system follows a **Vertical Slice Architecture** with distinct service boundaries.

### Core Services & Boundaries
- **RAG.Orchestrator.Api**: Main API using Minimal APIs (.NET 8) with feature-based organization
- **RAG.Security**: JWT authentication, user management, role-based access (SQLite)
- **RAG.Collector**: Document ingestion and processing service
- **RAG.Web.UI**: React 18 + TypeScript frontend with Vite
- **RAG.Connectors/**: Integration adapters (Elastic, Oracle, Files)
- **RAG.Plugins/**: Semantic Kernel plugins (OracleSql, IfsSop, BizProcess)

## Key Development Patterns

### Vertical Slice Architecture (Features)
Each feature in `src/RAG.Orchestrator.Api/Features/` is self-contained:
```
Features/{FeatureName}/
├── {Feature}Endpoints.cs    # Static Map{Feature}Endpoints extension
├── {Feature}Models.cs       # DTOs and request/response models  
├── {Feature}Service.cs      # Business logic + I{Feature}Service interface
```

**Critical**: When adding features, you MUST:
1. Register service in `Extensions/ServiceCollectionExtensions.cs`
2. Map endpoints in `Program.cs` with `app.Map{Feature}Endpoints()`
3. Use route groups: `endpoints.MapGroup("/api/{feature-name}")`

### Service Registration Pattern
All services use extension methods in `ServiceCollectionExtensions.cs`:
```csharp
builder.Services.AddFeatureServices();        // All feature services
builder.Services.AddRAGSecurity(config);      // Authentication
builder.Services.AddChatDatabase(config);     // EntityFramework
builder.Services.AddSemanticKernel();         // AI integration
```

### Database Context Pattern
- **ChatDbContext**: PostgreSQL for chat sessions/messages (per-user isolation)
- **SecurityDbContext**: SQLite for users/roles (in RAG.Security)
- Uses snake_case naming convention for PostgreSQL tables

### Configuration Architecture
- `appsettings.{Environment}.json` for environment-specific settings
- **ElasticsearchOptions**: Vector store configuration
- **LlmEndpointConfig**: Ollama vs other LLM providers
- **JWT settings**: In RAG.Security configuration

## Critical Workflows

### Development Setup
```bash
# Start infrastructure (required first)
cd deploy && docker-compose up -d

# Run API (auto-creates databases)
cd src/RAG.Orchestrator.Api && dotnet run

# Run frontend
cd src/RAG.Web.UI && npm install && npm run dev
```

### Chat Service Architecture
- **UserChatService**: Per-user chat sessions with conversation history
- **LLM Integration**: Ollama-first with fallback to Semantic Kernel
- **Document Search**: BGE-M3 embeddings in Elasticsearch with hybrid search
- **Multilingual**: Auto-detection + localization via JSON files

### Search Integration Pattern
Services access search via `ISearchService` (RAG.Abstractions):
```csharp
var searchResults = await _searchService.SearchAsync(new SearchRequest(
    query, Filters: null, Limit: 1, Offset: 0), cancellationToken);
```

### Error Handling Convention
- Controllers return `Results.Ok()` or appropriate HTTP status
- Services throw exceptions with meaningful messages
- Chat services save error responses to database for user visibility

## Testing & Build

### Project Structure
- **Solution**: `RAGSuite.sln` (multi-project monorepo)
- **Tests**: `src/RAG.Tests/` (xUnit)
- **Build**: `dotnet build` from solution root or individual projects

### Dependencies
- **Microsoft.SemanticKernel**: AI orchestration
- **Elasticsearch.Net**: Vector search
- **Npgsql.EntityFrameworkCore.PostgreSQL**: Chat persistence
- **Microsoft.AspNetCore.OpenApi + Swashbuckle**: API documentation

## Deployment Architecture

### Docker Infrastructure
- **Elasticsearch 8.11.3**: Vector store with security enabled
- **Embedding Service**: Hugging Face text-embeddings-inference (CPU)
- **Kibana**: Elasticsearch management UI
- Production: NGINX reverse proxy + systemd services

### Environment Configuration
- **Development**: Local with Docker infrastructure
- **Production**: Full containerization with SSL via Let's Encrypt
- **Scripts**: `quick-install.sh` for one-command deployment

## Integration Points

### LLM Provider Abstraction
System supports multiple LLM providers through configuration:
- **Ollama**: Primary (supports chat history, system messages)
- **Other providers**: Via Semantic Kernel fallback

### Vector Store Integration
- **BGE-M3 embeddings**: 1024-dimensional vectors
- **Elasticsearch mappings**: In `deploy/elastic/mappings/`
- **Search strategies**: Full-text, semantic, hybrid

### Frontend-Backend Communication
- **JWT authentication**: Required for chat endpoints
- **RESTful APIs**: Feature-based endpoints (`/api/{feature}`)
- **Real-time**: Standard HTTP (no WebSockets currently)

## Troubleshooting

### Common Build Issues
- Missing `Map{Feature}Endpoints()` implementation → Check Features/{Feature}/
- Service registration errors → Check `ServiceCollectionExtensions.cs`
- Database errors → Ensure PostgreSQL container is running

### Development Dependencies
- **.NET 8 SDK**: Required for all projects
- **Node.js**: Required for React frontend
- **Docker**: Required for Elasticsearch, embeddings service
- **PostgreSQL**: Via Docker or local installation
