# CyberPanel Quiz System - Testing Guide

## Overview
This guide provides comprehensive testing scenarios for the CyberPanel quiz system with export/import functionality and image support (base64).

## Prerequisites

### Backend Setup
1. Ensure PostgreSQL is running
2. Apply database migrations:
   ```bash
   cd src/RAG.Orchestrator.Api
   dotnet ef database update --context CyberPanelDbContext
   ```
3. Start the API:
   ```bash
   dotnet run --project src/RAG.Orchestrator.Api/RAG.Orchestrator.Api.csproj
   ```

### Frontend Setup
1. Install dependencies:
   ```bash
   cd src/RAG.Web.UI
   npm install
   ```
2. Start development server:
   ```bash
   npm run dev
   ```
3. Open browser at: http://localhost:5173

### Authentication
- Login with valid credentials to access CyberPanel
- Navigate to: `/cyberpanel/quizzes`

---

## Test Scenarios

### Scenario 1: Create Quiz Without Images

**Objective:** Verify basic quiz creation functionality

**Steps:**
1. Click "New Quiz" button
2. Fill in quiz details:
   - Title: "Basic Security Quiz"
   - Description: "Test your basic security knowledge"
   - Check "Publish Quiz" checkbox
3. Add first question:
   - Click "Add Question"
   - Question text: "What does HTTPS stand for?"
   - Points: 10
   - Add 3 options:
     * "HyperText Transfer Protocol Secure" (mark as correct)
     * "HyperText Transport Protocol Safe"
     * "HyperText Transfer Protection System"
4. Add second question:
   - Question text: "Which port is used for HTTPS?"
   - Points: 5
   - Add 3 options:
     * "80"
     * "443" (mark as correct)
     * "8080"
5. Click "Save Quiz"

**Expected Results:**
- ✅ Success toast notification appears
- ✅ Redirected to quiz list
- ✅ New quiz appears in the list
- ✅ Quiz shows 2 questions
- ✅ Status shows "Published"

---

### Scenario 2: Create Quiz With Images (Base64)

**Objective:** Verify image upload and base64 conversion

**Steps:**
1. Click "New Quiz"
2. Fill in quiz details:
   - Title: "Phishing Detection Quiz"
   - Description: "Can you spot phishing attempts?"
3. Add question with image:
   - Question text: "Is this email legitimate or phishing?"
   - Click "Upload Image" for question
   - Select a screenshot (< 100KB)
   - Points: 15
4. Add options with images:
   - Option 1: "Legitimate email"
     * Upload image of legitimate email
   - Option 2: "Phishing attempt" (mark as correct)
     * Upload image showing phishing indicators
5. Preview the quiz before saving
6. Save quiz

**Expected Results:**
- ✅ Images appear in preview mode
- ✅ Images are displayed correctly in quiz list
- ✅ Images load without errors
- ✅ Image file size validation works (reject > 100KB)

**Validation:**
- Check browser DevTools Network tab - images should be data URIs (data:image/png;base64,...)
- Verify no separate image requests are made

---

### Scenario 3: Edit Existing Quiz

**Objective:** Verify quiz editing functionality

**Steps:**
1. From quiz list, click "Edit" button on existing quiz
2. Modify title: Add " (Updated)" suffix
3. Add a new question
4. Remove an existing question (if multiple exist)
5. Change points value on a question
6. Add new option to a question
7. Mark different option as correct
8. Save changes

**Expected Results:**
- ✅ All existing data loads correctly in edit mode
- ✅ Images are preserved and displayed
- ✅ Changes are saved successfully
- ✅ Updated quiz reflects all changes in list view

---

### Scenario 4: Export Quiz to JSON

**Objective:** Verify export functionality and JSON structure

**Steps:**
1. Click "Export" button (download icon) on a quiz
2. Verify file downloads
3. Open downloaded JSON file in text editor

**Expected Results:**
- ✅ File downloads with name: `quiz_title_YYYY-MM-DD.json`
- ✅ JSON is valid and properly formatted
- ✅ JSON includes:
  ```json
  {
    "id": "guid",
    "title": "Quiz Title",
    "description": "Description",
    "isPublished": true,
    "questions": [...],
    "exportVersion": "1.0",
    "exportedAt": "ISO-8601 timestamp"
  }
  ```
- ✅ Each question includes all fields (text, imageUrl, points, options)
- ✅ Each option includes correct answer flag
- ✅ Images are exported as base64 data URIs (if present)
- ✅ Base64 strings start with `data:image/png;base64,` or `data:image/jpeg;base64,`

**Sample structure to verify:**
```json
{
  "questions": [{
    "text": "Question text",
    "imageUrl": "data:image/png;base64,iVBORw0KGgo...",
    "points": 10,
    "options": [{
      "text": "Option text",
      "imageUrl": "data:image/jpeg;base64,/9j/4AAQ...",
      "isCorrect": true
    }]
  }]
}
```

