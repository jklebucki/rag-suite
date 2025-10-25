# Manual Testing Checklist - Quiz UI Improvements

## Setup
```bash
# Terminal 1 - Backend
cd /Users/jklebucki/Projects/rag-suite
dotnet run --project src/RAG.Orchestrator.Api/RAG.Orchestrator.Api.csproj

# Terminal 2 - Frontend
cd /Users/jklebucki/Projects/rag-suite/src/RAG.Web.UI
npm run dev
```

Open browser: http://localhost:5173
Login and navigate to: **CyberPanel → Quizzes**

---

## Test 1: Quiz List - New Play Button ✅

### Steps:
1. Navigate to `/cyberpanel/quizzes`
2. Locate any existing quiz in the list
3. Verify button layout for each quiz

### Expected Result:
```
Each quiz should have 3 buttons in this order:
[▶ Play] [✏️ Edit] [🗑️ Delete]
```

### Verify:
- [ ] Play button has Play icon (▶)
- [ ] Play button is PRIMARY variant (blue)
- [ ] Play button tooltip shows "Take Quiz" (EN) or "Rozwiąż quiz" (PL)
- [ ] NO Export button visible (Download icon)
- [ ] NO Import button in header area

### Action:
Click the **Play button** on any quiz

### Expected Result:
- [ ] Navigates to `/cyberpanel/quizzes/{quizId}`
- [ ] QuizDetail component loads
- [ ] Shows quiz intro screen with:
  - Quiz title
  - Description
  - Question count
  - Total points
  - "Start Quiz" button

---

## Test 2: QuizBuilder - Import Button (New Quiz Mode)

### Steps:
1. From quiz list, click **"New Quiz"** button
2. QuizBuilder opens in CREATE mode

### Expected Result:
```
Button layout in header:
[Cancel] [Import] [Preview] [Save]
```

### Verify:
- [ ] Import button visible (Upload icon)
- [ ] NO Export button (should only show in edit mode)
- [ ] Cancel button present
- [ ] Preview button present
- [ ] Save button present

### Action:
Click **Import** button

### Expected Result:
- [ ] File picker dialog opens
- [ ] Only .json files accepted

### Test Import:
1. Select a valid quiz JSON file (use `sample-cybersecurity-quiz.json` from `src/RAG.CyberPanel/Features/ImportQuiz/`)
2. Click Open

### Expected Result:
- [ ] Success toast: "Quiz imported successfully"
- [ ] Quiz data loads into form:
  - Title filled
  - Description filled
  - Questions loaded with all options
  - Images displayed (if any)
  - Published status set

---

## Test 3: QuizBuilder - Export Button (Edit Mode)

### Steps:
1. From quiz list, click **Edit** button (pencil icon) on any quiz
2. QuizBuilder opens in EDIT mode

### Expected Result:
```
Button layout in header:
[Cancel] [Export] [Import] [Preview] [Save]
```

### Verify:
- [ ] Export button NOW visible (Download icon)
- [ ] Export button positioned between Cancel and Import
- [ ] Import button still visible
- [ ] All other buttons present

### Action:
Click **Export** button

### Expected Result:
- [ ] File download starts immediately
- [ ] Filename format: `quiz_title_YYYY-MM-DD.json`
- [ ] Success toast: "Quiz exported successfully"

### Verify Downloaded File:
1. Open the downloaded JSON in text editor
2. Check structure:
```json
{
  "id": "guid",
  "title": "Quiz Title",
  "description": "Description",
  "isPublished": true,
  "questions": [...],
  "exportVersion": "1.0",
  "exportedAt": "2025-10-25T..."
}
```

### Verify:
- [ ] All questions exported
- [ ] All options exported with isCorrect flags
- [ ] Images exported as base64 (if present)
- [ ] Valid JSON (no syntax errors)

---

## Test 4: Export → Import Round Trip

### Steps:
1. Edit an existing quiz with images
2. Click **Export** → save file
3. Make a small change (e.g., edit title)
4. Click **Save**
5. Go back to quiz list
6. Click **"New Quiz"**
7. Click **Import** → select the exported file

### Expected Result:
- [ ] Import success
- [ ] Quiz loaded with ALL original data:
  - Original title (not the changed one)
  - Original description
  - Original questions in same order
  - Original options with same isCorrect flags
  - Original images displayed correctly (base64)
  - Original points values

### Verify Images:
- [ ] Question images render correctly
- [ ] Option images render correctly
- [ ] No broken image icons
- [ ] Images are base64 data URIs (check browser DevTools Network tab - no image requests)

---

## Test 5: Navigation Flow

### Test 5A: List → Play → Quiz Taking
1. List page → Click Play
2. Quiz intro → Click "Start Quiz"
3. Answer questions → Click "Submit Answers"
4. Results page → Click "Back to Quizzes"

