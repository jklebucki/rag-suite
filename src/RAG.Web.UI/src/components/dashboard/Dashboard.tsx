import React from 'react'
import {
  MessageSquare,
  Search,
  Activity,
  Zap
} from 'lucide-react'
import { useDashboard } from '@/hooks/useDashboard'
import { useI18n } from '@/contexts/I18nContext'
import { StatsCard } from './StatsCard'
import { TopQueries } from './TopQueries'
import { PluginsStatus } from './PluginsStatus'
import { SystemHealth } from './SystemHealth'

export function Dashboard() {
  const { t } = useI18n()
  const {
    stats,
    plugins,
    systemHealth,
    analyticsHealth,
    clusterStats,
    statsCards,
    isLoading,
    hasError,
  } = useDashboard()

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600"></div>
      </div>
    )
  }

  if (hasError) {
    return (
      <div className="text-center py-12">
        <p className="text-gray-500">{t('common.error')}</p>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">{t('dashboard.title')}</h1>
        <p className="text-gray-600 mt-1">
          {t('dashboard.subtitle')}
        </p>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {statsCards.map((card, index) => {
          const icons = [Search, MessageSquare, Zap, Activity]
          return (
            <StatsCard
              key={card.title}
              title={card.title}
              value={card.value}
              icon={icons[index]}
              trend={card.trend}
              trendUp={card.trendUp}
            />
          )
        })}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Top Queries */}
        <TopQueries 
          queries={stats?.topQueries} 
          searchStats={analyticsHealth?.searchStats}
        />

        {/* Plugin Status */}
        <PluginsStatus 
          plugins={plugins} 
          pluginUsage={stats?.pluginUsage}
        />
      </div>

      {/* Enhanced System Health */}
      <SystemHealth 
        systemHealth={systemHealth as any} 
        analyticsHealth={analyticsHealth}
        clusterStats={clusterStats}
      />
    </div>
  )
}
