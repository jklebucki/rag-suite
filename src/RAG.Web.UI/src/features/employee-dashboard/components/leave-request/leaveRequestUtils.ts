import type { TranslationKeys } from '@/shared/types/i18n'
import type { LeaveType } from '../../types/leaveRequest'

export function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('pl-PL', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  })
}

export function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleDateString('pl-PL', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  })
}

export function countWorkingDays(from: string, to: string): number {
  if (!from || !to) return 0
  const start = new Date(from)
  const end = new Date(to)
  if (end < start) return 0
  let count = 0
  const current = new Date(start)
  while (current <= end) {
    const day = current.getDay()
    if (day !== 0 && day !== 6) count++
    current.setDate(current.getDate() + 1)
  }
  return count
}

export function leaveTypeLabel(
  type: LeaveType,
  t: (key: keyof TranslationKeys) => string
): string {
  const map: Record<LeaveType, string> = {
    annual: t('employeeDashboard.leave.type.annual'),
    onDemand: t('employeeDashboard.leave.type.onDemand'),
    occasional: t('employeeDashboard.leave.type.occasional'),
    childCare: t('employeeDashboard.leave.type.childCare'),
    homeOffice: t('employeeDashboard.leave.type.homeOffice'),
    delegation: t('employeeDashboard.leave.type.delegation'),
  }
  return map[type] ?? type
}
