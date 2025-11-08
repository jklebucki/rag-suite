import { logger } from '@/utils/logger'
import { apiHttpClient } from '@/shared/services/api/httpClients'
import type {
  ApiResponse,
  ChatRequest,
  ChatMessage,
  MultilingualChatRequest,
  MultilingualChatResponse,
  ChatSession,
} from '@/types'

export async function sendMessage(sessionId: string, request: ChatRequest): Promise<ChatMessage> {
  const response = await apiHttpClient.post<ApiResponse<ChatMessage>>(
    `/user-chat/sessions/${sessionId}/messages`,
    request
  )
  return response.data.data
}

export async function sendMultilingualMessage(
  sessionId: string,
  request: MultilingualChatRequest
): Promise<MultilingualChatResponse> {
  logger.debug(`Calling API: POST /api/user-chat/sessions/${sessionId}/messages/multilingual`, request)
  try {
    const response = await apiHttpClient.post<ApiResponse<MultilingualChatResponse>>(
      `/user-chat/sessions/${sessionId}/messages/multilingual`,
      request
    )
    logger.debug('API response received:', response.data)
    return response.data.data
  } catch (error) {
    logger.error('API call failed:', error)
    throw error
  }
}

export async function getChatSessions(): Promise<ChatSession[]> {
  const response = await apiHttpClient.get<ApiResponse<ChatSession[]>>('/user-chat/sessions')
  return response.data.data
}

export async function getChatSession(sessionId: string): Promise<ChatSession> {
  const response = await apiHttpClient.get<ApiResponse<ChatSession>>(`/user-chat/sessions/${sessionId}`)
  return response.data.data
}

export async function createChatSession(title?: string, language?: string): Promise<ChatSession> {
  const response = await apiHttpClient.post<ApiResponse<ChatSession>>('/user-chat/sessions', { title, language })
  return response.data.data
}

export async function deleteChatSession(sessionId: string): Promise<void> {
  await apiHttpClient.delete(`/user-chat/sessions/${sessionId}`)
}

const chatApi = {
  sendMessage,
  sendMultilingualMessage,
  getChatSessions,
  getChatSession,
  createChatSession,
  deleteChatSession,
}

export default chatApi
