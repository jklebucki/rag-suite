Use .NET 8 Minimal APIs with Vertical Slice architecture.
Each feature is self-contained: Endpoints, Models (DTOs), Services, optional Validators/Mappings.
Do not reference code across features; shared logic lives in Domain or Application.Abstractions, implementations in Infrastructure.
Depend on interfaces/ports only; avoid direct DbContext access in features for writes.
Use per-aggregate repositories; avoid generic repositories.
Keep DTOs local to each feature unless truly cross-cutting.
Use TypedResults, FluentValidation, and endpoint filters for validation and error handling.
Vector search uses Elasticsearch (BGE-M3). Frontend is React 18 + TypeScript via Vite; backend uses REST with JWT.
