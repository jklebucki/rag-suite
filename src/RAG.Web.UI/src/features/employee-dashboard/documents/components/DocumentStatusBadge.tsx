import type { ComponentType } from 'react'
import { Archive, CheckCircle, Sparkles, RefreshCw } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { DocumentStatus } from '../types/documentsTypes'
import { documentStatusLabel } from '../services/documentsUtils'

export function DocumentStatusBadge({ status }: { status: DocumentStatus }) {
  const { t } = useI18n()
  const configs: Record<
    DocumentStatus,
    { icon: ComponentType<{ className?: string }>; className: string }
  > = {
    available: {
      icon: CheckCircle,
      className: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400',
    },
    new: {
      icon: Sparkles,
      className: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
    },
    updated: {
      icon: RefreshCw,
      className: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
    },
    archived: {
      icon: Archive,
      className: 'bg-gray-100 text-gray-700 dark:bg-slate-800 dark:text-gray-300',
    },
  }

  const { icon: Icon, className } = configs[status]

  return (
    <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium ${className}`}>
      <Icon className="h-3 w-3" />
      {documentStatusLabel(status, t)}
    </span>
  )
}
