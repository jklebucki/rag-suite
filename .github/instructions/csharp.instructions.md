---
applyTo: "**/*.cs"
---

Follow Vertical Slice in `src/RAG.Orchestrator.Api/Features/**`.
Prefer Minimal APIs (.NET 8): MapGroup, endpoint filters, TypedResults.
Inject only interfaces from Domain/Application.Abstractions; implementations are in Infrastructure.
Use per-aggregate repositories (e.g., IOrderRepository); avoid generic repositories.
Keep DTOs and validators local to the feature; avoid cross-feature DTO reuse.
For reads that span features, use dedicated read services (ports) or projections, not cross-calling handlers.
Handle validation with FluentValidation; translate errors to ProblemDetails/TypedResults.
SaveChanges once per request; for cross-feature side effects prefer domain events + outbox.
