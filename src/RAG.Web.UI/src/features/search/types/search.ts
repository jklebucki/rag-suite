export interface SearchFilters {
  documentType?: string[]
  dateRange?: {
    from: Date
    to: Date
  }
  source?: string[]
}

export interface SearchQuery {
  query: string
  filters?: SearchFilters
  limit?: number
  offset?: number
}

export interface MultilingualSearchQuery {
  query: string
  language?: string
  resultLanguage?: string
  maxResults?: number
  enableCrossLanguageSearch?: boolean
  filters?: Record<string, unknown>
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
  [key: string]: unknown
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

