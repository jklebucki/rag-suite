import type { SearchResult } from '@/features/search/types/search'

export interface ChatMessage {
  id: string
  role: 'user' | 'assistant' | 'system'
  content: string
  timestamp: Date
  sources?: SearchResult[]
  metadata?: Record<string, unknown>
}

export interface ChatSession {
  id: string
  userId?: string
  title: string
  messages: ChatMessage[]
  createdAt: Date
  updatedAt: Date
}

export interface ChatRequest {
  message: string
  sessionId?: string
  useRag?: boolean
  useDocumentSearch?: boolean
  context?: string[]
}

export interface MultilingualChatRequest {
  message: string
  sessionId?: string
  language?: string
  responseLanguage?: string
  enableTranslation?: boolean
  useDocumentSearch?: boolean
  metadata?: Record<string, unknown>
}

export interface MultilingualChatResponse {
  response: string
  sessionId?: string
  detectedLanguage: string
  responseLanguage: string
  wasTranslated: boolean
  translationConfidence?: number
  sources?: string[]
  processingTimeMs: number
  metadata?: Record<string, unknown>
}

