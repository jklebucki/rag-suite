import { useState } from 'react'
import { Briefcase, XCircle } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import { useManagerPanel } from '../hooks/useManagerPanel'
import type { ApprovalRequest } from '../types/managerTypes'
import { ApprovalRequestDetails } from './ApprovalRequestDetails'
import { DelegationSettings } from './DelegationSettings'
import { ManagerDashboard } from './ManagerDashboard'
import { ManagerTabs, type ManagerPanelTab } from './ManagerTabs'
import { PendingRequestsTable } from './PendingRequestsTable'
import { RejectionReasonModal } from './RejectionReasonModal'
import { TeamMembersTable } from './TeamMembersTable'

export function ManagerPanel() {
  const { t } = useI18n()
  const { data, isLoading, isMutating, error, approveRequest, rejectRequest, saveDelegation } =
    useManagerPanel()
  const [activeTab, setActiveTab] = useState<ManagerPanelTab>('dashboard')
  const [detailRequest, setDetailRequest] = useState<ApprovalRequest | null>(null)
  const [rejectionRequest, setRejectionRequest] = useState<ApprovalRequest | null>(null)

  async function handleApprove(requestId: string) {
    await approveRequest(requestId)
    setDetailRequest(null)
  }

  async function handleReject(requestId: string, reason: string) {
    await rejectRequest(requestId, reason)
    setRejectionRequest(null)
    setDetailRequest(null)
  }

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center text-gray-600 dark:text-gray-400">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary-200 border-t-primary-600" />
      </div>
    )
  }

  if (error || !data) {
    return (
      <div className="surface p-6 text-center text-red-600 dark:text-red-400">
        <XCircle className="mx-auto mb-2 h-8 w-8" />
        <p className="text-sm">{error ?? t('common.error')}</p>
      </div>
    )
  }

  return (
    <div className="space-y-6 text-gray-900 dark:text-gray-100">
      <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex items-center gap-3">
          <Briefcase className="h-8 w-8 flex-shrink-0 text-primary-600 dark:text-primary-400" />
          <div>
            <h1 className="text-2xl font-bold">{t('employeeDashboard.managerPanel')}</h1>
            <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
              {data.teamName} - {data.managerName}
            </p>
          </div>
        </div>
      </div>

      <ManagerTabs active={activeTab} onChange={setActiveTab} />

      {activeTab === 'dashboard' && <ManagerDashboard data={data} />}

      {activeTab === 'team' && <TeamMembersTable members={data.teamMembers} />}

      {activeTab === 'requests' && (
        <PendingRequestsTable
          requests={data.approvalRequests}
          isMutating={isMutating}
          onViewDetails={setDetailRequest}
          onApprove={handleApprove}
          onReject={setRejectionRequest}
        />
      )}

      {activeTab === 'delegation' && (
        <DelegationSettings
          teamMembers={data.teamMembers}
          activeDelegation={data.activeDelegation}
          delegations={data.delegations}
          isMutating={isMutating}
          onSave={saveDelegation}
        />
      )}

      <ApprovalRequestDetails
        request={detailRequest}
        isMutating={isMutating}
        onClose={() => setDetailRequest(null)}
        onApprove={handleApprove}
        onReject={setRejectionRequest}
      />

      <RejectionReasonModal
        request={rejectionRequest}
        isMutating={isMutating}
        onClose={() => setRejectionRequest(null)}
        onConfirm={handleReject}
      />
    </div>
  )
}
