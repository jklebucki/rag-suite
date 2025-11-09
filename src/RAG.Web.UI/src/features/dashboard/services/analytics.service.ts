import type { ApiResponse } from '@/shared/types/api'
import type {
  PluginInfo,
  UsageStats,
  PerformanceMetrics,
  SystemHealthResponse,
  ElasticsearchStats,
  IndexStats,
  NodeStats,
  SearchStatistics,
  SystemHealth,
  DashboardData,
} from '@/features/dashboard/types/analytics'
import { apiHttpClient, healthHttpClient } from '@/shared/services/api/httpClients'

type RequestOptions = {
  signal?: AbortSignal
}

export async function getPlugins(options: RequestOptions = {}): Promise<PluginInfo[]> {
  const response = await apiHttpClient.get<ApiResponse<PluginInfo[]>>('/plugins', {
    signal: options.signal,
  })
  return response.data.data
}

export async function getPlugin(pluginId: string): Promise<PluginInfo> {
  const response = await apiHttpClient.get<ApiResponse<PluginInfo>>(`/plugins/${pluginId}`)
  return response.data.data
}

export async function togglePlugin(pluginId: string, enabled: boolean): Promise<void> {
  const endpoint = enabled ? `/plugins/${pluginId}/enable` : `/plugins/${pluginId}/disable`
  await apiHttpClient.post(endpoint)
}

export async function getUsageStats(
  filters?: {
    startDate?: Date
    endDate?: Date
    endpoint?: string
  },
  options: RequestOptions = {},
): Promise<UsageStats> {
  const params = new URLSearchParams()
  if (filters?.startDate) params.append('startDate', filters.startDate.toISOString())
  if (filters?.endDate) params.append('endDate', filters.endDate.toISOString())
  if (filters?.endpoint) params.append('endpoint', filters.endpoint)

  const response = await apiHttpClient.get<ApiResponse<UsageStats>>(`/analytics/usage?${params}`, {
    signal: options.signal,
  })
  return response.data.data
}

export async function getPerformanceMetrics(
  filters?: {
    startDate?: Date
    endDate?: Date
    endpoint?: string
  },
  options: RequestOptions = {},
): Promise<PerformanceMetrics[]> {
  const params = new URLSearchParams()
  if (filters?.startDate) params.append('startDate', filters.startDate.toISOString())
  if (filters?.endDate) params.append('endDate', filters.endDate.toISOString())
  if (filters?.endpoint) params.append('endpoint', filters.endpoint)

  const response = await apiHttpClient.get<ApiResponse<PerformanceMetrics[]>>(`/analytics/performance?${params}`, {
    signal: options.signal,
  })
  return response.data.data
}

export async function getElasticsearchClusterStats(options: RequestOptions = {}): Promise<ElasticsearchStats> {
  const response = await apiHttpClient.get<ApiResponse<ElasticsearchStats>>('/analytics/elasticsearch/cluster', {
    signal: options.signal,
  })
  return response.data.data
}

export async function getElasticsearchIndices(indexName?: string, options: RequestOptions = {}): Promise<IndexStats[]> {
  const url = indexName ? `/analytics/elasticsearch/indices/${indexName}` : '/analytics/elasticsearch/indices'
  const response = await apiHttpClient.get<ApiResponse<IndexStats[]>>(url, {
    signal: options.signal,
  })
  return response.data.data
}

export async function getElasticsearchNodes(options: RequestOptions = {}): Promise<NodeStats[]> {
  const response = await apiHttpClient.get<ApiResponse<NodeStats[]>>('/analytics/elasticsearch/nodes', {
    signal: options.signal,
  })
  return response.data.data
}

export async function getSearchStatistics(options: RequestOptions = {}): Promise<SearchStatistics> {
  const response = await apiHttpClient.get<ApiResponse<SearchStatistics>>('/analytics/search', {
    signal: options.signal,
  })
  return response.data.data
}

export async function getAnalyticsHealth(options: RequestOptions = {}): Promise<SystemHealth> {
  const response = await apiHttpClient.get<ApiResponse<SystemHealth>>('/analytics/health', {
    signal: options.signal,
  })
  return response.data.data
}

export async function getDashboardData(includeDetailedStats = false, options: RequestOptions = {}): Promise<DashboardData> {
  const params = new URLSearchParams()
  if (includeDetailedStats) params.append('includeDetailedStats', 'true')

  const response = await apiHttpClient.get<ApiResponse<DashboardData>>(`/analytics/dashboard?${params}`, {
    signal: options.signal,
  })
  return response.data.data
}

export async function getAnalyticsStatus(
  options: RequestOptions = {},
): Promise<{ status: string; services: Record<string, boolean> }> {
  const response = await apiHttpClient.get<ApiResponse<{ status: string; services: Record<string, boolean> }>>(
    '/analytics/status',
    {
      signal: options.signal,
    },
  )
  return response.data.data
}

export async function getSystemHealth(options: RequestOptions = {}): Promise<SystemHealthResponse> {
  const response = await healthHttpClient.get<ApiResponse<SystemHealthResponse>>('/healthz/system', {
    signal: options.signal,
  })
  return response.data.data
}

const analyticsService = {
  getPlugins,
  getPlugin,
  togglePlugin,
  getUsageStats,
  getPerformanceMetrics,
  getElasticsearchClusterStats,
  getElasticsearchIndices,
  getElasticsearchNodes,
  getSearchStatistics,
  getAnalyticsHealth,
  getDashboardData,
  getAnalyticsStatus,
  getSystemHealth,
}

export default analyticsService
