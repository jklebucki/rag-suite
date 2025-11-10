import { apiHttpClient } from '@/shared/services/api/httpClients'
import type {
  SubmitFeedbackRequest,
  FeedbackItem,
  FeedbackFilters,
  RespondFeedbackRequest
} from '@/features/feedback/types/feedback'

export async function submitFeedback(payload: SubmitFeedbackRequest): Promise<FeedbackItem> {
  const response = await apiHttpClient.post<FeedbackItem>('/feedback', {
    ...payload,
    attachments: payload.attachments ?? []
  })
  return response.data
}

export async function getFeedbackList(filters: FeedbackFilters = {}): Promise<FeedbackItem[]> {
  const params = new URLSearchParams()

  if (filters.from) params.set('from', filters.from)
  if (filters.to) params.set('to', filters.to)
  if (filters.subject) params.set('subject', filters.subject)
  if (filters.userId) params.set('userId', filters.userId)
  if ((filters as any).userEmail) params.set('userEmail', (filters as any).userEmail)

  const query = params.toString()
  const response = await apiHttpClient.get<FeedbackItem[]>(query ? `/feedback?${query}` : '/feedback')
  return response.data
}

export async function respondToFeedback(id: string, payload: RespondFeedbackRequest): Promise<FeedbackItem> {
  const response = await apiHttpClient.post<FeedbackItem>(`/feedback/${id}/response`, payload)
  return response.data
}

export async function deleteFeedback(id: string): Promise<void> {
  await apiHttpClient.delete(`/feedback/${id}`)
}

export async function getMyFeedback(): Promise<FeedbackItem[]> {
  const response = await apiHttpClient.get<FeedbackItem[]>('/feedback/mine')
  return response.data
}

export async function acknowledgeFeedbackResponse(id: string): Promise<FeedbackItem> {
  const response = await apiHttpClient.post<FeedbackItem>(`/feedback/${id}/acknowledge`)
  return response.data
}

const feedbackService = {
  submitFeedback,
  getFeedbackList,
  respondToFeedback,
  deleteFeedback,
  getMyFeedback,
  acknowledgeFeedbackResponse
}

export default feedbackService

