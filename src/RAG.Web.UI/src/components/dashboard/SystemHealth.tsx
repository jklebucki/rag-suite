import React from 'react'
import { Activity, Brain, Database, Server, Clock } from 'lucide-react'
import type { SystemHealthResponse, SystemHealth as AnalyticsSystemHealth, ElasticsearchStats } from '@/types'

interface HealthMetricProps {
  label: string
  status: string
  value: string
  subtext?: string
}

function HealthMetric({ label, status, value, subtext }: HealthMetricProps) {
  // Map various status types to standard colors
  const getStatusColor = (status: string) => {
    switch (status) {
      case 'healthy':
      case 'green':
        return 'bg-green-100 text-green-800'
      case 'warning':
      case 'yellow':
        return 'bg-yellow-100 text-yellow-800'
      case 'error':
      case 'red':
        return 'bg-red-100 text-red-800'
      default:
        return 'bg-gray-100 text-gray-800'
    }
  }

  return (
    <div className="text-center">
      <div className={`inline-flex px-3 py-1 rounded-full text-sm font-medium ${getStatusColor(status)}`}>
        {status}
      </div>
      <p className="text-lg font-semibold text-gray-900 mt-2">{label}</p>
      <p className="text-sm text-gray-600">{value}</p>
      {subtext && <p className="text-xs text-gray-500 mt-1">{subtext}</p>}
    </div>
  )
}

interface ElasticsearchHealthProps {
  elasticsearchStats?: ElasticsearchStats
  indices?: any[]
  nodes?: any[]
}

