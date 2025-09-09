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
  metadata: SearchResultMetadata
  createdAt: Date
  updatedAt: Date
  // Multilingual fields
  originalLanguage?: string
  wasTranslated?: boolean
  translationConfidence?: number
}

export interface SearchResultMetadata {
  category?: string
  score?: number
  index?: string
  chunksFound?: number
  totalChunks?: number
  reconstructed?: boolean
  highlights?: string
  // Backend compatibility fields
  chunk_count?: number
  total_chunks?: number
  // Additional metadata fields
  file_size?: string | number
  last_modified?: string
  indexed_at?: string
  file_path?: string
  source_file?: string
  file_extension?: string
  document_id?: string
  [key: string]: any
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
  filePath?: string
  fileName?: string
  metadata: SearchResultMetadata
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

// Enhanced Analytics Types for Elasticsearch Integration
export interface ElasticsearchStats {
  clusterName: string
  status: string
  numberOfNodes: number
  numberOfDataNodes: number
  activePrimaryShards: number
  activeShards: number
  unassignedShards: number
  activeShardsPercent: number
}

export interface IndexStats {
  indexName: string
  health: string
  status: string
  documentCount: number
  deletedDocuments: number
  storeSize: string
  storeSizeBytes: number
  indexTotal: number
  indexTimeInMillis: number
  searchTotal: number
  searchTimeInMillis: number
  getTotal: number
  getTimeInMillis: number
}

export interface NodeStats {
  nodeName: string
  nodeId: string
  roles: string[]
  jvmMemoryUsed: number
  jvmMemoryMax: number
  jvmMemoryPercent: number
  documentCount: number
  indexingCurrent: number
  searchCurrent: number
}

export interface SearchStatistics {
  totalSearches: number
  totalSearchTimeMs: number
  averageSearchTime: number
  searchesLast24h: number
  mostActiveIndex: string
  searchesByIndex: Record<string, number>
}

export interface SystemHealth {
  elasticsearchAvailable: boolean
  embeddingServiceAvailable: boolean
  llmServiceAvailable: boolean
  elasticsearchStats?: ElasticsearchStats
  indices: IndexStats[]
  nodes: NodeStats[]
  searchStats: SearchStatistics
}

export interface DashboardData {
  systemHealth: SystemHealth
  clusterStats: ElasticsearchStats
  recentMetrics: PerformanceMetrics[]
}

// Legacy Health Types (keeping for compatibility)
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
