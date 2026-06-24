// ---------------------------------------------------------------------------
// Employee Personal Data – domain types
//
// All shapes mirror the expected API/ERP contract so that the mock service
// can be replaced by a real HTTP call without touching the UI layer.
// ---------------------------------------------------------------------------

export interface Address {
  street: string
  buildingNumber: string
  apartmentNumber?: string
  postalCode: string
  city: string
}

export interface BasicInfo {
  firstName: string
  lastName: string
  employeeCode: string
  birthDate: string     // ISO date string
  position: string
  department: string
  supervisor: string
  hireDate: string      // ISO date string
}

export interface ContactInfo {
  workEmail: string
  privateEmail: string
  workPhone: string
  privatePhone: string
  residenceAddress: Address
  correspondenceAddress: Address
}

export interface EmploymentInfo {
  id: string            // Unique identifier for this employment record
  company: string
  organizationalUnit: string
  costCenter: string
  contractType: string
  workTimeFraction: string
  employmentStatus: string
  hireDate: string      // ISO date string
}

export interface EmergencyContact {
  fullName: string
  relationship: string
  phone: string
}

// ---------------------------------------------------------------------------
// Aggregated DTO returned by the service / API endpoint
// ---------------------------------------------------------------------------
export interface EmployeePersonalData {
  basicInfo: BasicInfo
  contactInfo: ContactInfo
  // Array – employee may have parallel employments across companies
  employments: EmploymentInfo[]
  emergencyContact: EmergencyContact
}

export type ChangeRequestType =
  | 'residenceAddress'
  | 'correspondenceAddress'
  | 'privatePhone'
  | 'privateEmail'
  | 'lastName'
  | 'emergencyContact'
  | 'other'

export type ChangeRequestStatus = 'pending' | 'approved' | 'rejected'

export interface DataChangeRequest {
  id: string
  date: string          // ISO datetime string
  changeType: ChangeRequestType
  status: ChangeRequestStatus
  comment?: string
}

// ---------------------------------------------------------------------------
// Aggregated DTO for the Personal Data page
// ---------------------------------------------------------------------------
export interface PersonalDataPageData {
  personalData: EmployeePersonalData
  changeRequests: DataChangeRequest[]
}
