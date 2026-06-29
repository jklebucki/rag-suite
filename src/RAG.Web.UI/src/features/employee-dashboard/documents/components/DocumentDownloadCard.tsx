import { Download, Info, LockKeyhole } from 'lucide-react'
import { Button } from '@/shared/components/ui'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { TranslationKeys } from '@/shared/types/i18n'
import type { EmployeeDocument } from '../types/documentsTypes'

interface DocumentDownloadCardProps {
  document: EmployeeDocument
  isDownloading: boolean
  canDownload: boolean
  message: string | null
  onDownload: (documentId: string) => void
  onClearMessage: () => void
}

export function DocumentDownloadCard({
  document,
  isDownloading,
  canDownload,
  message,
  onDownload,
  onClearMessage,
}: DocumentDownloadCardProps) {
  const { t } = useI18n()
  const isArchived = document.status === 'archived'

  return (
    <div className="surface p-5">
      <div className="flex items-center gap-3">
        <div className="rounded-lg bg-emerald-50 p-2 dark:bg-emerald-900/20">
          <Download className="h-5 w-5 text-emerald-600 dark:text-emerald-400" />
        </div>
        <div>
          <h2 className="font-semibold text-gray-900 dark:text-gray-100">
            {t('employeeDashboard.documents.download.title')}
          </h2>
          <p className="text-xs text-gray-500 dark:text-gray-400">{document.fileName}</p>
        </div>
      </div>

      <Button
        variant="outline"
        size="sm"
        className="mt-4 w-full gap-2"
        disabled={!canDownload || isDownloading || isArchived}
        onClick={() => onDownload(document.id)}
      >
        <Download className="h-4 w-4" />
        {isDownloading
          ? t('employeeDashboard.documents.download.preparing')
          : t('employeeDashboard.documents.download.button')}
      </Button>

      <div className="mt-4 flex items-start gap-2 rounded-xl border border-gray-200 bg-gray-50 p-3 dark:border-slate-800 dark:bg-slate-900/50">
        <LockKeyhole className="mt-0.5 h-4 w-4 text-gray-500 dark:text-gray-400" />
        <p className="text-xs leading-5 text-gray-500 dark:text-gray-400">
          {t('employeeDashboard.documents.download.auditNotice')}
        </p>
      </div>

      {message && (
        <div className="mt-4 rounded-xl border border-blue-200 bg-blue-50 p-3 dark:border-blue-900/40 dark:bg-blue-900/10">
          <div className="flex items-start gap-2">
            <Info className="mt-0.5 h-4 w-4 text-blue-600 dark:text-blue-400" />
            <div className="min-w-0">
              <p className="text-xs text-blue-700 dark:text-blue-300">
                {t(message as keyof TranslationKeys)}
              </p>
              <button
                type="button"
                onClick={onClearMessage}
                className="mt-1 text-xs font-medium text-blue-700 hover:underline dark:text-blue-300"
              >
                {t('employeeDashboard.documents.download.closeMessage')}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
