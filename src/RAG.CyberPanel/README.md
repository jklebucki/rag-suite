# RAG.CyberPanel

**Cybersecurity Quiz Engine** for the RAG Suite project.

## Overview

CyberPanel is a feature module that provides a complete quiz system for cybersecurity training and assessment. It supports:

- ✅ Quiz creation and management
- ✅ Multiple-choice questions with scoring
- ✅ Image support for questions and answers
- ✅ Quiz attempts tracking and scoring
- ✅ Vertical Slice Architecture
- ✅ Full validation with FluentValidation
- ✅ TypedResults and OpenAPI support

## Key Features

### Image Support
Both questions and answer options can include images:
- Question images: Screenshots of attacks, network diagrams, configuration examples
- Answer option images: Visual choices for identification questions
- Images are referenced by URL (hosted externally or via CDN)

### Scoring System
- Each question has configurable points
- Multiple correct answers supported
- Partial credit: All correct options must be selected
- Detailed per-question results returned

### Architecture
Follows **Vertical Slice Architecture**:
```
Features/
├── CreateQuiz/
│   ├── CreateQuizEndpoint.cs    - HTTP endpoint
│   ├── CreateQuizHandler.cs     - Business logic
│   ├── CreateQuizRequest.cs     - DTOs
│   └── CreateQuizValidator.cs   - Validation rules
├── GetQuiz/
├── ListQuizzes/
└── SubmitAttempt/
```

## API Endpoints

All endpoints are under `/api/cyberpanel/quizzes` and require authentication.

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | List all quizzes |
| GET | `/{id}` | Get quiz details |
| POST | `/` | Create new quiz |
| POST | `/{quizId}/attempts` | Submit quiz attempt |

## Data Model

### Quiz
- Title, Description
- Published status
- Collection of Questions

### Question
- Text (required)
- ImageUrl (optional) - **NEW**
- Points (default: 1)
- Order
- Collection of Options

### Option
- Text (required)
- ImageUrl (optional) - **NEW**
- IsCorrect flag

## Database

Uses PostgreSQL with Entity Framework Core:
- Connection string: `SecurityDatabase` from configuration
- Migrations managed via EF Core CLI
- Design-time factory for migrations

### Latest Migration
`AddImageUrlSupport` - Adds `ImageUrl` columns to Questions and Options tables.

## Integration

This project is registered in `RAG.Orchestrator.Api`:

```csharp
// In Program.cs
builder.Services.AddCyberPanel(builder.Configuration);
app.MapCyberPanelEndpoints();
await app.Services.EnsureCyberPanelDatabaseCreatedAsync();
```

## Usage Example

See `Features/README.md` for detailed API examples and request/response schemas.

Quick example:
```bash
# Create a quiz
curl -X POST https://api.example.com/api/cyberpanel/quizzes \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Phishing Detection Quiz",
    "description": "Learn to identify phishing attacks",
    "isPublished": true,
    "questions": [{
      "text": "Is this email legitimate?",
      "imageUrl": "https://example.com/email-screenshot.png",
      "points": 2,
      "options": [
        {"text": "Yes, it's safe", "isCorrect": false},
        {"text": "No, it's phishing", "isCorrect": true}
      ]
    }]
  }'
```

## Development

### Adding Migrations
```bash
cd src/RAG.CyberPanel
dotnet ef migrations add YourMigrationName --context CyberPanelDbContext
```

### Running Tests
```bash
dotnet test ../RAG.CyberPanel.Tests
```

## Dependencies

- EF Core + Npgsql (PostgreSQL)
- FluentValidation
- ASP.NET Core Minimal APIs
- RAG.Security (for user context)
- RAG.Abstractions
