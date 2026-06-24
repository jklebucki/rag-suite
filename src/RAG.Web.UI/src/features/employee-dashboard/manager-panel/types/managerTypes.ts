export type TeamMemberPresenceStatus =
  | 'present'
  | 'vacation'
  | 'delegation'
  | 'homeOffice'
  | 'absence'

export type ManagerApprovalStatus = 'pending' | 'approved' | 'rejected'

export type ManagerLeaveType =
  | 'annual'
  | 'onDemand'
  | 'occasional'
  | 'childCare'
  | 'homeOffice'
  | 'delegation'

export type DelegationStatus = 'active' | 'scheduled' | 'expired'

export interface TeamMember {
  id: string
  fullName: string
  position: string
  department: string
  seniority: string
  presenceStatus: TeamMemberPresenceStatus
  remainingLeaveDays: number
  absenceDaysThisYear: number
  email: string
  phone: string
  currentProject: string
}

export interface ApprovalRequest {
  id: string
  employeeId: string
  employeeName: string
  leaveType: ManagerLeaveType
  dateFrom: string
  dateTo: string
  daysCount: number
  submittedAt: string
  status: ManagerApprovalStatus
  employeeComment: string
  hasConflict: boolean
  conflictNote?: string
}

export interface OperationLogEntry {
  id: string
  date: string
  user: string
  operation: string
}

export interface ApprovalDelegation {
  id: string
  substituteId: string
  substituteName: string
  dateFrom: string
  dateTo: string
  status: DelegationStatus
  createdAt: string
}

export interface TeamStatistics {
  directReports: number
  pendingRequests: number
  absentToday: number
  absentNextSevenDays: number
  vacationConflicts: number
}

export interface ManagerPanelData {
  managerName: string
  teamName: string
  statistics: TeamStatistics
  teamMembers: TeamMember[]
  approvalRequests: ApprovalRequest[]
  operationLogs: OperationLogEntry[]
  delegations: ApprovalDelegation[]
  activeDelegation: ApprovalDelegation | null
}

export interface DelegationPayload {
  substituteId: string
  dateFrom: string
  dateTo: string
}
