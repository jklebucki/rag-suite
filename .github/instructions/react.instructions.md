---
applyTo: "**/*.ts,**/*.tsx"
---

Use React 18 with function components and hooks.
Keep components presentational; move data fetching and mutations to services/hooks.
Use fetch/axios wrappers with centralized error handling; align request/response DTOs to backend feature endpoints.
Prefer co-located feature folders for UI matching backend features.
TypeScript: enable strict typing; avoid any; prefer explicit interfaces for props and API models.
State/data: prefer React Query (or equivalent) for caching, loading/error states, and retries.
Routing: keep route groups consistent with backend route groups (/api/{feature}).
Auth: include JWT in requests; handle 401 by redirecting to login.
