import { History } from 'lucide-react'
import type { OperationLogEntry } from '../types/managerTypes'
import { formatDateTime } from './managerPanelUtils'
import { useManagerT } from './managerTranslations'

interface AuditLogTableProps {
  logs: OperationLogEntry[]
}

export function AuditLogTable({ logs }: AuditLogTableProps) {
  const t = useManagerT()

  return (
    <div className="surface overflow-hidden">
      <div className="flex items-center gap-2 border-b border-gray-100 px-5 py-4 dark:border-slate-800">
        <div className="rounded-lg bg-primary-50 p-2 dark:bg-primary-900/20">
          <History className="h-5 w-5 text-primary-600 dark:text-primary-400" />
        </div>
        <h2 className="font-semibold text-gray-900 dark:text-gray-100">{t('audit.title')}</h2>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-gray-100 bg-gray-50 dark:border-slate-800 dark:bg-slate-900/50">
              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                {t('audit.date')}
              </th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                {t('audit.user')}
              </th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                {t('audit.operation')}
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
                  {formatDateTime(log.date)}
                </td>
                <td className="whitespace-nowrap px-4 py-3.5 font-medium text-gray-900 dark:text-gray-100">
                  {log.user}
                </td>
                <td className="px-4 py-3.5 text-gray-600 dark:text-gray-300">{log.operation}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
