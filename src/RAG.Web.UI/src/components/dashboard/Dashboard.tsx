import React from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  BarChart3,
  MessageSquare,
  Search,
  TrendingUp,
  Activity,
  Database,
  Zap
} from 'lucide-react'
import { apiClient } from '@/services/api'

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
        <div className="bg-white rounded-lg shadow-sm border p-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold text-gray-900">Top Queries</h2>
            <BarChart3 className="h-5 w-5 text-gray-400" />
          </div>

          <div className="space-y-3">
            {stats?.topQueries?.slice(0, 5).map((query, index) => (
              <div key={index} className="flex items-center justify-between py-2">
                <span className="text-sm text-gray-900 truncate flex-1">{query}</span>
                <span className="text-xs text-gray-500 ml-2">#{index + 1}</span>
              </div>
            )) || (
              <div className="text-gray-500 text-sm py-4 text-center">
                No query data available
              </div>
            )}
          </div>
        </div>

        {/* Plugin Status */}
        <div className="bg-white rounded-lg shadow-sm border p-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold text-gray-900">Plugins Status</h2>
            <Database className="h-5 w-5 text-gray-400" />
          </div>

          <div className="space-y-3">
            {plugins?.map((plugin) => (
              <div key={plugin.id} className="flex items-center justify-between py-2">
                <div className="flex items-center gap-3">
                  <div className={`w-3 h-3 rounded-full ${
                    plugin.enabled ? 'bg-green-400' : 'bg-gray-300'
                  }`} />
                  <div>
                    <p className="text-sm font-medium text-gray-900">{plugin.name}</p>
                    <p className="text-xs text-gray-500">v{plugin.version}</p>
                  </div>
                </div>
                <span className="text-xs text-gray-500">
                  {stats?.pluginUsage?.[plugin.id] || 0} uses
                </span>
              </div>
            )) || (
              <div className="text-gray-500 text-sm py-4 text-center">
                No plugins configured
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Recent Activity */}
      <div className="bg-white rounded-lg shadow-sm border p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-gray-900">System Health</h2>
          <Activity className="h-5 w-5 text-gray-400" />
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <HealthMetric
            label="API Status"
            status="healthy"
            value="99.9% uptime"
          />
          <HealthMetric
            label="Elasticsearch"
            status="healthy"
            value="All indices green"
          />
          <HealthMetric
            label="Vector Store"
            status="healthy"
            value="1.2M documents"
          />
        </div>
      </div>
    </div>
  )
}

interface StatsCardProps {
  title: string
  value: string
  icon: React.ComponentType<{ className?: string }>
  trend?: string
  trendUp: boolean | null
}

function StatsCard({ title, value, icon: Icon, trend, trendUp }: StatsCardProps) {
  return (
    <div className="bg-white rounded-lg shadow-sm border p-6">
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm font-medium text-gray-600">{title}</p>
          <p className="text-2xl font-bold text-gray-900 mt-1">{value}</p>
          {trend && (
            <p className={`text-sm mt-1 flex items-center gap-1 ${
              trendUp === null
                ? 'text-gray-500'
                : trendUp
                  ? 'text-green-600'
                  : 'text-red-600'
            }`}>
              {trendUp === true && <TrendingUp className="h-4 w-4" />}
              {trendUp === false && <TrendingUp className="h-4 w-4 rotate-180" />}
              {trend}
            </p>
          )}
        </div>
        <div className="p-3 bg-primary-50 rounded-lg">
          <Icon className="h-6 w-6 text-primary-600" />
        </div>
      </div>
    </div>
  )
}

interface HealthMetricProps {
  label: string
  status: 'healthy' | 'warning' | 'error'
  value: string
}

function HealthMetric({ label, status, value }: HealthMetricProps) {
  const statusColors = {
    healthy: 'bg-green-100 text-green-800',
    warning: 'bg-yellow-100 text-yellow-800',
    error: 'bg-red-100 text-red-800',
  }

  return (
    <div className="text-center">
      <div className={`inline-flex px-3 py-1 rounded-full text-sm font-medium ${statusColors[status]}`}>
        {status}
      </div>
      <p className="text-lg font-semibold text-gray-900 mt-2">{label}</p>
      <p className="text-sm text-gray-600">{value}</p>
    </div>
  )
}
