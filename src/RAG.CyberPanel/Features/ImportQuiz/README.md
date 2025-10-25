# Quiz Import Feature - Sample Files

This directory contains sample files and documentation for the Quiz Import functionality.

## Files Overview

### 1. `sample-cybersecurity-quiz.json`
A complete, ready-to-import quiz example covering cybersecurity fundamentals.

**Content:**
- 10 questions about cybersecurity topics
- Mix of single-answer and multiple-answer questions
- Points ranging from 5 to 15 per question
- Total: 110 points possible
- Topics covered: phishing, passwords, HTTPS, malware, 2FA, firewalls, SFTP, SQL injection, VPN, security best practices

**Usage:**
```bash
# Import via curl
curl -X POST http://localhost:5000/api/cyberpanel/quizzes/import \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d @sample-cybersecurity-quiz.json

# Expected response: 201 Created
{
  "quizId": "guid-here",
  "title": "Cybersecurity Fundamentals Quiz",
  "questionsImported": 10,
  "optionsImported": 50,
  "wasOverwritten": false,
  "importedAt": "2025-10-25T12:00:00Z"
}
```

### 2. `LLM-QUIZ-GENERATION-INSTRUCTIONS.md`
Comprehensive English instructions for LLMs to generate valid quiz JSON files.

**Contains:**
- Complete validation rules from `ImportQuizValidator.cs`
- Character limits for all fields
- Required vs optional field specifications
- Common validation errors to avoid
- JSON structure template
- Base64 image support guidelines
- Testing checklist

**Use this when:** You want an LLM to generate a new quiz JSON that will pass validation.

### 3. `INSTRUKCJE-GENEROWANIA-QUIZOW-PL.md`
Polish version of the LLM quiz generation instructions.

**Use this when:** You want an LLM to generate a quiz in Polish or when working with Polish-speaking teams.

## Validation Rules Summary

| Field | Type | Min | Max | Required | Notes |
|-------|------|-----|-----|----------|-------|
| title | string | 1 | 200 | ✅ | Quiz title |
| description | string | - | 1000 | ❌ | Optional description |
| isPublished | boolean | - | - | ✅ | Publish status |
| createNew | boolean | - | - | ✅ | true = new quiz, false = overwrite |
| overwriteQuizId | Guid? | - | - | ⚠️ | Required if createNew=false |
| questions | array | 1 | 100 | ✅ | Quiz questions |
| question.text | string | 1 | 1000 | ✅ | Question text |
| question.imageUrl | string? | - | 100000 | ❌ | Image URL or base64 |
| question.points | int | 1 | 100 | ✅ | Points for question |
| question.options | array | 2 | 10 | ✅ | Answer options |
| option.text | string | 1 | 500 | ✅ | Option text |
| option.imageUrl | string? | - | 100000 | ❌ | Image URL or base64 |
| option.isCorrect | boolean | - | - | ✅ | At least 1 per question must be true |

## Creating Your Own Quiz

### Method 1: Manual JSON Creation
1. Copy `sample-cybersecurity-quiz.json`
2. Modify title, description, questions
3. Ensure all validation rules are met
4. Test with JSON validator
5. Import via API

### Method 2: LLM Generation
1. Provide the LLM with `LLM-QUIZ-GENERATION-INSTRUCTIONS.md`
2. Ask it to generate a quiz on your desired topic
3. Review the generated JSON
4. Import via API

### Method 3: Export and Modify
1. Create a quiz via the UI
2. Export it using `/api/cyberpanel/quizzes/{id}/export`
3. Modify the exported JSON
4. Import as new quiz or overwrite existing one

## Testing Imports

### Valid Import (Create New)
```json
{
  "title": "Test Quiz",
  "description": "A test",
  "isPublished": false,
  "createNew": true,
  "overwriteQuizId": null,
  "questions": [...]
}
```

### Valid Import (Overwrite Existing)
```json
{
  "title": "Updated Quiz",
  "description": "Updated version",
  "isPublished": true,
  "createNew": false,
  "overwriteQuizId": "550e8400-e29b-41d4-a716-446655440000",
  "questions": [...]
}
```

### Common Validation Errors

**Error:** "Quiz must have at least one question"
```json
"questions": []  // ❌ Empty array
```

**Error:** "Question must have at least 2 options"
```json
"options": [
  { "text": "Only one", "isCorrect": true }  // ❌ Need at least 2
]
```

**Error:** "Question must have at least one correct answer"
```json
"options": [
  { "text": "A", "isCorrect": false },  // ❌ All false
  { "text": "B", "isCorrect": false }
]
```

## Image Support

Both questions and options support images via:

1. **External URLs:**
```json
"imageUrl": "https://example.com/diagram.png"
```

2. **Base64 Data URIs (up to ~100KB):**
```json
"imageUrl": "data:image/png;base64,iVBORw0KGgoAAAANSUhEU..."
```

3. **No Image:**
```json
"imageUrl": null
```

## API Endpoints

### Import Quiz
```
POST /api/cyberpanel/quizzes/import
Content-Type: application/json
Authorization: Bearer {token}
```

### Export Quiz
```
GET /api/cyberpanel/quizzes/{quizId}/export
Authorization: Bearer {token}
```

### List Quizzes
```
GET /api/cyberpanel/quizzes
Authorization: Bearer {token}
```

### Get Quiz
```
GET /api/cyberpanel/quizzes/{quizId}
Authorization: Bearer {token}
```

## Related Files

- **Validator:** `../ImportQuizValidator.cs` - All validation logic
- **Handler:** `../ImportQuizHandler.cs` - Import processing logic
- **Endpoint:** `../ImportQuizEndpoint.cs` - API endpoint definition
- **Request DTOs:** `../ImportQuizRequest.cs` - Data transfer objects

## Support

For issues or questions about quiz import:
1. Check validation rules in the instructions files
2. Verify JSON structure matches the template
3. Review error messages from API responses
4. Consult `ImportQuizValidator.cs` for exact validation logic
