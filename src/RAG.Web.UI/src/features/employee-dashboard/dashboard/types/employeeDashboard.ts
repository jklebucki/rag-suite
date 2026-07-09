// ---------------------------------------------------------------------------
// Employee Dashboard – domain types
// All shapes mirror the expected API/ERP contract so that the mock service
// can be replaced by a real HTTP service without touching the UI layer.
// ---------------------------------------------------------------------------

export interface EmployeeProfile {
  id: string
  firstName: string
  lastName: string
  fullName: string
  position: string
  department: string
  supervisor: string
  hireDate: string        // ISO date string
  phone: string
  email: string
  avatarUrl?: string
  lastLoginAt?: string    // ISO datetime string
}

export interface LeaveBalance {
  annual: number          // Annual leave days remaining
  carryover: number       // Carried-over leave days
  onDemand: number        // On-demand leave days remaining
  total: number           // Sum of all available days
}

export interface LastPayslip {
  grossAmount: number
  netAmount: number
  currency: string        // e.g. "PLN"
  paymentDate: string     // ISO date string
  periodLabel: string     // e.g. "May 2025"
  downloadUrl?: string    // URL for PDF download (null = not yet available)
}

export type HrRequestStatus = 'pending' | 'approved' | 'rejected'

export interface HrRequestsSummary {
  pending: number
  approved: number
  rejected: number
  total: number
}

export type ExpiringDocumentStatus = 'critical' | 'warning' | 'ok'

export interface ExpiringDocument {
  id: string
  documentType: string
  documentName: string
  validFrom?: string      // ISO date string
  validTo: string         // ISO date string
  requiresAction: boolean
  daysUntilExpiry: number
  status: ExpiringDocumentStatus
}

export type NotificationSeverity = 'info' | 'warning' | 'error' | 'success'

export type NotificationCategory =
  | 'leave'
  | 'payslip'
  | 'medical'
  | 'training'
  | 'general'

export interface DashboardNotification {
  id: string
  date: string                        // ISO datetime string
  category: NotificationCategory
  title: string
  description: string
  severity: NotificationSeverity
  isRead: boolean
}

export type UpcomingEventType =
  | 'leave'
  | 'medical'
  | 'bhp_training'
  | 'delegation'
  | 'organization'

export interface UpcomingEvent {
  id: string
  type: UpcomingEventType
  title: string
  startDate: string       // ISO date string
  endDate?: string        // ISO date string (optional, for multi-day events)
  description?: string
}

// ---------------------------------------------------------------------------
// Aggregated DTO returned by the service / API endpoint
// ---------------------------------------------------------------------------
export interface EmployeeDashboardData {
  profile: EmployeeProfile
  leaveBalance: LeaveBalance
  lastPayslip: LastPayslip
  hrRequestsSummary: HrRequestsSummary
  expiringDocuments: ExpiringDocument[]
  notifications: DashboardNotification[]
  upcomingEvents: UpcomingEvent[]
}
