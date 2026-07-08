import { logger } from '@/utils/logger'
import { apiHttpClient } from '@/shared/services/api/httpClients'
import type { ApiResponse } from '@/shared/types/api'
import type {
  ChatRequest,
  ChatMessage,
  ChatAttachmentUploadResponse,
  ChatContextUsage,
  MultilingualChatRequest,
  MultilingualChatResponse,
  ChatSession,
} from '@/features/chat/types/chat'

type RequestOptions = {
  signal?: AbortSignal
}

export async function sendMessage(
  sessionId: string,
  request: ChatRequest,
  options: RequestOptions = {},
): Promise<ChatMessage> {
  const response = await apiHttpClient.post<ApiResponse<ChatMessage>>(
    `/user-chat/sessions/${sessionId}/messages`,
    request,
    {
      signal: options.signal,
    },
  )
  return response.data.data
}

export async function sendMultilingualMessage(
  sessionId: string,
  request: MultilingualChatRequest,
  options: RequestOptions = {},
): Promise<MultilingualChatResponse> {
  logger.debug(`Calling API: POST /api/user-chat/sessions/${sessionId}/messages/multilingual`, request)
  try {
    const response = await apiHttpClient.post<ApiResponse<MultilingualChatResponse>>(
      `/user-chat/sessions/${sessionId}/messages/multilingual`,
      request,
      {
        signal: options.signal,
      },
    )
    logger.debug('API response received:', response.data)
    return response.data.data
  } catch (error) {
    logger.error('API call failed:', error)
    throw error
  }
}

export async function getChatSessions(options: RequestOptions = {}): Promise<ChatSession[]> {
  const response = await apiHttpClient.get<ApiResponse<ChatSession[]>>('/user-chat/sessions', {
    signal: options.signal,
  })
  return response.data.data
}

export async function getChatSession(sessionId: string, options: RequestOptions = {}): Promise<ChatSession> {
  const response = await apiHttpClient.get<ApiResponse<ChatSession>>(`/user-chat/sessions/${sessionId}`, {
    signal: options.signal,
  })
  return response.data.data
}

export async function createChatSession(title?: string, language?: string): Promise<ChatSession> {
  const response = await apiHttpClient.post<ApiResponse<ChatSession>>('/user-chat/sessions', { title, language })
  return response.data.data
}

export async function deleteChatSession(sessionId: string): Promise<void> {
  await apiHttpClient.delete(`/user-chat/sessions/${sessionId}`)
}

export async function getChatContext(sessionId: string, options: RequestOptions = {}): Promise<ChatContextUsage> {
  const response = await apiHttpClient.get<ApiResponse<ChatContextUsage>>(
    `/user-chat/sessions/${sessionId}/context`,
    {
      signal: options.signal,
    },
  )
  return response.data.data
}

export async function uploadChatAttachments(
  sessionId: string,
  files: File[],
  options: RequestOptions = {},
): Promise<ChatAttachmentUploadResponse> {
  const formData = new FormData()
  files.forEach(file => formData.append('files', file))

  const response = await apiHttpClient.post<ApiResponse<ChatAttachmentUploadResponse>>(
    `/user-chat/sessions/${sessionId}/attachments`,
    formData,
    {
      signal: options.signal,
    },
  )
  return response.data.data
}

export async function deleteChatAttachment(sessionId: string, attachmentId: string): Promise<void> {
  await apiHttpClient.delete(`/user-chat/sessions/${sessionId}/attachments/${attachmentId}`)
}

const chatService = {
  sendMessage,
  sendMultilingualMessage,
  getChatSessions,
  getChatSession,
  createChatSession,
  deleteChatSession,
  getChatContext,
  uploadChatAttachments,
  deleteChatAttachment,
}

export default chatService