---

### Scenario 5: Import Quiz from JSON

**Objective:** Verify import functionality

**Steps:**
1. Export an existing quiz (Scenario 4)
2. Click "Import Quiz" button
3. Select the exported JSON file
4. Wait for import to complete

**Expected Results:**
- ✅ Success toast notification appears
- ✅ New quiz appears in list (duplicate of original)
- ✅ Imported quiz has same title with "(Imported)" or similar suffix
- ✅ All questions imported correctly
- ✅ All options imported correctly
- ✅ Correct answer flags preserved
- ✅ Images displayed correctly (base64 converted back)
- ✅ Points values preserved

**Advanced Test:**
1. Edit exported JSON manually:
   - Change title
   - Modify question text
   - Add new question
2. Import modified JSON
3. Verify all changes applied

---

### Scenario 6: Import Sample Quiz

**Objective:** Test with provided sample quiz

**Steps:**
1. Navigate to: `src/RAG.CyberPanel/Features/ImportQuiz/`
2. Copy content from `sample-cybersecurity-quiz.json`
3. Save as new file locally
4. Import using "Import Quiz" button

**Expected Results:**
- ✅ Sample quiz imported successfully
- ✅ 10 questions imported
- ✅ All 43 options imported
- ✅ Total points: 110
- ✅ Quiz is functional and can be taken

---

### Scenario 7: Take Quiz (Quiz Taker Flow)

**Objective:** Verify quiz taking functionality

**Steps:**
1. Navigate to quiz detail page (click on quiz title or view icon)
2. Review quiz intro screen:
   - Read description
   - Check question count
   - Check total points
3. Click "Start Quiz"
4. Answer all questions:
   - Select options by clicking checkboxes
   - Verify images display correctly
   - Review question points
5. Click "Submit Answers"
6. Review results screen

**Expected Results:**
- ✅ Intro screen shows correct information
- ✅ All questions display in order
- ✅ Images render correctly (including base64)
- ✅ Option selection works (visual feedback)
- ✅ Cannot submit without answering all questions
- ✅ Results screen shows:
  * Percentage score
  * Correct/incorrect count
  * Points earned/total
- ✅ Answer details show:
  * Which answers were correct/incorrect
  * Points earned per question
  * Visual indicators (✓/✗)
  * Highlighted selected answers
- ✅ "Retake Quiz" button works
- ✅ "Back to Quizzes" navigation works

---

### Scenario 8: Delete Quiz

**Objective:** Verify delete functionality

**Steps:**
1. Click "Delete" button (trash icon) on a quiz
2. Confirm deletion in modal
3. Verify quiz is removed from list

**Expected Results:**
- ✅ Confirmation modal appears
- ✅ Success toast after confirmation
- ✅ Quiz removed from list immediately
- ✅ Quiz cannot be accessed anymore

---

### Scenario 9: Validation Tests

**Objective:** Verify all validation rules

**Test Cases:**

#### 9.1 Quiz Builder Validation
1. Try to save quiz without title
   - Expected: Error message
2. Try to save quiz without questions
   - Expected: Validation error
3. Try to add question without text
   - Expected: Warning/error
4. Try to add option without text
   - Expected: Validation error
5. Try to add question with no correct answer
   - Expected: "At least one option must be marked as correct"
6. Try to upload image > 100KB
   - Expected: "Image is too large (max 100KB)"
7. Try to create question with < 2 options
   - Expected: Validation error

#### 9.2 Import Validation
1. Import invalid JSON file
   - Expected: Error toast
2. Import JSON missing required fields
   - Expected: Validation errors
3. Import JSON with invalid data types
   - Expected: Specific field errors

---

### Scenario 10: Image Handling Edge Cases

**Objective:** Test various image scenarios

**Steps:**
1. Upload different image formats:
   - PNG
   - JPEG
   - GIF (if supported)
2. Upload very small images (1KB)
3. Upload maximum size images (~100KB)
4. Upload images with special characters in filename
5. Remove uploaded image
6. Replace uploaded image

**Expected Results:**
- ✅ Common formats supported
- ✅ Size validation enforced
- ✅ Remove/replace operations work
- ✅ Images persist through export/import cycle
- ✅ Base64 encoding/decoding works correctly

---

### Scenario 11: Multi-User Scenarios

**Objective:** Test concurrent usage

**Steps:**
1. User A creates quiz
2. User B views quiz list (should see new quiz)
3. User A edits quiz
4. User B refreshes (should see updates)
5. User B takes quiz
6. User A exports quiz
7. User B imports same quiz

