import { AlertTriangle } from 'lucide-react'
import { Button } from '@/shared/components/ui/Button'
import { Modal } from '@/shared/components/ui/Modal'
import type { ApprovalRequest } from '../types/managerTypes'
import { ManagerStatusBadge } from './ManagerStatusBadge'
import { formatDate, formatDateTime, leaveTypeLabel } from './managerPanelUtils'
import { useManagerT } from './managerTranslations'

interface ApprovalRequestDetailsProps {
  request: ApprovalRequest | null
  isMutating: boolean
  onClose: () => void
  onApprove: (requestId: string) => void
  onReject: (request: ApprovalRequest) => void
}

export function ApprovalRequestDetails({
  request,
  isMutating,
  onClose,
  onApprove,
  onReject,
}: ApprovalRequestDetailsProps) {
  const t = useManagerT()
  if (!request) return null

  return (
    <Modal
      isOpen={!!request}
      onClose={onClose}
      title={t('details.title')}
      size="md"
    >
      <div className="space-y-4 p-6">
        <div className="flex items-center justify-between gap-3">
          <span className="text-sm text-gray-500 dark:text-gray-400">{t('details.status')}</span>
          <ManagerStatusBadge type="approval" status={request.status} />
        </div>

        <DetailRow label={t('details.employee')} value={request.employeeName} />
        <DetailRow label={t('details.leaveType')} value={leaveTypeLabel(request.leaveType, t)} />
        <DetailRow label={t('details.dateFrom')} value={formatDate(request.dateFrom)} />
        <DetailRow label={t('details.dateTo')} value={formatDate(request.dateTo)} />
        <DetailRow label={t('details.daysCount')} value={`${request.daysCount} ${t('common.days')}`} />
        <DetailRow label={t('details.submittedAt')} value={formatDateTime(request.submittedAt)} />

        <div>
          <p className="mb-1.5 text-sm text-gray-500 dark:text-gray-400">{t('details.employeeComment')}</p>
          <div className="rounded-xl border border-gray-100 bg-gray-50 p-3 text-sm leading-6 text-gray-700 dark:border-slate-800 dark:bg-slate-950 dark:text-gray-300">
            {request.employeeComment}
          </div>
        </div>

        {request.hasConflict && (
          <div className="flex gap-3 rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800 dark:border-amber-800 dark:bg-amber-900/20 dark:text-amber-300">
            <AlertTriangle className="mt-0.5 h-4 w-4 flex-shrink-0" />
            <div>
              <p className="font-semibold">{t('details.conflictTitle')}</p>
              <p className="mt-1 leading-5">{request.conflictNote}</p>
            </div>
          </div>
        )}

        {request.status === 'pending' && (
          <div className="flex flex-col-reverse gap-3 border-t border-gray-100 pt-4 dark:border-slate-800 sm:flex-row sm:justify-end">
            <Button
              type="button"
              variant="outline"
              disabled={isMutating}
              onClick={() => onReject(request)}
            >
              {t('requests.reject')}
            </Button>
            <Button
              type="button"
              variant="primary"
              disabled={isMutating}
              onClick={() => onApprove(request.id)}
            >
              {t('requests.approve')}
            </Button>
          </div>
        )}
      </div>
    </Modal>
  )
}

function DetailRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col gap-1 sm:flex-row sm:items-start sm:justify-between">
      <span className="text-sm text-gray-500 dark:text-gray-400">{label}</span>
      <span className="text-sm font-medium text-gray-900 dark:text-gray-100 sm:text-right">
        {value}
      </span>
    </div>
  )
}
