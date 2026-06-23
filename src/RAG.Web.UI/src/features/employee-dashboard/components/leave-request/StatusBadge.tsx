import type { ComponentType } from 'react'
import { AlertCircle, Ban, CheckCircle, XCircle } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { LeaveRequestStatus } from '../../types/leaveRequest'

export function StatusBadge({ status }: { status: LeaveRequestStatus }) {
  const { t } = useI18n()

  const configs: Record<
    LeaveRequestStatus,
    { icon: ComponentType<{ className?: string }>; label: string; className: string }
  > = {
    pending: {
      icon: AlertCircle,
      label: t('employeeDashboard.leave.status.pending'),
      className:
        'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
    },
    approved: {
      icon: CheckCircle,
      label: t('employeeDashboard.leave.status.approved'),
      className:
        'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400',
    },
    rejected: {
      icon: XCircle,
      label: t('employeeDashboard.leave.status.rejected'),
      className: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',
    },
    cancelled: {
      icon: Ban,
      label: t('employeeDashboard.leave.status.cancelled'),
      className:
        'bg-gray-100 text-gray-600 dark:bg-slate-800 dark:text-gray-400',
    },
  }

  const { icon: Icon, label, className } = configs[status]

  return (
    <span
      className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium ${className}`}
    >
      <Icon className="h-3 w-3" />
      {label}
    </span>
  )
}
