---
applyTo: "**/*.cs"
---
Follow **Vertical Slice** in `**/Features/**`.
Prefer **.NET 8 Minimal APIs**: MapGroup + WithOpenApi, endpoint filters, **TypedResults**.
Inject only interfaces from Domain/Application.Abstractions; implementations live in Infrastructure.
Repositories are **per aggregate**; avoid generic repository; one SaveChanges per request.
Validation via **FluentValidation**; translate to **ProblemDetails**/ValidationProblem.
Cross-feature reads via dedicated read services/projections (ports), not other handlers.
