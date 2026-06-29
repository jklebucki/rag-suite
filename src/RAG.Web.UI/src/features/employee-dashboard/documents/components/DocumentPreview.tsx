import { Eye, FileText, ShieldCheck } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { DocumentCategory, EmployeeDocument } from '../types/documentsTypes'
import { formatDocumentDate, getCategoryName } from '../services/documentsUtils'
import { DocumentStatusBadge } from './DocumentStatusBadge'

interface DocumentPreviewProps {
  document: EmployeeDocument
  categories: DocumentCategory[]
}

export function DocumentPreview({ document, categories }: DocumentPreviewProps) {
  const { t } = useI18n()
  const details = [
    { label: t('employeeDashboard.documents.details.category'), value: getCategoryName(document.categoryId, categories, t) },
    { label: t('employeeDashboard.documents.details.addedAt'), value: formatDocumentDate(document.addedAt) },
    { label: t('employeeDashboard.documents.details.version'), value: document.version },
    { label: t('employeeDashboard.documents.details.owner'), value: document.owner },
  ]

  return (
    <div className="surface overflow-hidden">
      <div className="border-b border-gray-100 px-5 py-4 dark:border-slate-800">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <div className="flex flex-wrap items-center gap-2">
              <h2 className="font-semibold text-gray-900 dark:text-gray-100">{document.name}</h2>
              <DocumentStatusBadge status={document.status} />
            </div>
            <p className="mt-2 max-w-3xl text-sm leading-6 text-gray-600 dark:text-gray-300">
              {document.description}
            </p>
          </div>
          <div className="inline-flex items-center gap-1 rounded-full bg-gray-100 px-2 py-1 text-xs font-medium text-gray-600 dark:bg-slate-800 dark:text-gray-300">
            <ShieldCheck className="h-3.5 w-3.5" />
            {t('employeeDashboard.documents.readOnly')}
          </div>
        </div>

        <dl className="mt-4 grid grid-cols-2 gap-3 text-sm sm:grid-cols-4">
          {details.map((item) => (
            <div key={item.label}>
              <dt className="text-xs text-gray-500 dark:text-gray-400">{item.label}</dt>
              <dd className="mt-0.5 font-medium text-gray-900 dark:text-gray-100">{item.value}</dd>
            </div>
          ))}
        </dl>
      </div>

      <div className="p-5">
        <div className="mb-3 flex items-center gap-2">
          <Eye className="h-5 w-5 text-primary-600 dark:text-primary-400" />
          <h3 className="font-semibold text-gray-900 dark:text-gray-100">
            {t('employeeDashboard.documents.preview.title')}
          </h3>
        </div>
        <div className="flex min-h-[360px] flex-col items-center justify-center rounded-xl border border-dashed border-gray-300 bg-gray-50 px-6 text-center dark:border-slate-700 dark:bg-slate-900/50">
          <div className="rounded-2xl bg-white p-4 shadow-sm dark:bg-slate-800">
            <FileText className="h-10 w-10 text-primary-600 dark:text-primary-400" />
          </div>
          <p className="mt-4 text-sm font-semibold text-gray-900 dark:text-gray-100">
            {t('employeeDashboard.documents.preview.placeholderTitle')}
          </p>
          <p className="mt-2 max-w-md text-xs leading-5 text-gray-500 dark:text-gray-400">
            {t('employeeDashboard.documents.preview.placeholderDescription')}
          </p>
        </div>
      </div>
    </div>
  )
}
