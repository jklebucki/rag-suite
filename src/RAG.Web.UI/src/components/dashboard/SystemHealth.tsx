import React from 'react'
import { Activity, Brain } from 'lucide-react'
import type { SystemHealthResponse } from '@/types'

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

interface SystemHealthProps { systemHealth?: SystemHealthResponse }

export function SystemHealth({ systemHealth }: SystemHealthProps) {
  const api = systemHealth?.api
  const es = systemHealth?.elasticsearch
  const vector = systemHealth?.vectorStore
  const llm = systemHealth?.llm
  const models: string[] = (llm?.details?.models) || []

  return (
    <div className="bg-white rounded-lg shadow-sm border p-6">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold text-gray-900">System Health</h2>
        <Activity className="h-5 w-5 text-gray-400" />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <HealthMetric
          label="API"
          status={(api?.status as any) || 'healthy'}
          value={api?.message || 'Running'}
        />
        <HealthMetric
          label="Elasticsearch"
          status={(es?.status as any) || 'healthy'}
          value={es?.message || 'Cluster OK'}
        />
        <HealthMetric
          label="Vector Store"
          status={(vector?.status as any) || 'healthy'}
          value={vector?.message || 'Operational'}
        />
      </div>

      <div className="mt-6">
        <div className="flex items-center gap-2 mb-2">
          <Brain className="h-4 w-4 text-gray-500" />
          <h3 className="text-sm font-semibold text-gray-700">LLM Service</h3>
        </div>
        <div className="flex items-center gap-3 flex-wrap">
          <div className={`inline-flex px-3 py-1 rounded-full text-sm font-medium ${llm?.status === 'healthy' ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`}>
            {llm?.status || 'unknown'}
          </div>
          {models.length > 0 && (
            <div className="text-xs text-gray-600 flex flex-wrap gap-2">
              {models.map(m => (
                <span key={m} className="px-2 py-0.5 bg-gray-100 rounded border text-gray-700">{m}</span>
              ))}
            </div>
          )}
          {models.length === 0 && (
            <span className="text-xs text-gray-500">Brak dostępnych modeli</span>
          )}
        </div>
      </div>
    </div>
  )
}
