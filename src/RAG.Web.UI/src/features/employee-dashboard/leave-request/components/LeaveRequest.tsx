import { useState } from 'react'
import { CalendarDays, XCircle } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import { useLeaveRequest } from '../hooks/useLeaveRequest'
import type { LeaveRequestRecord } from '../types/leaveRequest'
import {
  LeaveBalanceCard,
  LeaveHistoryTable,
  LeaveRequestForm,
  RequestDetailModal,
  TabBar,
  type LeaveRequestTab,
} from '.'

export function LeaveRequest() {
  const { t } = useI18n()
  const { data, isLoading, isSubmitting, isCancelling, error, submitRequest, cancelRequest } =
    useLeaveRequest()
  const [activeTab, setActiveTab] = useState<LeaveRequestTab>('new')
  const [detailRequest, setDetailRequest] = useState<LeaveRequestRecord | null>(null)

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-48">
        <div className="h-8 w-8 rounded-full border-4 border-primary-200 border-t-primary-600 animate-spin" />
      </div>
    )
  }

  if (error) {
    return (
      <div className="surface p-6 text-center text-red-600 dark:text-red-400">
        <XCircle className="h-8 w-8 mx-auto mb-2" />
        <p className="text-sm">{error}</p>
      </div>
    )
  }

  const balance = data?.leaveBalance ?? { annual: 0, carryover: 0, onDemand: 0, total: 0 }
  const requests = data?.requests ?? []
  const substitutes = data?.substitutes ?? []

  return (
    <div className="space-y-5 text-gray-900 dark:text-gray-100">
      <div className="flex items-center gap-3">
        <CalendarDays className="h-8 w-8 text-primary-600 dark:text-primary-400" />
        <h1 className="text-2xl font-bold">{t('employeeDashboard.leaveRequest')}</h1>
      </div>

      <TabBar active={activeTab} onChange={setActiveTab} />

      {activeTab === 'new' && (
        <div className="space-y-5">
          <LeaveBalanceCard
            annual={balance.annual}
            carryover={balance.carryover}
            onDemand={balance.onDemand}
            total={balance.total}
          />
          <LeaveRequestForm
            substitutes={substitutes}
            isSubmitting={isSubmitting}
            onSubmit={submitRequest}
          />
        </div>
      )}

      {activeTab === 'history' && (
        <LeaveHistoryTable
          requests={requests}
          isCancelling={isCancelling}
          onViewDetail={setDetailRequest}
          onCancel={cancelRequest}
        />
      )}

      <RequestDetailModal
        request={detailRequest}
        onClose={() => setDetailRequest(null)}
      />
    </div>
  )
}
