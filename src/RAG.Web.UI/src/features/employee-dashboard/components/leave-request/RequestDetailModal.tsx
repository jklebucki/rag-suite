import { Modal } from '@/shared/components/ui/Modal'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { LeaveRequestRecord } from '../../types/leaveRequest'
import { formatDate, formatDateTime, leaveTypeLabel } from './leaveRequestUtils'
import { StatusBadge } from './StatusBadge'

interface RequestDetailModalProps {
  request: LeaveRequestRecord | null
  onClose: () => void
}

export function RequestDetailModal({ request, onClose }: RequestDetailModalProps) {
  const { t } = useI18n()
  if (!request) return null

  return (
    <Modal
      isOpen={!!request}
      onClose={onClose}
      title={t('employeeDashboard.leave.detail.title')}
      size="md"
    >
      <div className="p-6 space-y-4">
        <div className="flex items-center justify-between">
          <span className="text-sm text-gray-500 dark:text-gray-400">
            {t('employeeDashboard.leave.detail.status')}
          </span>
          <StatusBadge status={request.status} />
        </div>

        <DetailRow
          label={t('employeeDashboard.leave.detail.leaveType')}
          value={leaveTypeLabel(request.leaveType, t)}
        />
        <DetailRow
          label={t('employeeDashboard.leave.detail.company')}
          value={request.companyName}
        />
        <DetailRow
          label={t('employeeDashboard.leave.detail.dateFrom')}
          value={formatDate(request.dateFrom)}
        />
        <DetailRow
          label={t('employeeDashboard.leave.detail.dateTo')}
          value={formatDate(request.dateTo)}
        />
        <DetailRow
          label={t('employeeDashboard.leave.detail.daysCount')}
          value={`${request.daysCount} ${t('employeeDashboard.leave.balance.daysUnit')}`}
        />
        {request.substituteName && (
          <DetailRow
            label={t('employeeDashboard.leave.detail.substitute')}
            value={request.substituteName}
          />
        )}
        {request.comment && (
          <DetailRow
            label={t('employeeDashboard.leave.detail.comment')}
            value={request.comment}
          />
        )}
        <DetailRow
          label={t('employeeDashboard.leave.detail.createdAt')}
          value={formatDateTime(request.createdAt)}
        />

        {(request.reviewedAt || request.managerComment) && (
          <div className="pt-3 border-t border-gray-100 dark:border-slate-800 space-y-3">
            <p className="text-xs font-semibold text-gray-500 dark:text-gray-500 uppercase tracking-wider">
              {t('employeeDashboard.leave.detail.managerSection')}
            </p>
            {request.reviewedBy && (
              <DetailRow
                label={t('employeeDashboard.leave.detail.reviewedBy')}
                value={request.reviewedBy}
              />
            )}
            {request.reviewedAt && (
              <DetailRow
                label={t('employeeDashboard.leave.detail.reviewedAt')}
                value={formatDateTime(request.reviewedAt)}
              />
            )}
            {request.managerComment && (
              <DetailRow
                label={t('employeeDashboard.leave.detail.managerComment')}
                value={request.managerComment}
              />
            )}
          </div>
        )}
      </div>
    </Modal>
  )
}

interface DetailRowProps {
  label: string
  value: string
}

function DetailRow({ label, value }: DetailRowProps) {
  return (
    <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-1">
      <span className="text-sm text-gray-500 dark:text-gray-400 flex-shrink-0">{label}</span>
      <span className="text-sm font-medium text-gray-900 dark:text-gray-100 sm:text-right">
        {value}
      </span>
    </div>
  )
}