### Verify:
- [ ] Each step navigates correctly
- [ ] Can take quiz successfully
- [ ] Can return to list from results

### Test 5B: List → Edit → Export
1. List page → Click Edit
2. QuizBuilder → Click Export
3. File downloads

### Verify:
- [ ] Export works from edit mode
- [ ] Can continue editing after export
- [ ] No navigation changes

### Test 5C: List → New Quiz → Import
1. List page → Click "New Quiz"
2. QuizBuilder → Click Import
3. Select JSON file
4. Quiz loads

### Verify:
- [ ] Import works from new quiz mode
- [ ] Quiz data fills form
- [ ] Can modify and save as new quiz

---

## Test 6: Translation Verification

### English (EN)
Switch language to English, check:
- [ ] Play button tooltip: "Take Quiz"
- [ ] Export button: "Export Quiz"
- [ ] Import button: "Import Quiz"

### Polish (PL)
Switch language to Polish, check:
- [ ] Play button tooltip: "Rozwiąż quiz"
- [ ] Export button: "Eksportuj quiz"
- [ ] Import button: "Importuj quiz"

---

## Test 7: Error Handling

### Test 7A: Export without saving (new quiz)
1. Create new quiz
2. Fill in some data (don't save)
3. Click Export

### Expected Result:
- [ ] Error toast: "Please save the quiz first before exporting"
- [ ] No file download

### Test 7B: Import invalid JSON
1. Create a .txt file with random text
2. Rename to .json
3. Try to import

### Expected Result:
- [ ] Error toast: "Failed to import quiz"
- [ ] Form remains unchanged

### Test 7C: Import JSON with missing fields
1. Edit exported JSON, remove required field (e.g., "title")
2. Save and try to import

### Expected Result:
- [ ] Error toast with validation message
- [ ] Form remains unchanged

---

## Test 8: Responsive Design

### Desktop (1920x1080)
- [ ] All buttons visible and properly spaced
- [ ] No text overflow
- [ ] Icons properly sized

### Tablet (iPad - 768x1024)
- [ ] Buttons stack or wrap properly
- [ ] Touch targets adequate size
- [ ] All functions accessible

### Mobile (iPhone - 375x667)
- [ ] Buttons wrap to multiple rows if needed
- [ ] Icons and text readable
- [ ] No horizontal scroll

---

## Test 9: Regression Tests

### Existing Functionality Should Still Work:

#### Quiz Creation
- [ ] Can create new quiz from scratch
- [ ] Can add questions
- [ ] Can add options
- [ ] Can upload images
- [ ] Can set correct answers
- [ ] Can save quiz

#### Quiz Editing
- [ ] Can edit existing quiz
- [ ] Changes save correctly
- [ ] Images preserved
- [ ] Question order preserved

#### Quiz Taking
- [ ] Can start quiz
- [ ] Can select answers
- [ ] Can submit quiz
- [ ] Results display correctly
- [ ] Can retake quiz

#### Quiz Management
- [ ] Can delete quiz (with confirmation)
- [ ] Can publish/unpublish quiz
- [ ] Quiz list updates after changes

---

## Test 10: Performance

### Large Quiz (50+ questions)
1. Create or import quiz with 50 questions
2. Edit mode → Click Export

### Verify:
- [ ] Export completes in < 3 seconds
- [ ] File downloads successfully
- [ ] No browser freeze

### Import Large Quiz
1. Import the 50-question quiz JSON

### Verify:
- [ ] Import completes in < 5 seconds
- [ ] All questions load
- [ ] Form remains responsive
- [ ] Can scroll through questions smoothly

---

## Bug Report Template

If you find issues, document them:

```markdown
### Bug: [Brief Description]

**Component:** Quizzes.tsx / QuizBuilder.tsx / QuizDetail.tsx
**Severity:** Critical / High / Medium / Low

**Steps to Reproduce:**
1. 
2. 
3. 

**Expected Result:**


**Actual Result:**


**Browser:** Chrome/Firefox/Safari [Version]
**OS:** macOS/Windows/Linux
**Screenshot:** [Attach if helpful]

**Console Errors:**
```
[Paste any console errors]
```
```

---

## Success Criteria

All tests should pass with:
- ✅ No TypeScript compilation errors
- ✅ No runtime JavaScript errors
- ✅ No broken UI elements
- ✅ All navigation flows work
- ✅ Export/Import functionality works correctly
- ✅ Translations display correctly
- ✅ Images handle properly (base64)
- ✅ Responsive design works on all screen sizes

---

## Notes

- Tests should be performed in Chrome, Firefox, and Safari
- Clear browser cache if experiencing issues
- Check browser console for errors during testing
- Test with actual quiz data (not just sample data)

---

**Tester:** _________________
**Date:** _________________
**Build:** Frontend v1.0.0, Backend .NET 8
**Status:** ⏳ Pending / ✅ Passed / ❌ Failed
