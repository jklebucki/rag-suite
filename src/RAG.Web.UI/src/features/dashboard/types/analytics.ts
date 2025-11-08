export interface PluginInfo {
  id: string
  name: string
  description: string
  version: string
  enabled: boolean
  capabilities: string[]
}

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

export interface ServiceStatus {
  name: string
  status: 'healthy' | 'warning' | 'error' | string
  message?: string
  details?: unknown
}

export interface SystemHealthResponse {
  api: ServiceStatus
  llm: ServiceStatus & { details?: { models?: string[] } }
  elasticsearch: ServiceStatus
  vectorStore: ServiceStatus
  timestamp: Date
}

