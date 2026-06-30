// ---------------------------------------------------------------------------
// Leave Request – domain types
//
// All shapes mirror the expected API/ERP contract so that the mock service
// can be replaced by a real HTTP call without touching the UI layer.
//
// Example real implementation:
//   const response = await apiClient.get<LeaveRequestPageData>(
//     `/api/v1/employees/${userId}/leave-requests`
//   )
//   return response.data
// ---------------------------------------------------------------------------

export type LeaveType =
  | 'annual'
  | 'onDemand'
  | 'occasional'
  | 'childCare'
  | 'homeOffice'
  | 'delegation'

export type LeaveRequestStatus =
  | 'pending'
  | 'approved'
  | 'rejected'
  | 'cancelled'

export interface LeaveBalance {
  annual: number
  carryover: number
  onDemand: number
  total: number
}

export interface LeaveSubstitute {
  id: string
  fullName: string
  position: string
}

// DTO representing a single leave request record
export interface LeaveRequestRecord {
  id: string
  companyId: string
  companyName: string
  leaveType: LeaveType
  dateFrom: string           // ISO date string
  dateTo: string             // ISO date string
  daysCount: number
  status: LeaveRequestStatus
  substituteId?: string
  substituteName?: string
  comment?: string
  attachmentName?: string    // filename if attachment was added
  createdAt: string          // ISO datetime string
  // Fields populated after manager action (ready for next phase)
  reviewedAt?: string        // ISO datetime string
  reviewedBy?: string        // Manager full name
  managerComment?: string
}

// Form model – matches the shape the create endpoint will expect
export interface CreateLeaveRequestPayload {
  leaveType: LeaveType
  dateFrom: string           // ISO date string
  dateTo: string             // ISO date string
  daysCount: number
  substituteId?: string
  comment?: string
  // attachment handled separately as multipart/form-data
}

// Aggregated DTO returned by the service / API endpoint
export interface LeaveRequestPageData {
  leaveBalance: LeaveBalance
  requests: LeaveRequestRecord[]
  substitutes: LeaveSubstitute[]
}
