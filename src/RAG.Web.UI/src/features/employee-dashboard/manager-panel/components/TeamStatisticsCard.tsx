import type { ComponentType } from 'react'

interface TeamStatisticsCardProps {
  title: string
  value: number
  icon: ComponentType<{ className?: string }>
  tone?: 'neutral' | 'warning' | 'danger' | 'muted'
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
    neutral: {
      icon: 'bg-primary-50 text-primary-600 ring-primary-100 dark:bg-primary-900/20 dark:text-primary-400 dark:ring-primary-900/40',
      divider: 'bg-primary-100 dark:bg-primary-900/40',
    },
    warning: {
      icon: 'bg-amber-50 text-amber-600 ring-amber-100 dark:bg-amber-900/20 dark:text-amber-400 dark:ring-amber-900/40',
      divider: 'bg-amber-100 dark:bg-amber-900/40',
    },
    danger: {
      icon: 'bg-red-50 text-red-600 ring-red-100 dark:bg-red-900/20 dark:text-red-400 dark:ring-red-900/40',
      divider: 'bg-red-100 dark:bg-red-900/40',
    },
    muted: {
      icon: 'bg-slate-100 text-slate-600 ring-slate-200 dark:bg-slate-800 dark:text-slate-300 dark:ring-slate-700',
      divider: 'bg-slate-200 dark:bg-slate-700',
    },
  }
  const classes = toneClasses[tone]

  return (
    <div className="surface flex h-full min-h-[172px] p-5">
      <div className="flex w-full flex-col">
        <div className="flex min-h-10 items-center gap-3">
          <div className={`flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-xl ring-1 ${classes.icon}`}>
            <Icon className="h-5 w-5" />
          </div>
          <p className="min-w-0 text-sm font-semibold leading-5 text-gray-700 dark:text-gray-200">
            {title}
          </p>
        </div>

        <div className={`mt-4 h-px w-full ${classes.divider}`} />

        <div className="flex min-h-20 flex-1 items-center justify-center py-4">
          <p className="text-center text-5xl font-bold leading-none tracking-normal tabular-nums text-gray-950 dark:text-gray-50">
            {value}
          </p>
        </div>

        {description && (
          <p className="min-h-5 text-center text-sm font-medium leading-5 text-gray-500 dark:text-gray-400">
            {description}
          </p>
        )}
      </div>
    </div>
  )
}
