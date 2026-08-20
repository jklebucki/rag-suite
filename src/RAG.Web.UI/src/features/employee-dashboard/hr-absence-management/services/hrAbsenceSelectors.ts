import type { HrAbsenceRequest } from '../types/hrAbsenceTypes'
import { isHrAbsenceOverdue } from '../components/hrAbsenceUtils'

export interface HrAbsenceStatistics {
  all: number
  pending: number
  payrollClosure: number
  overdue: number
}

export function selectHrAbsenceRequestsByCompany(
  requests: HrAbsenceRequest[],
  companyId: string | null
): HrAbsenceRequest[] {
  if (!companyId) return []
  return requests.filter((request) => request.companyId === companyId)
}

export function getHrAbsenceStatistics(requests: HrAbsenceRequest[]): HrAbsenceStatistics {
  return {
    all: requests.length,
    pending: requests.filter((request) => request.status === 'pending').length,
    payrollClosure: requests.filter((request) => request.requiresPayrollClosure).length,
    overdue: requests.filter((request) => isHrAbsenceOverdue(request)).length,
  }
}