function ElasticsearchHealth({ elasticsearchStats, indices, nodes }: ElasticsearchHealthProps) {
  if (!elasticsearchStats) return null

  const totalDocuments = indices?.reduce((sum, idx) => sum + idx.documentCount, 0) || 0

  return (
    <div className="mt-6 border-t pt-6">
      <div className="flex items-center gap-2 mb-4">
        <Database className="h-5 w-5 text-blue-500" />
        <h3 className="text-lg font-semibold text-gray-700">Elasticsearch Cluster</h3>
      </div>
      
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
        <HealthMetric
          label="Cluster Status"
          status={elasticsearchStats.status}
          value={elasticsearchStats.clusterName}
          subtext={`${elasticsearchStats.numberOfNodes} nodes`}
        />
        <HealthMetric
          label="Documents"
          status="healthy"
          value={totalDocuments.toLocaleString()}
          subtext={`${indices?.length || 0} indices`}
        />
        <HealthMetric
          label="Shards"
          status={elasticsearchStats.activeShardsPercent > 90 ? 'healthy' : 'warning'}
          value={`${elasticsearchStats.activeShards}/${elasticsearchStats.activeShards + elasticsearchStats.unassignedShards}`}
          subtext={`${elasticsearchStats.activeShardsPercent.toFixed(1)}% active`}
        />
      </div>

      {/* Node Information */}
      {nodes && nodes.length > 0 && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
          {nodes.slice(0, 3).map((node) => (
            <div key={node.nodeId} className="bg-gray-50 rounded-lg p-3">
              <div className="flex items-center gap-2 mb-2">
                <Server className="h-4 w-4 text-gray-500" />
                <span className="text-sm font-medium text-gray-700 truncate">{node.nodeName}</span>
              </div>
              <div className="text-xs text-gray-600 space-y-1">
                <div>JVM Memory: {node.jvmMemoryPercent.toFixed(1)}%</div>
                <div>Documents: {node.documentCount.toLocaleString()}</div>
                <div>Roles: {node.roles.join(', ')}</div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

interface SystemHealthProps { 
  systemHealth?: SystemHealthResponse
  analyticsHealth?: AnalyticsSystemHealth
  clusterStats?: ElasticsearchStats
}

export function SystemHealth({ systemHealth, analyticsHealth, clusterStats }: SystemHealthProps) {
  // Use analytics health if available, otherwise fall back to legacy system health
  const effectiveAnalyticsHealth = analyticsHealth
  const effectiveClusterStats = clusterStats || analyticsHealth?.elasticsearchStats

  // Legacy system health data
  const api = systemHealth?.api
  const es = systemHealth?.elasticsearch
  const vector = systemHealth?.vectorStore
  const llm = systemHealth?.llm
  const models: string[] = (llm?.details?.models) || []

  // Analytics health data
  const searchStats = effectiveAnalyticsHealth?.searchStats

  return (
    <div className="bg-white rounded-lg shadow-sm border p-6">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold text-gray-900">System Health</h2>
        <Activity className="h-5 w-5 text-gray-400" />
      </div>

      {/* Main Service Health */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <HealthMetric
          label="API"
          status={effectiveAnalyticsHealth ? 'healthy' : (api?.status as any) || 'healthy'}
          value={api?.message || 'Running'}
        />
        <HealthMetric
          label="Elasticsearch"
          status={effectiveAnalyticsHealth?.elasticsearchAvailable 
            ? effectiveClusterStats?.status || 'healthy'
            : (es?.status as any) || 'healthy'}
          value={effectiveClusterStats?.clusterName || es?.message || 'Cluster OK'}
          subtext={effectiveClusterStats ? `${effectiveClusterStats.numberOfNodes} nodes` : undefined}
        />
        <HealthMetric
          label="LLM Service"
          status={effectiveAnalyticsHealth?.llmServiceAvailable 
            ? 'healthy' 
            : (llm?.status as any) || 'healthy'}
          value={llm?.message || 'Operational'}
          subtext={models.length > 0 ? `${models.length} models` : undefined}
        />
      </div>

      {/* Search Performance Stats */}
      {searchStats && (
        <div className="mt-6 border-t pt-6">
          <div className="flex items-center gap-2 mb-4">
            <Clock className="h-5 w-5 text-green-500" />
            <h3 className="text-lg font-semibold text-gray-700">Search Performance</h3>
          </div>
          
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div className="text-center">
              <p className="text-2xl font-bold text-gray-900">{searchStats.totalSearches.toLocaleString()}</p>
              <p className="text-sm text-gray-600">Total Searches</p>
            </div>
            <div className="text-center">
              <p className="text-2xl font-bold text-gray-900">{searchStats.averageSearchTime.toFixed(2)}ms</p>
              <p className="text-sm text-gray-600">Avg Response Time</p>
            </div>
            <div className="text-center">
              <p className="text-2xl font-bold text-gray-900">{searchStats.searchesLast24h.toLocaleString()}</p>
              <p className="text-sm text-gray-600">Last 24h</p>
            </div>
            <div className="text-center">
              <p className="text-2xl font-bold text-gray-900 truncate">{searchStats.mostActiveIndex}</p>
              <p className="text-sm text-gray-600">Most Active Index</p>
            </div>
          </div>
        </div>
      )}

      {/* LLM Models */}
      {models.length > 0 && (
        <div className="mt-6 border-t pt-6">
          <div className="flex items-center gap-2 mb-2">
            <Brain className="h-4 w-4 text-gray-500" />
            <h3 className="text-sm font-semibold text-gray-700">Available Models</h3>
          </div>
          <div className="flex items-center gap-3 flex-wrap">
            <div className={`inline-flex px-3 py-1 rounded-full text-sm font-medium ${llm?.status === 'healthy' ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`}>
              {llm?.status || 'unknown'}
            </div>
            <div className="text-xs text-gray-600 flex flex-wrap gap-2">
              {models.map(m => (
                <span key={m} className="px-2 py-0.5 bg-gray-100 rounded border text-gray-700">{m}</span>
              ))}
            </div>
          </div>
        </div>
      )}

      {/* Elasticsearch Detailed Health */}
      <ElasticsearchHealth 
        elasticsearchStats={effectiveClusterStats}
        indices={effectiveAnalyticsHealth?.indices}
        nodes={effectiveAnalyticsHealth?.nodes}
      />
    </div>
  )
}
