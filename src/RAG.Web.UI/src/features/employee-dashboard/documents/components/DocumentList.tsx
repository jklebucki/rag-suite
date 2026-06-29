import { ChevronRight, FileText } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { DocumentCategory, EmployeeDocument } from '../types/documentsTypes'
import { formatDocumentDate, getCategoryName } from '../services/documentsUtils'
import { DocumentStatusBadge } from './DocumentStatusBadge'

interface DocumentListProps {
  documents: EmployeeDocument[]
  categories: DocumentCategory[]
  selectedDocumentId: string | null
  onSelect: (document: EmployeeDocument) => void
}

export function DocumentList({
  documents,
  categories,
  selectedDocumentId,
  onSelect,
}: DocumentListProps) {
  const { t } = useI18n()

  return (
    <div className="surface overflow-hidden">
      <div className="flex items-center gap-2 border-b border-gray-100 px-5 py-4 dark:border-slate-800">
        <div className="rounded-lg bg-primary-50 p-2 dark:bg-primary-900/20">
          <FileText className="h-5 w-5 text-primary-600 dark:text-primary-400" />
        </div>
        <h2 className="font-semibold text-gray-900 dark:text-gray-100">
          {t('employeeDashboard.documents.list.title')}
        </h2>
        <span className="ml-auto text-xs tabular-nums text-gray-400 dark:text-gray-500">
          {t('employeeDashboard.documents.list.recordsCount', { count: String(documents.length) })}
        </span>
      </div>

      <div className="divide-y divide-gray-100 dark:divide-slate-800">
        {documents.map((document) => {
          const selected = document.id === selectedDocumentId

          return (
            <button
              key={document.id}
              type="button"
              onClick={() => onSelect(document)}
              className={`flex w-full items-start gap-3 px-5 py-4 text-left transition-colors ${
                selected
                  ? 'bg-primary-50/70 dark:bg-primary-900/10'
                  : 'hover:bg-gray-50 dark:hover:bg-slate-800/50'
              }`}
            >
              <FileText className="mt-0.5 h-5 w-5 shrink-0 text-gray-400 dark:text-gray-500" />
              <div className="min-w-0 flex-1">
                <div className="flex flex-wrap items-center gap-2">
                  <h3 className="font-medium text-gray-900 dark:text-gray-100">{document.name}</h3>
                  <DocumentStatusBadge status={document.status} />
                </div>
                <div className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-gray-500 dark:text-gray-400">
                  <span>{getCategoryName(document.categoryId, categories, t)}</span>
                  <span>{t('employeeDashboard.documents.version', { version: document.version })}</span>
                  <span>{t('employeeDashboard.documents.addedAt', { date: formatDocumentDate(document.addedAt) })}</span>
                </div>
              </div>
              <ChevronRight className="mt-1 h-4 w-4 shrink-0 text-gray-400" />
            </button>
          )
        })}
      </div>
    </div>
  )
}
