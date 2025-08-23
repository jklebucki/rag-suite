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
  metadata: Record<string, any>
  createdAt: Date
  updatedAt: Date
}

export interface SearchResponse {
  results: SearchResult[]
  total: number
  took: number
  query: string
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
