import { apiHttpClient } from '@/shared/services/api/httpClients'
import { logger } from '@/utils/logger'
import type { ApiResponse } from '@/shared/types/api'
import type {
  Contact,
  ContactChangeProposal,
  CreateContactRequest,
  CreateContactResponse,
  ImportContactsRequest,
  ImportContactsResponse,
  ListContactsRequest,
  ListContactsResponse,
  ListProposalsRequest,
  ListProposalsResponse,
  ProposeChangeRequest,
  ProposeChangeResponse,
  ReviewProposalRequest,
  ReviewProposalResponse,
  SearchContactsRequest,
  SearchContactsResponse,
  UpdateContactRequest,
  UpdateContactResponse,
} from '@/features/address-book/types/addressbook'

class AddressBookService {
  async listContacts(request?: ListContactsRequest): Promise<ListContactsResponse> {
    const params = new URLSearchParams()
    params.append('includeInactive', request?.includeInactive ? 'true' : 'false')
    if (request?.department) params.append('department', request.department)
    if (request?.location) params.append('location', request.location)

    const url = params.toString() ? `/addressbook?${params}` : '/addressbook'
    const response = await apiHttpClient.get<ListContactsResponse>(url)
    return response.data
  }

  async getContact(contactId: string): Promise<Contact> {
    const response = await apiHttpClient.get<ApiResponse<Contact>>(`/addressbook/${contactId}`)
    return response.data.data
  }

  async searchContacts(request?: SearchContactsRequest): Promise<SearchContactsResponse> {
    const params = new URLSearchParams()
    if (request?.searchTerm) params.append('searchTerm', request.searchTerm)

    const url = params.toString() ? `/addressbook/search?${params}` : '/addressbook/search'
    const response = await apiHttpClient.get<SearchContactsResponse>(url)
    return response.data
  }

  async createContact(request: CreateContactRequest): Promise<CreateContactResponse> {
    const response = await apiHttpClient.post<CreateContactResponse>('/addressbook', request)
    return response.data
  }

  async updateContact(contactId: string, request: UpdateContactRequest): Promise<UpdateContactResponse> {
    const response = await apiHttpClient.put<UpdateContactResponse>(`/addressbook/${contactId}`, request)
    return response.data
  }

  async deleteContact(contactId: string): Promise<void> {
    await apiHttpClient.delete(`/addressbook/${contactId}`)
  }

  async proposeChange(request: ProposeChangeRequest): Promise<ProposeChangeResponse> {
    const response = await apiHttpClient.post<ProposeChangeResponse>('/addressbook/proposals', request)
    return response.data
  }

  async listProposals(request?: ListProposalsRequest): Promise<ListProposalsResponse> {
    const params = new URLSearchParams()
    if (request?.status !== undefined && request.status !== null) {
      params.append('status', request.status.toString())
    }
    if (request?.proposalType !== undefined && request.proposalType !== null) {
      params.append('proposalType', request.proposalType.toString())
    }
    if (request?.proposedByUserId) {
      params.append('proposedByUserId', request.proposedByUserId)
    }

    const url = params.toString() ? `/addressbook/proposals?${params}` : '/addressbook/proposals'
    const response = await apiHttpClient.get<ListProposalsResponse>(url)
    return response.data
  }

  async getProposal(proposalId: string): Promise<ContactChangeProposal> {
    const response = await apiHttpClient.get<ContactChangeProposal>(`/addressbook/proposals/${proposalId}`)
    return response.data
  }

  async reviewProposal(proposalId: string, request: ReviewProposalRequest): Promise<ReviewProposalResponse> {
    const response = await apiHttpClient.post<ReviewProposalResponse>(`/addressbook/proposals/${proposalId}/review`, request)
    return response.data
  }

  async importContacts(request: ImportContactsRequest): Promise<ImportContactsResponse> {
    const response = await apiHttpClient.post<ImportContactsResponse>('/addressbook/import', request)
    return response.data
  }

  async importContactsFromFile(
    file: File,
    skipDuplicates: boolean = true,
    encoding: string = 'UTF-8'
  ): Promise<ImportContactsResponse> {
    try {
      const arrayBuffer = await file.arrayBuffer()
      const decoder = new TextDecoder(encoding)
      const csvContent = decoder.decode(arrayBuffer)

      return await this.importContacts({
        csvContent,
        skipDuplicates,
      })
    } catch (error) {
      logger.error('File import failed:', error)
      throw error
    }
  }
}

export const addressBookService = new AddressBookService()
export default addressBookService
