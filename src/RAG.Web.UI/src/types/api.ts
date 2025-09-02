// API Response Types
export interface ApiResponse<T> {
  data: T
  success: boolean
  message?: string
  errors?: string[]
}

// RAG Types
export interface SearchQuery {
  query: string
  filters?: SearchFilters
  limit?: number
  offset?: number
}

// Multilingual Search Types
export interface MultilingualSearchQuery {
  query: string
  language?: string
  resultLanguage?: string
  maxResults?: number
  enableCrossLanguageSearch?: boolean
  filters?: Record<string, any>
}

export interface SearchFilters {
  documentType?: string[]
  dateRange?: {
    from: Date
    to: Date
  }
  source?: string[]
}

export interface SearchResult {
  id: string
  title: string
  content: string
  score: number
  source: string
  documentType: string
  filePath?: string
  fileName?: string
  metadata: Record<string, any>
  createdAt: Date
  updatedAt: Date
  // Multilingual fields
  originalLanguage?: string
  wasTranslated?: boolean
  translationConfidence?: number
}

export interface SearchResponse {
  results: SearchResult[]
  total: number
  took: number
  query: string
  // Multilingual fields
  detectedLanguage?: string
  resultLanguage?: string
  processingTimeMs?: number
  usedCrossLanguageSearch?: boolean
}

// Document Detail Types
export interface DocumentDetailResponse {
  id: string
  title: string
  content: string
  fullContent?: string
  score: number
  source: string
  documentType: string
  metadata: Record<string, any>
  createdAt: Date
  updatedAt: Date
  relatedDocuments?: SearchResult[]
}

// Chat Types
export interface ChatMessage {
  id: string
  role: 'user' | 'assistant' | 'system'
  content: string
  timestamp: Date
  sources?: SearchResult[]
  metadata?: Record<string, any>
}

export interface ChatSession {
  id: string
  userId?: string // Optional for backwards compatibility with anonymous chat
  title: string
  messages: ChatMessage[]
  createdAt: Date
  updatedAt: Date
}

export interface ChatRequest {
  message: string
  sessionId?: string
  useRag?: boolean
  context?: string[]
}

// Multilingual Chat Types
export interface MultilingualChatRequest {
  message: string
  sessionId?: string
  language?: string
  responseLanguage?: string
  enableTranslation?: boolean
  metadata?: Record<string, any>
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
  metadata?: Record<string, any>
}

// Plugin Types
export interface PluginInfo {
  id: string
  name: string
  description: string
  version: string
  enabled: boolean
  capabilities: string[]
}

// Analytics Types
export interface UsageStats {
  totalQueries: number
  totalSessions: number
  avgResponseTime: number
  topQueries: string[]
  pluginUsage: Record<string, number>
}

export interface PerformanceMetrics {
  timestamp: Date
  responseTime: number
  activeSessions: number
  endpoint: string
}

// Health Types
export interface ServiceStatus {
  name: string
  status: 'healthy' | 'warning' | 'error' | string
  message?: string
  details?: any
}

export interface SystemHealthResponse {
  api: ServiceStatus
  llm: ServiceStatus & { details?: { models?: string[] } }
  elasticsearch: ServiceStatus
  vectorStore: ServiceStatus
  timestamp: Date
}
