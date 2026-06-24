import { AlertTriangle, ChevronRight, ClipboardCheck } from 'lucide-react'
import { Button } from '@/shared/components/ui/Button'
import type { ApprovalRequest } from '../types/managerTypes'
import { ManagerStatusBadge } from './ManagerStatusBadge'
import { formatDate, formatDateTime, leaveTypeLabel } from './managerPanelUtils'
import { useManagerT } from './managerTranslations'

interface PendingRequestsTableProps {
  requests: ApprovalRequest[]
  isMutating: boolean
  onViewDetails: (request: ApprovalRequest) => void
  onApprove: (requestId: string) => void
  onReject: (request: ApprovalRequest) => void
}

export function PendingRequestsTable({
  requests,
  isMutating,
  onViewDetails,
  onApprove,
  onReject,
}: PendingRequestsTableProps) {
  const t = useManagerT()
  const pendingCount = requests.filter((request) => request.status === 'pending').length

  return (
    <div className="surface overflow-hidden">
      <div className="flex items-center gap-2 border-b border-gray-100 px-5 py-4 dark:border-slate-800">
        <div className="rounded-lg bg-primary-50 p-2 dark:bg-primary-900/20">
          <ClipboardCheck className="h-5 w-5 text-primary-600 dark:text-primary-400" />
        </div>
        <div>
          <h2 className="font-semibold text-gray-900 dark:text-gray-100">{t('requests.title')}</h2>
          <p className="text-xs text-gray-500 dark:text-gray-400">
            {t('requests.summary', { pending: pendingCount, total: requests.length })}
          </p>
        </div>
      </div>

      <div className="hidden overflow-x-auto xl:block">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-gray-100 bg-gray-50 dark:border-slate-800 dark:bg-slate-900/50">
              <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                {t('requests.col.employee')}
              </th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                {t('requests.col.type')}
              </th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                {t('requests.col.dateFrom')}
              </th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                {t('requests.col.dateTo')}
              </th>
              <th className="px-4 py-3 text-center text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                {t('requests.col.days')}
              </th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                {t('requests.col.submittedAt')}
              </th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                {t('requests.col.status')}
              </th>
              <th className="px-5 py-3 text-right text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                {t('requests.col.actions')}
              </th>
            </tr>
          </thead>
          <tbody>
            {requests.map((request) => (
              <tr
                key={request.id}
                className="border-b border-gray-50 transition-colors last:border-0 hover:bg-gray-50 dark:border-slate-800 dark:hover:bg-slate-800/50"
              >
                <td className="px-5 py-3.5">
                  <div className="flex items-center gap-2 font-medium text-gray-900 dark:text-gray-100">
                    {request.hasConflict && <AlertTriangle className="h-4 w-4 text-amber-500" />}
                    {request.employeeName}
                  </div>
                </td>
                <td className="px-4 py-3.5 text-gray-600 dark:text-gray-300">{leaveTypeLabel(request.leaveType, t)}</td>
                <td className="whitespace-nowrap px-4 py-3.5 tabular-nums text-gray-600 dark:text-gray-300">
                  {formatDate(request.dateFrom)}
                </td>
                <td className="whitespace-nowrap px-4 py-3.5 tabular-nums text-gray-600 dark:text-gray-300">
                  {formatDate(request.dateTo)}
                </td>
                <td className="px-4 py-3.5 text-center font-semibold tabular-nums text-gray-900 dark:text-gray-100">
                  {request.daysCount}
                </td>
                <td className="whitespace-nowrap px-4 py-3.5 text-xs tabular-nums text-gray-500 dark:text-gray-400">
                  {formatDateTime(request.submittedAt)}
                </td>
                <td className="px-4 py-3.5">
                  <ManagerStatusBadge type="approval" status={request.status} />
                </td>
                <td className="px-5 py-3.5">
                  <div className="flex items-center justify-end gap-2">
                    <button
                      type="button"
                      onClick={() => onViewDetails(request)}
                      className="inline-flex items-center gap-1 text-xs font-medium text-primary-600 transition-colors hover:text-primary-700 dark:text-primary-400 dark:hover:text-primary-300"
                    >
                      <ChevronRight className="h-3.5 w-3.5" />
                      {t('requests.details')}
                    </button>
                    {request.status === 'pending' && (
                      <>
                        <Button
                          type="button"
                          size="sm"
                          variant="outline"
                          disabled={isMutating}
                          onClick={() => onReject(request)}
                        >
                          {t('requests.reject')}
                        </Button>
                        <Button
                          type="button"
                          size="sm"
                          variant="primary"
                          disabled={isMutating}
                          onClick={() => onApprove(request.id)}
                        >
                          {t('requests.approve')}
                        </Button>
                      </>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="divide-y divide-gray-100 dark:divide-slate-800 xl:hidden">
        {requests.map((request) => (
          <div key={request.id} className="space-y-3 px-4 py-4">
            <div className="flex items-start justify-between gap-3">
              <div>
                <div className="flex items-center gap-2 font-medium text-gray-900 dark:text-gray-100">
                  {request.hasConflict && <AlertTriangle className="h-4 w-4 text-amber-500" />}
                  {request.employeeName}
                </div>
                <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">{leaveTypeLabel(request.leaveType, t)}</p>
              </div>
              <ManagerStatusBadge type="approval" status={request.status} />
            </div>
            <div className="flex flex-wrap gap-x-4 gap-y-1 text-xs text-gray-500 dark:text-gray-400">
              <span>{formatDate(request.dateFrom)} - {formatDate(request.dateTo)}</span>
              <span>{request.daysCount} {t('common.days')}</span>
              <span>{t('requests.submittedShort')}: {formatDate(request.submittedAt)}</span>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              <Button type="button" size="sm" variant="ghost" onClick={() => onViewDetails(request)}>
                {t('requests.details')}
              </Button>
              {request.status === 'pending' && (
                <>
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    disabled={isMutating}
                    onClick={() => onReject(request)}
                  >
                    {t('requests.reject')}
                  </Button>
                  <Button
                    type="button"
                    size="sm"
                    variant="primary"
                    disabled={isMutating}
                    onClick={() => onApprove(request.id)}
                  >
                    {t('requests.approve')}
                  </Button>
                </>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