**Expected Results:**
- ✅ All operations work independently
- ✅ No data corruption
- ✅ List updates reflect changes

---

### Scenario 12: Performance Tests

**Objective:** Verify performance with larger quizzes

**Steps:**
1. Create quiz with 50 questions
2. Each question has 10 options
3. Add images to 20 questions
4. Save and measure time
5. Export quiz
6. Import quiz back
7. Take quiz (answer all questions)

**Expected Results:**
- ✅ No timeout errors
- ✅ Reasonable load times (< 5s for save/load)
- ✅ UI remains responsive
- ✅ Images load progressively
- ✅ Export/import completes successfully

---

## Regression Tests

### After Each Change, Verify:
1. ✅ Existing quizzes still load
2. ✅ Quiz list displays correctly
3. ✅ Create/Edit/Delete operations work
4. ✅ Export/Import functionality intact
5. ✅ Images display correctly
6. ✅ Quiz taking flow works
7. ✅ Validation rules enforced
8. ✅ Translations display correctly (EN/PL)

---

## Browser Compatibility

Test on:
- ✅ Chrome (latest)
- ✅ Firefox (latest)
- ✅ Safari (latest)
- ✅ Edge (latest)

**Check:**
- Image rendering
- File upload/download
- Modal dialogs
- Toast notifications
- Responsive design

---

## API Testing (Optional)

### Using HTTP client or curl:

#### List Quizzes
```bash
curl -X GET "http://localhost:5000/api/cyberpanel/quizzes" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

#### Get Quiz
```bash
curl -X GET "http://localhost:5000/api/cyberpanel/quizzes/{id}" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

#### Export Quiz
```bash
curl -X GET "http://localhost:5000/api/cyberpanel/quizzes/{id}/export" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

#### Import Quiz
```bash
curl -X POST "http://localhost:5000/api/cyberpanel/quizzes/import" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d @sample-quiz.json
```

#### Submit Quiz
```bash
curl -X POST "http://localhost:5000/api/cyberpanel/quizzes/submit" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "quizId": "guid",
    "answers": [
      {
        "questionId": "guid",
        "selectedOptionIds": ["guid1", "guid2"]
      }
    ]
  }'
```

---

## Known Issues / Limitations

Document any issues found during testing:

1. **Image Size Limit:** 100KB may be too restrictive for high-quality screenshots
2. **Browser Support:** Base64 images in very old browsers may not work
3. **Performance:** Very large quizzes (100+ questions) may need pagination
4. **Export File Size:** Quizzes with many images result in large JSON files

---

## Test Results Template

| Scenario | Status | Notes | Tester | Date |
|----------|--------|-------|--------|------|
| 1. Create Quiz (No Images) | ⏳ Pending | | | |
| 2. Create Quiz (With Images) | ⏳ Pending | | | |
| 3. Edit Quiz | ⏳ Pending | | | |
| 4. Export Quiz | ⏳ Pending | | | |
| 5. Import Quiz | ⏳ Pending | | | |
| 6. Import Sample | ⏳ Pending | | | |
| 7. Take Quiz | ⏳ Pending | | | |
| 8. Delete Quiz | ⏳ Pending | | | |
| 9. Validation | ⏳ Pending | | | |
| 10. Image Edge Cases | ⏳ Pending | | | |
| 11. Multi-User | ⏳ Pending | | | |
| 12. Performance | ⏳ Pending | | | |

Legend:
- ⏳ Pending
- ✅ Passed
- ❌ Failed
- ⚠️ Passed with issues

---

## Bug Report Template

When issues are found:

```markdown
### Bug Report

**Title:** Brief description

**Severity:** Critical / High / Medium / Low

**Steps to Reproduce:**
1. Step 1
2. Step 2
3. Step 3

**Expected Result:**
What should happen

**Actual Result:**
What actually happens

**Screenshots:**
If applicable

**Environment:**
- Browser: Chrome 120
- OS: macOS 14
- Backend Version: .NET 8.0
- Frontend Version: React 18

**Additional Context:**
Any other relevant information
```

---

## Checklist for Production Deployment

Before deploying to production:

- [ ] All test scenarios pass
- [ ] No console errors in browser
- [ ] No compilation warnings
- [ ] Database migrations applied
- [ ] API endpoints secured (authentication/authorization)
- [ ] CORS configured correctly
- [ ] Rate limiting in place for import/export
- [ ] File size limits enforced
- [ ] Input sanitization verified
- [ ] Error messages are user-friendly
- [ ] Translations complete (all languages)
- [ ] Performance tested with realistic data volumes
- [ ] Backup/restore procedures tested
- [ ] Monitoring and logging configured

---

## Support

For issues or questions:
- Check application logs
- Review API responses in browser DevTools
- Consult backend logs in console
- Review ARCHITECTURE.md and README.md files
