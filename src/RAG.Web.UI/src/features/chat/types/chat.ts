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

export interface ChatAttachmentDraft {
  id: string
  fileName: string
  contentType: string
  sizeBytes: number
  tokenCount: number
  uploadedAt: string
}

export interface ChatContextUsage {
  usedTokens: number
  limitTokens: number
  percentUsed: number
  isLimitExceeded: boolean
  attachmentTokens: number
  attachmentLimitTokens: number
  attachments: ChatAttachmentDraft[]
}

export interface ChatAttachmentUploadResponse {
  contextUsage: ChatContextUsage
}

export interface MultilingualChatRequest {
  message: string
  sessionId?: string
  language?: string
  responseLanguage?: string
  enableTranslation?: boolean
  useDocumentSearch?: boolean
  attachmentIds?: string[]
  metadata?: Record<string, unknown>
}

export interface MultilingualChatResponse {
  response: string
  sessionId?: string
  userMessageId?: string
  assistantMessageId?: string
  detectedLanguage: string
  responseLanguage: string
  wasTranslated: boolean
  translationConfidence?: number
  sources?: string[]
  processingTimeMs: number
  metadata?: Record<string, unknown>
}

