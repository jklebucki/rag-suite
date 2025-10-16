# CyberPanel Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         RAG.Orchestrator.Api                            │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────┐     │
│  │  Program.cs                                                   │     │
│  │  • builder.Services.AddCyberPanel(configuration)             │     │
│  │  • app.MapCyberPanelEndpoints()                              │     │
│  │  • await app.Services.EnsureCyberPanelDatabaseCreatedAsync() │     │
│  └──────────────────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ Uses
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                          RAG.CyberPanel                                 │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────┐     │
│  │  Endpoints/CyberPanelEndpoints.cs                            │     │
│  │  • MapGroup("/api/cyberpanel/quizzes")                       │     │
│  │  • Maps all feature endpoints                                │     │
│  └──────────────────────────────────────────────────────────────┘     │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐  │
│  │  Features/ (Vertical Slice Architecture)                       │  │
│  │                                                                 │  │
│  │  ┌─────────────────┐  ┌─────────────────┐                     │  │
│  │  │  ListQuizzes/   │  │  GetQuiz/       │                     │  │
│  │  │  • Endpoint     │  │  • Endpoint     │                     │  │
│  │  │  • Service      │  │  • Service      │                     │  │
│  │  │  • Models       │  │  • Models       │                     │  │
│  │  └─────────────────┘  └─────────────────┘                     │  │
│  │                                                                 │  │
│  │  ┌─────────────────┐  ┌─────────────────┐                     │  │
│  │  │  CreateQuiz/    │  │  SubmitAttempt/ │                     │  │
│  │  │  • Endpoint     │  │  • Endpoint     │                     │  │
│  │  │  • Handler      │  │  • Handler      │                     │  │
│  │  │  • Models       │  │  • Models       │                     │  │
│  │  │  • Validator    │  │  • Validator    │                     │  │
│  │  └─────────────────┘  └─────────────────┘                     │  │
│  └─────────────────────────────────────────────────────────────────┘  │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────┐     │
│  │  Domain/ (Entities)                                          │     │
│  │  • Quiz                                                       │     │
│  │  • Question (with ImageUrl 🖼️)                               │     │
│  │  • Option (with ImageUrl 🖼️)                                 │     │
│  │  • QuizAttempt                                                │     │
│  │  • QuizAnswer                                                 │     │
│  │  • QuizAnswerOption                                           │     │
│  └──────────────────────────────────────────────────────────────┘     │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────┐     │
│  │  Data/                                                        │     │
│  │  • CyberPanelDbContext (EF Core + PostgreSQL)                │     │
│  │  • CyberPanelDbContextFactory (for migrations)               │     │
│  └──────────────────────────────────────────────────────────────┘     │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────┐     │
│  │  Migrations/                                                  │     │
│  │  • 20250926193742_InitCyberPanelEntities                     │     │
│  │  • 20251016125517_AddImageUrlSupport ⭐ NEW                  │     │
│  └──────────────────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                         API Flow Example                                │
└─────────────────────────────────────────────────────────────────────────┘

Client Request:
POST /api/cyberpanel/quizzes
{
  "title": "Phishing Detection",
  "questions": [{
    "text": "Is this legitimate?",
    "imageUrl": "https://example.com/email.png",  ⬅️ IMAGE SUPPORT
    "points": 3,
    "options": [
      {
        "text": "Yes, safe",
        "imageUrl": null,
        "isCorrect": false
      },
      {
        "text": "No, phishing",
        "imageUrl": "https://example.com/warning.png",  ⬅️ IMAGE SUPPORT
        "isCorrect": true
      }
    ]
  }]
}
        │
        ▼
CyberPanelEndpoints.MapCreateQuiz()
        │
        ▼
CreateQuizEndpoint (validation via FluentValidation)
        │
        ├─ Validates Title, Questions
        ├─ Validates ImageUrl format (absolute URL)
        └─ Validates at least 2 options per question
        │
        ▼
CreateQuizHandler.Handle()
        │
        ├─ Gets current user from IUserContextService
        ├─ Creates Quiz entity
        ├─ Creates Question entities (with ImageUrl)
        ├─ Creates Option entities (with ImageUrl)
        └─ Saves to database via DbContext
        │
        ▼
Returns: TypedResults.Created() with quiz details

┌─────────────────────────────────────────────────────────────────────────┐
│                         Key Features                                    │
└─────────────────────────────────────────────────────────────────────────┘

✅ Image Support
   • Questions can have images (diagrams, screenshots)
   • Answer options can have images (visual choices)
   • URLs validated as absolute URIs
   • Optional (nullable) - backward compatible

✅ Vertical Slice Architecture
   • Each feature is self-contained
   • No cross-feature dependencies
   • Clear separation of concerns

✅ Validation
   • FluentValidation for all inputs
   • RFC7807 ProblemDetails for errors
   • Clear, actionable error messages

✅ Security
   • All endpoints require authorization
   • IsCorrect flag hidden from quiz takers
   • User tracking via IUserContextService

✅ Scoring
   • Configurable points per question
   • Multiple correct answers supported
   • Detailed per-question results
