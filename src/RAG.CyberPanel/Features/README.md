# CyberPanel Features

This folder contains all CyberPanel features organized using **Vertical Slice Architecture**. Each feature is self-contained with its own:
- **Endpoint** - HTTP route handler
- **Service/Handler** - Business logic
- **Models** - Request/Response DTOs
- **Validator** - Input validation rules

## Features Overview

### 1. ListQuizzes
**Endpoint:** `GET /api/cyberpanel/quizzes`

Lists all available cybersecurity quizzes with basic information.

**Response:**
```json
{
  "quizzes": [
    {
      "id": "guid",
      "title": "Basic Cybersecurity",
      "description": "Test your knowledge...",
      "isPublished": true,
      "createdAt": "2025-10-16T12:00:00Z",
      "questionCount": 10
    }
  ]
}
```

### 2. GetQuiz
**Endpoint:** `GET /api/cyberpanel/quizzes/{id}`

Retrieves full quiz details with questions and options (without revealing correct answers).

**Response:**
```json
{
  "id": "guid",
  "title": "Basic Cybersecurity",
  "description": "Test your knowledge...",
  "isPublished": true,
  "questions": [
    {
      "id": "guid",
      "text": "What is phishing?",
      "imageUrl": "https://example.com/images/phishing-example.png",
      "points": 2,
      "options": [
        {
          "id": "guid",
          "text": "Social engineering attack",
          "imageUrl": null
        },
        {
          "id": "guid",
          "text": "Fishing for data",
          "imageUrl": "https://example.com/images/option2.png"
        }
      ]
    }
  ]
}
```

### 3. CreateQuiz
**Endpoint:** `POST /api/cyberpanel/quizzes`

Creates a new cybersecurity quiz with questions and options.

**Request:**
```json
{
  "title": "Advanced Cybersecurity",
  "description": "For experienced users",
  "isPublished": true,
  "questions": [
    {
      "text": "Identify the attack type in this screenshot:",
      "imageUrl": "https://example.com/images/attack-screenshot.png",
      "points": 3,
      "options": [
        {
          "text": "SQL Injection",
          "imageUrl": null,
          "isCorrect": true
        },
        {
          "text": "XSS Attack",
          "imageUrl": null,
          "isCorrect": false
        },
        {
          "text": "CSRF Attack",
          "imageUrl": "https://example.com/images/csrf-example.png",
          "isCorrect": false
        }
      ]
    }
  ]
}
```

**Response:**
```json
{
  "id": "guid",
  "title": "Advanced Cybersecurity",
  "isPublished": true
}
```

### 4. SubmitAttempt
**Endpoint:** `POST /api/cyberpanel/quizzes/{quizId}/attempts`

Submits quiz answers and receives scored results.

**Request:**
```json
{
  "quizId": "guid",
  "answers": [
    {
      "questionId": "guid",
      "selectedOptionIds": ["guid1", "guid2"]
    }
  ]
}
```

**Response:**
```json
{
  "attemptId": "guid",
  "score": 7,
  "maxScore": 10,
  "perQuestionResults": [
    {
      "questionId": "guid",
      "correct": true,
      "pointsAwarded": 2,
      "maxPoints": 2
    }
  ]
}
```

## Image Support

Both questions and answer options support optional image URLs:
- **Question images**: Diagrams, screenshots, network topology charts
- **Option images**: Visual answer choices (e.g., identifying malware screenshots)

Image URLs must be:
- Valid absolute URLs (http:// or https://)
- Accessible to clients
- Optional (can be null)

## Validation Rules

### CreateQuiz
- Title: Required, max 200 characters
- Questions: At least 1 required
- Each question: Text required, valid ImageUrl (if provided)
- Options: At least 2 per question
- At least 1 correct option per question
- Each option: Text required, valid ImageUrl (if provided)

### SubmitAttempt
- QuizId: Required, must exist
- Answers: Required
- Each answer: Valid questionId, at least 1 selected option

## Architecture Notes

Following **Vertical Slice Architecture**:
- Each feature is independent
- No cross-feature dependencies
- Services registered in `ServiceCollectionExtensions`
- Endpoints mapped in `CyberPanelEndpoints`
- Uses FluentValidation for input validation
- Returns TypedResults with proper HTTP status codes
- Implements RFC7807 ProblemDetails for errors
