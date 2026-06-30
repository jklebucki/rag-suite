export type EmploymentContextStatus = 'active' | 'ended' | 'suspended'

export interface EmploymentContextOption {
  id: string
  companyName: string
  position: string
  status: EmploymentContextStatus
  startDate: string
  companyCode: string
  employmentType: string
}
