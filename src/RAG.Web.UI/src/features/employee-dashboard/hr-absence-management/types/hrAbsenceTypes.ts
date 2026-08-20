export type HrAbsenceStatus = 'pending' | 'approved' | 'rejected'

export type HrAbsenceLeaveType = 'annual' | 'onDemand' | 'occasional' | 'childCare'

export interface HrAbsenceRequest {
  id: string
  companyId: string
  employeeId: string
  employeeName: string
  leaveType: HrAbsenceLeaveType
  dateFrom: string
  dateTo: string
  days: number
  submittedAt: string
  managerName: string
  status: HrAbsenceStatus
  requiresPayrollClosure: boolean
}

export type HrAbsenceRequestCategory = 'payrollClosure' | 'overdue'

export interface HrAbsenceRequestFilters {
  status?: HrAbsenceStatus
  category?: HrAbsenceRequestCategory
}
