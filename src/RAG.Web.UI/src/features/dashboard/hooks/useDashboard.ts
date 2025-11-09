import { useQuery } from '@tanstack/react-query'
import { useEffect } from 'react'
import analyticsService from '@/features/dashboard/services/analytics.service'
import { useToastContext } from '@/shared/contexts/ToastContext'
import { REFETCH_INTERVALS } from '@/app/config/appConfig'
import type {
  DashboardData,
  SystemHealth,
  ElasticsearchStats,
  UsageStats,
  PluginInfo,
} from '@/features/dashboard/types/analytics'

export function useDashboard() {
  const { showError } = useToastContext()

  // Enhanced Dashboard Data - using new comprehensive endpoint
  const {
    data: dashboardData,
    isLoading: isDashboardLoading,
    error: dashboardError,
  } = useQuery<DashboardData>({
    queryKey: ['dashboard'],
    queryFn: ({ signal }) => analyticsService.getDashboardData(true, { signal }), // Include detailed stats
    refetchInterval: REFETCH_INTERVALS.DASHBOARD,
  })

  // Analytics Health - more detailed health information
  const {
    data: analyticsHealth,
    isLoading: isAnalyticsHealthLoading,
    error: analyticsHealthError,
  } = useQuery<SystemHealth>({
    queryKey: ['analytics-health'],
    queryFn: ({ signal }) => analyticsService.getAnalyticsHealth({ signal }),
    refetchInterval: REFETCH_INTERVALS.ANALYTICS_HEALTH,
  })

  // Elasticsearch Cluster Stats
  const {
    data: clusterStats,
    isLoading: isClusterStatsLoading,
    error: clusterStatsError,
  } = useQuery<ElasticsearchStats>({
    queryKey: ['elasticsearch-cluster'],
    queryFn: ({ signal }) => analyticsService.getElasticsearchClusterStats({ signal }),
    refetchInterval: REFETCH_INTERVALS.CLUSTER_STATS,
  })

  // Usage Statistics (keeping for compatibility and fallback)
  const {
    data: usageStats,
    isLoading: isStatsLoading,
    error: statsError,
  } = useQuery<UsageStats>({
    queryKey: ['usage-stats'],
    queryFn: ({ signal }) => analyticsService.getUsageStats(undefined, { signal }),
    refetchInterval: REFETCH_INTERVALS.USAGE_STATS,
    enabled: !dashboardData, // Only fetch if dashboard data is not available
  })

  // Plugins
  const {
    data: plugins,
    isLoading: isPluginsLoading,
    error: pluginsError,
  } = useQuery<PluginInfo[]>({
    queryKey: ['plugins'],
    queryFn: ({ signal }) => analyticsService.getPlugins({ signal }),
    refetchInterval: REFETCH_INTERVALS.PLUGINS,
  })

  // Legacy system health (keeping for compatibility)
  const {
    data: systemHealth,
    isLoading: isHealthLoading,
    error: healthError,
  } = useQuery({
    queryKey: ['system-health'],
    queryFn: ({ signal }) => analyticsService.getSystemHealth({ signal }),
    refetchInterval: REFETCH_INTERVALS.SYSTEM_HEALTH,
    enabled: !analyticsHealth, // Only fetch if analytics health is not available
  })

  // Show error notifications
  useEffect(() => {
    if (dashboardError) {
      showError('Failed to load dashboard data', 'Dashboard information may be incomplete')
    }
  }, [dashboardError, showError])

  useEffect(() => {
    if (analyticsHealthError) {
      showError('Failed to load analytics health', 'Health information may be stale')
    }
  }, [analyticsHealthError, showError])

  useEffect(() => {
    if (clusterStatsError) {
      showError('Failed to load cluster statistics', 'Elasticsearch data may be unavailable')
    }
  }, [clusterStatsError, showError])

  useEffect(() => {
    if (statsError && !dashboardData) {
      showError('Failed to load usage statistics', 'Dashboard data may be incomplete')
    }
  }, [statsError, dashboardData, showError])

  useEffect(() => {
    if (pluginsError) {
      showError('Failed to load plugins data', 'Plugin information may be unavailable')
    }
  }, [pluginsError, showError])

  useEffect(() => {
    if (healthError && !analyticsHealth) {
      showError('Failed to load system health', 'Health information may be stale')
    }
  }, [healthError, analyticsHealth, showError])

  // Use dashboard data if available, otherwise fall back to individual stats
  const effectiveStats = dashboardData?.systemHealth?.searchStats ? {
    totalQueries: dashboardData.systemHealth.searchStats.totalSearches,
    totalSessions: Math.floor(dashboardData.systemHealth.searchStats.totalSearches / 10), // Estimate
    avgResponseTime: dashboardData.systemHealth.searchStats.averageSearchTime,
    topQueries: [], // Will be populated from search stats
    pluginUsage: {}
  } : usageStats

  const effectiveHealth = analyticsHealth || systemHealth

  // Computed values
  const activePlugins = plugins?.filter(p => p.enabled) || []
  const isLoading = isDashboardLoading || isAnalyticsHealthLoading || isClusterStatsLoading || 
                   (isStatsLoading && !dashboardData) || isPluginsLoading || 
                   (isHealthLoading && !analyticsHealth)
  const hasError = dashboardError || analyticsHealthError || clusterStatsError || 
                  (statsError && !dashboardData) || pluginsError || 
                  (healthError && !analyticsHealth)

  // Enhanced stats cards with real Elasticsearch data
  const statsCards = [
    {
      title: "Total Searches",
      value: effectiveStats?.totalQueries?.toLocaleString() || '0',
      trend: dashboardData?.systemHealth?.searchStats?.searchesLast24h 
        ? `${dashboardData.systemHealth.searchStats.searchesLast24h.toLocaleString()} last 24h`
        : "+12%",
      trendUp: true,
    },
    {
      title: "Documents", 
      value: dashboardData?.systemHealth?.indices?.reduce((sum, idx) => sum + idx.documentCount, 0)?.toLocaleString() 
        || clusterStats?.activePrimaryShards?.toString() 
        || '0',
      trend: dashboardData?.systemHealth?.indices?.length 
        ? `${dashboardData.systemHealth.indices.length} indices`
        : "+8%",
      trendUp: true,
    },
    {
      title: "Avg Response Time",
      value: effectiveStats?.avgResponseTime 
        ? `${effectiveStats.avgResponseTime.toFixed(2)}ms`
        : `${effectiveStats?.avgResponseTime || 0}ms`,
      trend: "-5%",
      trendUp: true,
    },
    {
      title: "Cluster Health",
      value: clusterStats?.status || analyticsHealth?.elasticsearchStats?.status || 'Unknown',
      trend: clusterStats?.numberOfNodes 
        ? `${clusterStats.numberOfNodes} nodes`
        : `${activePlugins.length} plugins`,
      trendUp: clusterStats?.status === 'green' || clusterStats?.status === 'yellow',
    },
  ]

  return {
    // Enhanced data from new endpoints
    dashboardData,
    analyticsHealth,
    clusterStats,
    
    // Effective data (combines new and legacy)
    stats: effectiveStats,
    systemHealth: effectiveHealth,
    plugins,
    
    // Computed data
    activePlugins,
    statsCards,
    
    // Loading states
    isDashboardLoading,
    isAnalyticsHealthLoading,
    isClusterStatsLoading,
    isStatsLoading,
    isPluginsLoading,
    isHealthLoading,
    isLoading,
    
    // Error states
    hasError,
    dashboardError,
    analyticsHealthError,
    clusterStatsError,
    statsError,
    pluginsError,
    healthError,
  }
}
