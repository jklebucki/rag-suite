# Project overview
Modular monolith with **Vertical Slice Architecture** on **.NET 8 Minimal APIs** (backend) and **React 18 + TypeScript** (frontend). Goal: clear feature boundaries, fast iteration, easy modularization.

# Architecture & boundaries
- Use **feature slices**: each feature owns Endpoints, Service (use cases), Models (DTOs), optional Validators/Mappings.
- **No cross-feature references.** Shared abstractions in `Domain` or `Application.Abstractions`; implementations in `Infrastructure`.
- Depend on **ports/interfaces** only. Avoid direct `DbContext` usage from features for writes.
- Repositories are **per aggregate**; one `SaveChanges` per request; cross-feature side effects via **domain events + outbox**.
- Reads spanning features: use **read services/projections (ports)**, not calling other feature handlers.

# Minimal APIs (ASP.NET Core)
- Use **route groups** and `WithOpenApi()`; return **TypedResults** everywhere.
- Apply **endpoint filters** for cross-cutting (validation, auth, logging).
- Every endpoint accepts `CancellationToken`; prefer `async`.
- Version endpoints with route groups (`/api/v{version}/{feature}`) when needed.

# Validation & errors
- Validate at the boundary (eg. **FluentValidation**).
- Map validation issues to `Results.ValidationProblem(...)`.
- Use **RFC7807 ProblemDetails** for errors (400/404/409/422). Keep messages actionable.

# Data & persistence
- **EF Core**: `DbContext` is Unit of Work; avoid custom generic repository.
- Per-aggregate repositories (interfaces in `Domain/Application.Abstractions`, impl in `Infrastructure`).
- PostgreSQL uses snake_case; one transaction/save per request.

# Search (Elasticsearch)
- Use **hybrid search** (lexical + vector) with **BGE-M3** embeddings; prefer RRF/convex blend when ranking.
- Keep mappings in `deploy/elastic/mappings/`. Expose filters/pagination in APIs.

# Security & configuration
- JWT (bearer) required on protected endpoints; authorize via route groups/policies.
- No secrets in code; use environment/secret store. Bind settings via the options pattern.
- Add `/health` and OpenAPI metadata to all public routes.

# Project structure (convention)
- `src/*Api/Features/{Feature}/` → `Endpoints.cs`, `Service.cs`, `Models.cs`, optional `Validators.cs`, `Mappings.cs`.
- `src/*Domain/` → Aggregates, Value Objects, domain events, repository/service interfaces.
- `src/*Application.Abstractions/` → ports, Result/Errors primitives.
- `src/*Infrastructure/` → EF Core repos, outbox, email, clock, current-user.
- `src/*Web` or `web/` → React 18 + TS (co-located UI by feature when practical).

# Coding guidelines
- C#: async/await with `CancellationToken`; file-scoped namespaces; records for DTOs; guard clauses.
- React: function components + hooks; strict TS; API calls in services/hooks; cache/loading/error via React Query.
- Keep DTOs **local to a feature** unless truly cross-cutting; align FE/BE DTO shapes.
- **All code comments must be written in English, regardless of the conversation language.**
