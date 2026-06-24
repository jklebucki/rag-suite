import { Clock } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { DataChangeRequest } from '../types/personalData'
import { StatusBadge } from './StatusBadge'

interface RequestHistoryTableProps {
  requests: DataChangeRequest[]
}

export function RequestHistoryTable({ requests }: RequestHistoryTableProps) {
  const { t } = useI18n()

  return (
    <div className="surface p-5">
      <div className="flex items-center gap-2 mb-4">
        <div className="p-2 bg-gray-100 dark:bg-slate-800 rounded-lg">
          <Clock className="h-5 w-5 text-gray-600 dark:text-gray-400" />
        </div>
        <h2 className="font-semibold text-gray-900 dark:text-gray-100">
          {t('employeeDashboard.personal.history.title')}
        </h2>
      </div>

      {requests.length === 0 ? (
        <p className="text-sm text-gray-500 dark:text-gray-400 py-4 text-center">
          {t('employeeDashboard.personal.history.empty')}
        </p>
      ) : (
        <div className="overflow-x-auto -mx-5">
          <table className="w-full text-sm min-w-[560px]">
            <thead>
              <tr className="border-b border-gray-100 dark:border-slate-800">
                <th className="px-5 py-2.5 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                  {t('employeeDashboard.personal.history.date')}
                </th>
                <th className="px-5 py-2.5 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                  {t('employeeDashboard.personal.history.changeType')}
                </th>
                <th className="px-5 py-2.5 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                  {t('employeeDashboard.personal.history.status')}
                </th>
                <th className="px-5 py-2.5 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                  {t('employeeDashboard.personal.history.comment')}
                </th>
              </tr>
            </thead>
            <tbody>
              {requests.map((request) => (
                <tr
                  key={request.id}
                  className="border-b border-gray-50 dark:border-slate-800/60 last:border-0 hover:bg-gray-50/60 dark:hover:bg-slate-800/30 transition-colors"
                >
                  <td className="px-5 py-3 text-gray-700 dark:text-gray-300 whitespace-nowrap">
                    {new Date(request.date).toLocaleDateString('pl-PL', {
                      day: '2-digit',
                      month: '2-digit',
                      year: 'numeric',
                    })}
                  </td>
                  <td className="px-5 py-3 text-gray-700 dark:text-gray-300">
                    {t(`employeeDashboard.personal.changeType.${request.changeType}` as Parameters<typeof t>[0])}
                  </td>
                  <td className="px-5 py-3">
                    <StatusBadge status={request.status} />
                  </td>
                  <td className="px-5 py-3 text-gray-600 dark:text-gray-400 max-w-xs">
                    {request.comment ?? '-'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
