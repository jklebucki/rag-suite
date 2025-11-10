export interface SubmitFeedbackRequest {
  subject: string
  message: string
  attachments: FeedbackAttachmentUpload[]
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
  responseViewedAt?: string | null
  attachments: FeedbackAttachment[]
}

export interface FeedbackFilters {
  from?: string
  to?: string
  subject?: string
  userId?: string
  userEmail?: string
}

export interface FeedbackAttachment {
  id: string
  fileName: string
  contentType: string
  dataBase64: string
}

export interface FeedbackAttachmentUpload {
  fileName: string
  contentType: string
  dataBase64: string
}

