import type { ComponentType } from 'react'

interface TeamStatisticsCardProps {
  title: string
  value: number
  icon: ComponentType<{ className?: string }>
  tone?: 'neutral' | 'warning' | 'danger'
  description?: string
}

export function TeamStatisticsCard({
  title,
  value,
  icon: Icon,
  tone = 'neutral',
  description,
}: TeamStatisticsCardProps) {
  const toneClasses = {
    neutral: 'bg-primary-50 text-primary-600 dark:bg-primary-900/20 dark:text-primary-400',
    warning: 'bg-amber-50 text-amber-600 dark:bg-amber-900/20 dark:text-amber-400',
    danger: 'bg-red-50 text-red-600 dark:bg-red-900/20 dark:text-red-400',
  }

  return (
    <div className="surface p-5">
      <div className="flex items-start justify-between gap-3">
        <div className="space-y-2">
          <p className="text-sm font-medium text-gray-500 dark:text-gray-400">{title}</p>
          <p className="text-3xl font-bold tabular-nums text-gray-900 dark:text-gray-100">
            {value}
          </p>
        </div>
        <div className={`rounded-xl p-2.5 ${toneClasses[tone]}`}>
          <Icon className="h-5 w-5" />
        </div>
      </div>
      {description && (
        <p className="mt-3 text-xs leading-5 text-gray-500 dark:text-gray-400">{description}</p>
      )}
    </div>
  )
}
