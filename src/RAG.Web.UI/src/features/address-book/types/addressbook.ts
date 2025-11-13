// Address Book Types
// Matches DTOs from RAG.AddressBook backend API

// ============================================================================
// Domain Enums
// ============================================================================

export enum ChangeProposalType {
  Create = 1,
  Update = 2,
  Delete = 3
}

export enum ProposalStatus {
  Pending = 1,
  Approved = 2,
  Rejected = 3,
  Applied = 4
}

// ============================================================================
// Contact Models
// ============================================================================

export interface Contact {
  id: string
  firstName: string
  lastName: string
  displayName?: string | null
  department?: string | null
  position?: string | null
  location?: string | null
  company?: string | null
  workPhone?: string | null
  mobilePhone?: string | null
  email?: string | null
  notes?: string | null
  photoUrl?: string | null
  isActive: boolean
  tags?: string[]
  createdAt: string
  updatedAt?: string | null
  createdByUserId?: string | null
  updatedByUserId?: string | null
}

export interface ContactListItem {
  id: string
  firstName: string
  lastName: string
  displayName?: string | null
  department?: string | null
  position?: string | null
  location?: string | null
  email?: string | null
  mobilePhone?: string | null
  isActive: boolean
}

export interface ContactData {
  firstName: string
  lastName: string
  displayName?: string | null
  department?: string | null
  position?: string | null
  location?: string | null
  company?: string | null
  workPhone?: string | null
  mobilePhone?: string | null
  email?: string | null
  notes?: string | null
  photoUrl?: string | null
}

// ============================================================================
// Contact Request/Response Models
// ============================================================================

export interface ListContactsRequest {
  includeInactive?: boolean
  department?: string | null
  location?: string | null
}

export interface ListContactsResponse {
  contacts: ContactListItem[]
  totalCount: number
}

export interface CreateContactRequest {
  firstName: string
  lastName: string
  displayName?: string | null
  department?: string | null
  position?: string | null
  location?: string | null
  company?: string | null
  workPhone?: string | null
  mobilePhone?: string | null
  email?: string | null
  notes?: string | null
  photoUrl?: string | null
  tags?: string[]
}

export interface CreateContactResponse {
  id: string
  firstName: string
  lastName: string
  email?: string | null
  createdAt: string
}

export interface UpdateContactRequest {
  firstName: string
  lastName: string
  displayName?: string | null
  department?: string | null
  position?: string | null
  location?: string | null
  company?: string | null
  workPhone?: string | null
  mobilePhone?: string | null
  email?: string | null
  notes?: string | null
  photoUrl?: string | null
  isActive: boolean
  tags?: string[]
}

export interface ContactTagDto {
  id: string
  tagName: string
  color?: string | null
}

export interface UpdateContactResponse {
  id: string
  firstName: string
  lastName: string
  email?: string | null
  updatedAt: string
  tags: ContactTagDto[]
}

export interface SearchContactsRequest {
  query: string
  includeInactive?: boolean
  limit?: number
}

export interface SearchContactsResponse {
  contacts: ContactListItem[]
  totalCount: number
}

// ============================================================================
// Proposal Models
// ============================================================================

export interface ContactChangeProposal {
  id: string
  contactId?: string | null
  contactName?: string | null
  proposalType: ChangeProposalType
  proposedData: string // JSON string
  reason?: string | null
  status: ProposalStatus
  proposedByUserId: string
  proposedByUserName?: string | null
  proposedAt: string
  reviewedByUserId?: string | null
  reviewedByUserName?: string | null
  reviewedAt?: string | null
  reviewComment?: string | null
}

export interface ProposeChangeRequest {
  contactId?: string | null
  proposalType: ChangeProposalType
  proposedData: ContactData
  reason?: string | null
}

export interface ProposeChangeResponse {
  proposalId: string
  proposalType: ChangeProposalType
  status: ProposalStatus
  proposedAt: string
  message: string
}

export interface ListProposalsRequest {
  status?: ProposalStatus | null
  proposalType?: ChangeProposalType | null
  proposedByUserId?: string | null
}

export interface ListProposalsResponse {
  proposals: ProposalListItem[]
  totalCount: number
}

export interface ProposalListItem {
  id: string
  contactId?: string | null
  contactName?: string | null
  proposalType: ChangeProposalType
  status: ProposalStatus
  proposedByUserId: string
  proposedByUserName?: string | null
  proposedAt: string
  reviewedByUserName?: string | null
  reviewedAt?: string | null
  reason?: string | null
}

export interface ReviewProposalRequest {
  decision: ProposalStatus // Approved or Rejected
  reviewComment?: string | null
}

export interface ReviewProposalResponse {
  proposalId: string
  status: ProposalStatus
  reviewedAt: string
  message: string
}

// ============================================================================
// Import Models
// ============================================================================

export interface ImportContactsRequest {
  csvContent: string
  skipDuplicates?: boolean
}

export interface ImportContactsResponse {
  totalRows: number
  successCount: number
  skippedCount: number
  errorCount: number
  errors: string[]
  importedContacts: ImportedContact[]
}

export interface ImportedContact {
  id: string
  firstName: string
  lastName: string
  email?: string | null
  department?: string | null
}

// ============================================================================
// Helper Types
// ============================================================================

export interface ContactFilters {
  searchQuery: string
  department?: string | null
  location?: string | null
  includeInactive: boolean
}
