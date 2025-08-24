import { useQuery } from '@tanstack/react-query'
import { apiClient } from '@/services/api'

export function useDashboard() {
  // Get usage statistics
  const { data: stats, isLoading: isStatsLoading, error: statsError } = useQuery({
    queryKey: ['usage-stats'],
    queryFn: () => apiClient.getUsageStats(),
    refetchInterval: 30000, // Refresh every 30 seconds
  })

  // Get plugins data
  const { data: plugins, isLoading: isPluginsLoading, error: pluginsError } = useQuery({
    queryKey: ['plugins'],
    queryFn: () => apiClient.getPlugins(),
  })

  // Computed values
  const activePlugins = plugins?.filter(p => p.enabled) || []
  const isLoading = isStatsLoading || isPluginsLoading
  const hasError = statsError || pluginsError

  // Stats card data
  const statsCards = [
    {
      title: "Total Queries",
      value: stats?.totalQueries?.toLocaleString() || '0',
      trend: "+12%",
      trendUp: true,
    },
    {
      title: "Chat Sessions", 
      value: stats?.totalSessions?.toLocaleString() || '0',
      trend: "+8%",
      trendUp: true,
    },
    {
      title: "Avg Response Time",
      value: `${stats?.avgResponseTime || 0}ms`,
      trend: "-5%",
      trendUp: true,
    },
    {
      title: "Active Plugins",
      value: activePlugins.length.toString(),
      trend: `${plugins?.length || 0} total`,
      trendUp: null,
    },
  ]

  return {
    // Raw data
    stats,
    plugins,
    
    // Computed data
    activePlugins,
    statsCards,
    
    // Loading states
    isLoading,
    isStatsLoading,
    isPluginsLoading,
    
    // Error states
    hasError,
    statsError,
    pluginsError,
  }
}
