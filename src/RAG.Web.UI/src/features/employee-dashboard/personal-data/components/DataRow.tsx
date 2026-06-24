import type { ComponentType } from 'react'

interface DataRowProps {
  icon: ComponentType<{ className?: string }>
  label: string
  value: string
}

export function DataRow({ icon: Icon, label, value }: DataRowProps) {
  return (
    <div className="flex items-start gap-3 py-2.5 border-b border-gray-100 dark:border-slate-800 last:border-0">
      <div className="p-1.5 bg-primary-50 dark:bg-primary-900/20 rounded-lg flex-shrink-0 mt-0.5">
        <Icon className="h-4 w-4 text-primary-600 dark:text-primary-400" />
      </div>
      <div className="flex-1 min-w-0">
        <p className="text-xs text-gray-500 dark:text-gray-500">{label}</p>
        <p className="text-sm font-medium text-gray-900 dark:text-gray-100 break-words">{value}</p>
      </div>
    </div>
  )
}
