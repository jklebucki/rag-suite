import type { ComponentType } from 'react'
import { AlertCircle, Ban, CheckCircle, RotateCcw } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { SalaryStatus } from '../types/salaryTypes'
import { salaryStatusLabel } from '../services/salaryUtils'

export function SalaryStatusBadge({ status }: { status: SalaryStatus }) {
  const { t } = useI18n()
  const configs: Record<
    SalaryStatus,
    { icon: ComponentType<{ className?: string }>; className: string }
  > = {
    paid: {
      icon: CheckCircle,
      className: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400',
    },
    pending: {
      icon: AlertCircle,
      className: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
    },
    cancelled: {
      icon: Ban,
      className: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',
    },
    correction: {
      icon: RotateCcw,
      className: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
    },
  }

  const { icon: Icon, className } = configs[status]

  return (
    <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium ${className}`}>
      <Icon className="h-3 w-3" />
      {salaryStatusLabel(status, t)}
    </span>
  )
}
