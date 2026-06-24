import { CalendarDays, ChevronRight, X } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { LeaveRequestRecord } from '../types/leaveRequest'
import { formatDate, formatDateTime, leaveTypeLabel } from './leaveRequestUtils'
import { StatusBadge } from './StatusBadge'

interface LeaveHistoryTableProps {
  requests: LeaveRequestRecord[]
  isCancelling: boolean
  onViewDetail: (request: LeaveRequestRecord) => void
  onCancel: (requestId: string) => void
}

export function LeaveHistoryTable({
  requests,
  isCancelling,
  onViewDetail,
  onCancel,
}: LeaveHistoryTableProps) {
  const { t } = useI18n()

  if (requests.length === 0) {
    return (
      <div className="surface p-10 text-center">
        <CalendarDays className="h-10 w-10 text-gray-300 dark:text-slate-600 mx-auto mb-3" />
        <p className="text-sm text-gray-500 dark:text-gray-400">
          {t('employeeDashboard.leave.history.empty')}
        </p>
      </div>
    )
  }

  return (
    <div className="surface overflow-hidden">
      <div className="px-5 py-4 border-b border-gray-100 dark:border-slate-800 flex items-center gap-2">
        <div className="p-2 bg-primary-50 dark:bg-primary-900/20 rounded-lg">
          <CalendarDays className="h-5 w-5 text-primary-600 dark:text-primary-400" />
        </div>
        <h2 className="font-semibold text-gray-900 dark:text-gray-100">
          {t('employeeDashboard.leave.history.title')}
        </h2>
        <span className="ml-auto text-xs text-gray-400 dark:text-gray-500 tabular-nums">
          {requests.length} {t('employeeDashboard.leave.history.recordsCount')}
        </span>
      </div>

      <div className="hidden md:block overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-gray-100 dark:border-slate-800 bg-gray-50 dark:bg-slate-900/50">
              <th className="text-left px-5 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                {t('employeeDashboard.leave.history.col.leaveType')}
              </th>
              <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                {t('employeeDashboard.leave.history.col.dateFrom')}
              </th>
              <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                {t('employeeDashboard.leave.history.col.dateTo')}
              </th>
              <th className="text-center px-4 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                {t('employeeDashboard.leave.history.col.daysCount')}
              </th>
              <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                {t('employeeDashboard.leave.history.col.status')}
              </th>
              <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                {t('employeeDashboard.leave.history.col.createdAt')}
              </th>
              <th className="text-right px-5 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                {t('employeeDashboard.leave.history.col.actions')}
              </th>
            </tr>
          </thead>
          <tbody>
            {requests.map((request) => (
              <tr
                key={request.id}
                className="border-b border-gray-50 dark:border-slate-800 hover:bg-gray-50 dark:hover:bg-slate-800/50 transition-colors last:border-0"
              >
                <td className="px-5 py-3.5 font-medium text-gray-900 dark:text-gray-100">
                  {leaveTypeLabel(request.leaveType, t)}
                </td>
                <td className="px-4 py-3.5 text-gray-600 dark:text-gray-300 tabular-nums">
                  {formatDate(request.dateFrom)}
                </td>
                <td className="px-4 py-3.5 text-gray-600 dark:text-gray-300 tabular-nums">
                  {formatDate(request.dateTo)}
                </td>
                <td className="px-4 py-3.5 text-center">
                  <span className="font-semibold tabular-nums text-gray-900 dark:text-gray-100">
                    {request.daysCount}
                  </span>
                </td>
                <td className="px-4 py-3.5">
                  <StatusBadge status={request.status} />
                </td>
                <td className="px-4 py-3.5 text-gray-500 dark:text-gray-400 tabular-nums text-xs">
                  {formatDateTime(request.createdAt)}
                </td>
                <td className="px-5 py-3.5">
                  <div className="flex items-center justify-end gap-2">
                    <button
                      onClick={() => onViewDetail(request)}
                      className="inline-flex items-center gap-1 text-xs font-medium text-primary-600 dark:text-primary-400 hover:text-primary-700 dark:hover:text-primary-300 transition-colors"
                    >
                      <ChevronRight className="h-3.5 w-3.5" />
                      {t('employeeDashboard.leave.history.actions.details')}
                    </button>
                    {request.status === 'pending' && (
                      <button
                        onClick={() => onCancel(request.id)}
                        disabled={isCancelling}
                        className="inline-flex items-center gap-1 text-xs font-medium text-red-500 dark:text-red-400 hover:text-red-600 dark:hover:text-red-300 transition-colors disabled:opacity-50 disabled:pointer-events-none"
                      >
                        <X className="h-3.5 w-3.5" />
                        {t('employeeDashboard.leave.history.actions.cancel')}
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="md:hidden divide-y divide-gray-100 dark:divide-slate-800">
        {requests.map((request) => (
          <div key={request.id} className="px-4 py-4 space-y-2">
            <div className="flex items-start justify-between gap-2">
              <span className="text-sm font-medium text-gray-900 dark:text-gray-100">
                {leaveTypeLabel(request.leaveType, t)}
              </span>
              <StatusBadge status={request.status} />
            </div>
            <div className="flex items-center gap-3 text-xs text-gray-500 dark:text-gray-400">
              <span className="tabular-nums">{formatDate(request.dateFrom)}</span>
              <span>-</span>
              <span className="tabular-nums">{formatDate(request.dateTo)}</span>
              <span className="font-semibold text-gray-700 dark:text-gray-300">
                {request.daysCount} {t('employeeDashboard.leave.balance.daysUnit')}
              </span>
            </div>
            <div className="flex items-center gap-3 pt-1">
              <button
                onClick={() => onViewDetail(request)}
                className="text-xs font-medium text-primary-600 dark:text-primary-400 hover:underline"
              >
                {t('employeeDashboard.leave.history.actions.details')}
              </button>
              {request.status === 'pending' && (
                <button
                  onClick={() => onCancel(request.id)}
                  disabled={isCancelling}
                  className="text-xs font-medium text-red-500 dark:text-red-400 hover:underline disabled:opacity-50"
                >
                  {t('employeeDashboard.leave.history.actions.cancel')}
                </button>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
