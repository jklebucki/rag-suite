import React from 'react'
import { Activity } from 'lucide-react'

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

interface SystemHealthProps {
  stats?: any // Accept any stats object for now
}

export function SystemHealth({ stats }: SystemHealthProps) {
  return (
    <div className="bg-white rounded-lg shadow-sm border p-6">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold text-gray-900">System Health</h2>
        <Activity className="h-5 w-5 text-gray-400" />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <HealthMetric
          label="API Status"
          status={stats?.apiStatus?.status || 'healthy'}
          value={stats?.apiStatus?.value || '99.9% uptime'}
        />
        <HealthMetric
          label="Elasticsearch"
          status={stats?.elasticsearchStatus?.status || 'healthy'}
          value={stats?.elasticsearchStatus?.value || 'All indices green'}
        />
        <HealthMetric
          label="Vector Store"
          status={stats?.vectorStoreStatus?.status || 'healthy'}
          value={stats?.vectorStoreStatus?.value || '1.2M documents'}
        />
      </div>
    </div>
  )
}
