import type {
  HrAbsenceLeaveType,
  HrAbsenceRequest,
  HrAbsenceStatus,
} from '../types/hrAbsenceTypes'
import type { useHrAbsenceT } from './hrAbsenceTranslations'

type HrAbsenceT = ReturnType<typeof useHrAbsenceT>

export function formatHrAbsenceDate(value: string): string {
  return new Intl.DateTimeFormat('pl-PL', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(new Date(value))
}

export function formatHrAbsenceDateTime(value: string): string {
  return new Intl.DateTimeFormat('pl-PL', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}

export function isHrAbsenceOverdue(request: HrAbsenceRequest, now = new Date()): boolean {
  return request.status === 'pending' && new Date(`${request.dateTo}T23:59:59`).getTime() < now.getTime()
}

export function hrAbsenceLeaveTypeLabel(type: HrAbsenceLeaveType, t: HrAbsenceT): string {
  const keys: Record<HrAbsenceLeaveType, Parameters<HrAbsenceT>[0]> = {
    annual: 'leaveType.annual',
    onDemand: 'leaveType.onDemand',
    occasional: 'leaveType.occasional',
    childCare: 'leaveType.childCare',
  }
  return t(keys[type])
}

export function hrAbsenceStatusLabel(status: HrAbsenceStatus, t: HrAbsenceT): string {
  const keys: Record<HrAbsenceStatus, Parameters<HrAbsenceT>[0]> = {
    pending: 'status.pending',
    approved: 'status.approved',
    rejected: 'status.rejected',
  }
  return t(keys[status])
}

export function hrAbsenceActionLabel(request: HrAbsenceRequest, t: HrAbsenceT): string {
  if (isHrAbsenceOverdue(request)) return t('action.overdue')
  if (request.requiresPayrollClosure) return t('action.payrollClosure')
  return t('action.none')
}
