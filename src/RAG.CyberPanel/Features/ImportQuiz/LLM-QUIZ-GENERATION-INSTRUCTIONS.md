# Quiz Generation Instructions for LLM

## Objective
Generate a valid JSON file for quiz import that complies with all validation rules defined in `ImportQuizValidator.cs`.

## Critical Validation Rules

### Quiz Level Constraints
1. **Title** (REQUIRED)
   - Must not be empty
   - Maximum length: 200 characters
   - Type: string

2. **Description** (OPTIONAL)
   - Maximum length: 1000 characters when provided
   - Type: string | null

3. **IsPublished** (REQUIRED)
   - Type: boolean
   - Values: true or false

4. **Questions** (REQUIRED)
   - Minimum: 1 question
   - Maximum: 100 questions
   - Type: array of QuestionDto

5. **CreateNew** (REQUIRED)
   - Type: boolean
   - Default: true (create new quiz)
   - false = overwrite existing quiz (requires OverwriteQuizId)

6. **OverwriteQuizId** (CONDITIONAL)
   - Required when CreateNew = false
   - Must be null when CreateNew = true
   - Type: Guid string (e.g., "550e8400-e29b-41d4-a716-446655440000") | null

### Question Level Constraints
1. **Text** (REQUIRED)
   - Must not be empty
   - Maximum length: 1000 characters
   - Type: string

2. **ImageUrl** (OPTIONAL)
   - Maximum length: 100,000 characters (supports base64 up to ~100KB)
   - Can be:
     - null
     - URL string (e.g., "https://example.com/image.png")
     - Data URI with base64 (e.g., "data:image/png;base64,iVBORw0KGgo...")
   - Type: string | null

3. **Points** (REQUIRED)
   - Minimum: 1
   - Maximum: 100
   - Type: integer

4. **Options** (REQUIRED)
   - Minimum: 2 options per question
   - Maximum: 10 options per question
   - At least 1 option must have isCorrect = true
   - Type: array of OptionDto

### Option Level Constraints
1. **Text** (REQUIRED)
   - Must not be empty
   - Maximum length: 500 characters
   - Type: string

2. **ImageUrl** (OPTIONAL)
   - Maximum length: 100,000 characters
   - Same rules as Question ImageUrl
   - Type: string | null

3. **IsCorrect** (REQUIRED)
   - Type: boolean
   - At least one option per question must be true

## JSON Structure Template

```json
{
  "title": "Quiz Title Here (max 200 chars)",
  "description": "Optional description here (max 1000 chars)",
  "isPublished": true,
  "createNew": true,
  "overwriteQuizId": null,
  "questions": [
    {
      "text": "Question text here (max 1000 chars)",
      "imageUrl": null,
      "points": 10,
      "options": [
        {
          "text": "Option text (max 500 chars)",
          "imageUrl": null,
          "isCorrect": false
        },
        {
          "text": "Correct answer text",
          "imageUrl": null,
          "isCorrect": true
        }
      ]
    }
  ]
}
```

## Common Validation Errors to Avoid

❌ **DO NOT:**
- Leave title empty
- Include more than 100 questions
- Have questions with less than 2 options
- Have questions with more than 10 options
- Have questions with NO correct answer (all isCorrect = false)
- Exceed character limits (title: 200, description: 1000, question: 1000, option: 500)
- Set points to 0 or negative numbers
- Set points above 100
- Include base64 images larger than ~100KB
- Set overwriteQuizId when createNew is true
- Leave overwriteQuizId null when createNew is false

✅ **DO:**
- Always include at least one correct answer per question
- Keep all text within character limits
- Use between 2-10 options per question
- Set points between 1-100
- Use null for optional fields when not providing data
- Test JSON validity before import
- Use proper Guid format for overwriteQuizId when overwriting

## Base64 Image Support

If including images as base64:
```json
{
  "imageUrl": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
}
```

Keep base64 strings under 100KB (approximately 75KB of original image data).

## Example Valid Quiz

See `sample-cybersecurity-quiz.json` for a complete working example with 10 questions covering cybersecurity topics.

## Testing Your Quiz JSON

Before importing, verify:
1. ✅ JSON is valid (use JSON validator)
2. ✅ All required fields are present
3. ✅ No character limits exceeded
4. ✅ Each question has at least one correct answer
5. ✅ Questions have 2-10 options each
6. ✅ Points are between 1-100
7. ✅ CreateNew/OverwriteQuizId logic is correct

## Import Endpoint

```http
POST /api/cyberpanel/quizzes/import
Content-Type: application/json

{
  // Your quiz JSON here
}
```

**Expected Responses:**
- `201 Created` - Quiz successfully imported
- `400 Bad Request` - JSON format error
- `422 Unprocessable Entity` - Validation failed (check error details)
- `404 Not Found` - Quiz to overwrite not found (when createNew = false)

## Validation Error Response Example

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Quiz import validation failed",
  "status": 422,
  "errors": {
    "Title": ["Quiz title is required"],
    "Questions[0].Options": ["Question must have at least 2 options"],
    "Questions[1].Text": ["Question text cannot exceed 1000 characters"]
  }
}
```

---

**Note:** This validator ensures data integrity and prevents common security issues like excessively large payloads or malformed data. Always validate your JSON against these rules before attempting import.
