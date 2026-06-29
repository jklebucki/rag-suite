import { History } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { DocumentDownloadLog } from '../types/documentsTypes'
import { documentAuditActionLabel, formatDocumentDateTime } from '../services/documentsUtils'

interface DownloadLogTableProps {
  logs: DocumentDownloadLog[]
}

export function DownloadLogTable({ logs }: DownloadLogTableProps) {
  const { t } = useI18n()

  return (
    <div className="surface overflow-hidden">
      <div className="flex items-center gap-2 border-b border-gray-100 px-5 py-4 dark:border-slate-800">
        <div className="rounded-lg bg-primary-50 p-2 dark:bg-primary-900/20">
          <History className="h-5 w-5 text-primary-600 dark:text-primary-400" />
        </div>
        <h2 className="font-semibold text-gray-900 dark:text-gray-100">
          {t('employeeDashboard.documents.history.title')}
        </h2>
      </div>

      <div className="hidden overflow-x-auto lg:block">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-gray-100 bg-gray-50 dark:border-slate-800 dark:bg-slate-900/50">
              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                {t('employeeDashboard.documents.history.col.downloadedAt')}
              </th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                {t('employeeDashboard.documents.history.col.documentName')}
              </th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                {t('employeeDashboard.documents.history.col.category')}
              </th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                {t('employeeDashboard.documents.history.col.user')}
              </th>
              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                {t('employeeDashboard.documents.history.col.action')}
              </th>
            </tr>
          </thead>
          <tbody>
            {logs.map((log) => (
              <tr
                key={log.id}
                className="border-b border-gray-50 last:border-0 hover:bg-gray-50 dark:border-slate-800 dark:hover:bg-slate-800/50"
              >
                <td className="whitespace-nowrap px-5 py-3.5 text-xs tabular-nums text-gray-500 dark:text-gray-400">
                  {formatDocumentDateTime(log.downloadedAt)}
                </td>
                <td className="px-4 py-3.5 font-medium text-gray-900 dark:text-gray-100">{log.documentName}</td>
                <td className="px-4 py-3.5 text-gray-600 dark:text-gray-300">
                  {t(`employeeDashboard.documents.category.${log.categoryId}`)}
                </td>
                <td className="whitespace-nowrap px-4 py-3.5 text-gray-600 dark:text-gray-300">{log.user}</td>
                <td className="px-5 py-3.5 text-gray-600 dark:text-gray-300">{documentAuditActionLabel(log.action, t)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="divide-y divide-gray-100 lg:hidden dark:divide-slate-800">
        {logs.map((log) => (
          <div key={log.id} className="space-y-2 px-4 py-4">
            <div className="flex items-start justify-between gap-3">
              <div>
                <p className="text-sm font-semibold text-gray-900 dark:text-gray-100">{log.documentName}</p>
                <p className="text-xs text-gray-500 dark:text-gray-400">
                  {t(`employeeDashboard.documents.category.${log.categoryId}`)}
                </p>
              </div>
              <span className="text-right text-xs tabular-nums text-gray-500 dark:text-gray-400">
                {formatDocumentDateTime(log.downloadedAt)}
              </span>
            </div>
            <div className="flex flex-wrap items-center gap-3 text-xs text-gray-600 dark:text-gray-300">
              <span>{log.user}</span>
              <span>{documentAuditActionLabel(log.action, t)}</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
