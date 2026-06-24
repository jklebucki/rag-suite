import type {
  DelegationStatus,
  ManagerApprovalStatus,
  ManagerLeaveType,
  TeamMemberPresenceStatus,
} from '../types/managerTypes'
import type { useManagerT } from './managerTranslations'

type ManagerT = ReturnType<typeof useManagerT>

export function formatDate(value: string): string {
  return new Intl.DateTimeFormat('pl-PL', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(new Date(value))
}

export function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat('pl-PL', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}

export function leaveTypeLabel(type: ManagerLeaveType, t: ManagerT): string {
  const labels: Record<ManagerLeaveType, Parameters<ManagerT>[0]> = {
    annual: 'leaveType.annual',
    onDemand: 'leaveType.onDemand',
    occasional: 'leaveType.occasional',
    childCare: 'leaveType.childCare',
    homeOffice: 'leaveType.homeOffice',
    delegation: 'leaveType.delegation',
  }
  return t(labels[type])
}

export function presenceStatusLabel(status: TeamMemberPresenceStatus, t: ManagerT): string {
  const labels: Record<TeamMemberPresenceStatus, Parameters<ManagerT>[0]> = {
    present: 'status.presence.present',
    vacation: 'status.presence.vacation',
    delegation: 'status.presence.delegation',
    homeOffice: 'status.presence.homeOffice',
    absence: 'status.presence.absence',
  }
  return t(labels[status])
}

export function approvalStatusLabel(status: ManagerApprovalStatus, t: ManagerT): string {
  const labels: Record<ManagerApprovalStatus, Parameters<ManagerT>[0]> = {
    pending: 'status.approval.pending',
    approved: 'status.approval.approved',
    rejected: 'status.approval.rejected',
  }
  return t(labels[status])
}

export function delegationStatusLabel(status: DelegationStatus, t: ManagerT): string {
  const labels: Record<DelegationStatus, Parameters<ManagerT>[0]> = {
    active: 'status.delegation.active',
    scheduled: 'status.delegation.scheduled',
    expired: 'status.delegation.expired',
  }
  return t(labels[status])
}
