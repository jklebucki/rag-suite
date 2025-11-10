export interface SubmitFeedbackRequest {
  subject: string
  message: string
}

export interface RespondFeedbackRequest {
  response: string
}

export interface FeedbackItem {
  id: string
  userId: string
  userEmail?: string | null
  subject: string
  message: string
  response?: string | null
  responseAuthorEmail?: string | null
  createdAt: string
  updatedAt: string
  respondedAt?: string | null
}

export interface FeedbackFilters {
  from?: string
  to?: string
  subject?: string
  userId?: string
}

