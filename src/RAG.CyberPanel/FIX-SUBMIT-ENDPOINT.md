# Fix: Quiz Submit Endpoint 405 Error

## Problem
```
Failed to load resource: the server responded with a status of 405 (Method Not Allowed)
POST http://localhost:3000/api/cyberpanel/quizzes/submit
```

## Root Cause
**Frontend URL mismatch with Backend endpoint**

### Backend Endpoint (Correct):
```csharp
// src/RAG.CyberPanel/Features/SubmitAttempt/SubmitAttemptEndpoint.cs
group.MapPost("/{quizId:guid}/attempts", async (...)
```

Full path: `POST /api/cyberpanel/quizzes/{quizId}/attempts`

### Frontend Call (Incorrect - Before Fix):
```typescript
// src/RAG.Web.UI/src/services/api.ts
async submitQuizAttempt(request: SubmitAttemptRequest): Promise<SubmitAttemptResponse> {
  const response = await this.client.post('/cyberpanel/quizzes/submit', request)
  //                                                                   ^^^^^^ Wrong!
  return response.data
}
```

## Solution

### Fixed Frontend Code:
```typescript
// src/RAG.Web.UI/src/services/api.ts
async submitQuizAttempt(request: SubmitAttemptRequest): Promise<SubmitAttemptResponse> {
  const response = await this.client.post(`/cyberpanel/quizzes/${request.quizId}/attempts`, request)
  //                                                              ^^^^^^^^^^^^^^^^^^^^^^^^^ Correct!
  return response.data
}
```

## Changes Made
**File:** `src/RAG.Web.UI/src/services/api.ts`

**Line 358:** Changed from:
```typescript
const response = await this.client.post('/cyberpanel/quizzes/submit', request)
```

**To:**
```typescript
const response = await this.client.post(`/cyberpanel/quizzes/${request.quizId}/attempts`, request)
```

## Verification

### Backend Route Structure:
```
Base: /api/cyberpanel/quizzes
├── GET    /                     → ListQuizzes
├── GET    /{id}                 → GetQuiz
├── POST   /                     → CreateQuiz
├── PUT    /{id}                 → UpdateQuiz
├── DELETE /{id}                 → DeleteQuiz
├── GET    /{id}/export          → ExportQuiz
├── POST   /import               → ImportQuiz
└── POST   /{quizId}/attempts    → SubmitAttempt ✅
```

### Request Flow:
1. User completes quiz in `QuizDetail.tsx`
2. Clicks "Submit Answers"
3. `useQuizTaking` hook calls `submitAttempt()`
4. Calls `cyberPanelService.submitAttempt()`
5. Calls `apiClient.submitQuizAttempt()`
6. **Now correctly POSTs to:** `/api/cyberpanel/quizzes/{quizId}/attempts`

## Testing
After fix, quiz submission should work:
- ✅ Submit quiz without 405 error
- ✅ Receive results with score
- ✅ Display per-question feedback

## Status
✅ **Fixed** - HMR applied, ready for testing
