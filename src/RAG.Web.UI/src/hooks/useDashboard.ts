import { useQuery } from '@tanstack/react-query'
import { useEffect } from 'react'
import { apiClient } from '@/services/api'
import { useToastContext } from '@/contexts/ToastContext'

export function useDashboard() {
  const { showError } = useToastContext()
  
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

  // Get system health (LLM models etc.)
  const { data: systemHealth, isLoading: isHealthLoading, error: healthError } = useQuery({
    queryKey: ['system-health'],
    queryFn: () => apiClient.getSystemHealth(),
    refetchInterval: 15000, // refresh every 15s for near-real-time status
  })

  // Show error notifications
  useEffect(() => {
    if (statsError) {
      showError('Failed to load usage statistics', 'Dashboard data may be incomplete')
    }
  }, [statsError, showError])

  useEffect(() => {
    if (pluginsError) {
      showError('Failed to load plugins data', 'Plugin information may be unavailable')
    }
  }, [pluginsError, showError])

  useEffect(() => {
    if (healthError) {
      showError('Failed to load system health', 'Health information may be stale')
    }
  }, [healthError, showError])

  // Computed values
  const activePlugins = plugins?.filter(p => p.enabled) || []
  const isLoading = isStatsLoading || isPluginsLoading || isHealthLoading
  const hasError = statsError || pluginsError || healthError

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
  systemHealth,
    
    // Computed data
    activePlugins,
    statsCards,
    
    // Loading states
    isLoading,
    isStatsLoading,
    isPluginsLoading,
  isHealthLoading,
    
    // Error states
    hasError,
    statsError,
    pluginsError,
  healthError,
  }
}
