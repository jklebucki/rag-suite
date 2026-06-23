import { AlertCircle, CheckCircle, XCircle } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { ChangeRequestStatus } from '../../types/personalData'

export function StatusBadge({ status }: { status: ChangeRequestStatus }) {
  const { t } = useI18n()

  if (status === 'approved') {
    return (
      <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400">
        <CheckCircle className="h-3 w-3" />
        {t('employeeDashboard.personal.history.status.approved')}
      </span>
    )
  }

  if (status === 'rejected') {
    return (
      <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400">
        <XCircle className="h-3 w-3" />
        {t('employeeDashboard.personal.history.status.rejected')}
      </span>
    )
  }

  return (
    <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400">
      <AlertCircle className="h-3 w-3" />
      {t('employeeDashboard.personal.history.status.pending')}
    </span>
  )
}
