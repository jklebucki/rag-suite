import { FileText, XCircle } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { TranslationKeys } from '@/shared/types/i18n'
import { useDocumentsData } from '../hooks/useDocumentsData'
import { Pit11DocumentList } from './Pit11DocumentList'

export function Documents() {
  const { t } = useI18n()
  const {
    documents,
    isLoading,
    error,
  } = useDocumentsData()

  if (isLoading) {
    return (
      <div className="flex h-48 items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary-200 border-t-primary-600" />
      </div>
    )
  }

  if (error) {
    return (
      <div className="surface p-6 text-center text-red-600 dark:text-red-400">
        <XCircle className="mx-auto mb-2 h-8 w-8" />
        <p className="text-sm">
          {t(error as keyof TranslationKeys)}
        </p>
      </div>
    )
  }

  return (
    <div className="space-y-5 text-gray-900 dark:text-gray-100">
      <div className="flex items-center gap-3">
        <FileText className="h-8 w-8 text-primary-600 dark:text-primary-400" />
        <div>
          <h1 className="text-2xl font-bold">{t('employeeDashboard.pit11.title')}</h1>
          <p className="text-sm text-gray-500 dark:text-gray-400">
            {t('employeeDashboard.pit11.subtitle')}
          </p>
        </div>
      </div>

      <Pit11DocumentList documents={documents} />
    </div>
  )
}
