import React from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  MessageSquare,
  Search,
  Activity,
  Zap
} from 'lucide-react'
import { apiClient } from '@/services/api'
import { StatsCard } from './StatsCard'
import { TopQueries } from './TopQueries'
import { PluginsStatus } from './PluginsStatus'
import { SystemHealth } from './SystemHealth'

export function Dashboard() {
  const { data: stats } = useQuery({
    queryKey: ['usage-stats'],
    queryFn: () => apiClient.getUsageStats(),
    refetchInterval: 30000, // Refresh every 30 seconds
  })

  const { data: plugins } = useQuery({
    queryKey: ['plugins'],
    queryFn: () => apiClient.getPlugins(),
  })

  const activePlugins = plugins?.filter(p => p.enabled) || []

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Dashboard</h1>
        <p className="text-gray-600 mt-1">
          Overview of your RAG Suite system performance and usage
        </p>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <StatsCard
          title="Total Queries"
          value={stats?.totalQueries?.toLocaleString() || '0'}
          icon={Search}
          trend="+12%"
          trendUp={true}
        />
        <StatsCard
          title="Chat Sessions"
          value={stats?.totalSessions?.toLocaleString() || '0'}
          icon={MessageSquare}
          trend="+8%"
          trendUp={true}
        />
        <StatsCard
          title="Avg Response Time"
          value={`${stats?.avgResponseTime || 0}ms`}
          icon={Zap}
          trend="-5%"
          trendUp={true}
        />
        <StatsCard
          title="Active Plugins"
          value={activePlugins.length.toString()}
          icon={Activity}
          trend={`${plugins?.length || 0} total`}
          trendUp={null}
        />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Top Queries */}
        <TopQueries queries={stats?.topQueries} />

        {/* Plugin Status */}
        <PluginsStatus 
          plugins={plugins} 
          pluginUsage={stats?.pluginUsage}
        />
      </div>

      {/* System Health */}
      <SystemHealth stats={stats} />
    </div>
  )
}
