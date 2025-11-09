import React from 'react'
import { TrendingUp } from 'lucide-react'

interface StatsCardProps {
  title: string
  value: string
  icon: React.ComponentType<{ className?: string }>
  trend?: string
  trendUp: boolean | null
}

export function StatsCard({ title, value, icon: Icon, trend, trendUp }: StatsCardProps) {
  return (
    <div className="surface p-6">
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm font-medium text-gray-600 dark:text-gray-400">{title}</p>
          <p className="text-2xl font-bold text-gray-900 dark:text-gray-100 mt-1">{value}</p>
          {trend && (
            <p className={`text-sm mt-1 flex items-center gap-1 ${
              trendUp === null
                ? 'text-gray-500 dark:text-gray-400'
                : trendUp
                  ? 'text-green-600 dark:text-green-300'
                  : 'text-red-600 dark:text-red-300'
            }`}>
              {trendUp === true && <TrendingUp className="h-4 w-4" />}
              {trendUp === false && <TrendingUp className="h-4 w-4 rotate-180" />}
              {trend}
            </p>
          )}
        </div>
        <div className="p-3 bg-primary-50 dark:bg-primary-900/20 rounded-lg">
          <Icon className="h-6 w-6 text-primary-600 dark:text-primary-300" />
        </div>
      </div>
    </div>
  )
}
