// AddressBook API Service
// Handles all API calls to RAG.AddressBook backend
// Delegates to centralized ApiClient in api.ts

import apiClient from '@/shared/services/api'
import { logger } from '@/utils/logger'
import type {
  ListContactsRequest,
  ListContactsResponse,
  CreateContactRequest,
  CreateContactResponse,
  UpdateContactRequest,
  UpdateContactResponse,
  SearchContactsRequest,
  SearchContactsResponse,
  Contact,
  ProposeChangeRequest,
  ProposeChangeResponse,
  ListProposalsRequest,
  ListProposalsResponse,
  ContactChangeProposal,
  ReviewProposalRequest,
  ReviewProposalResponse,
  ImportContactsRequest,
  ImportContactsResponse
} from '@/features/address-book/types/addressbook'

/**
 * AddressBook Service
 * Provides methods for managing contacts, proposals, and imports
 * All methods delegate to centralized ApiClient
 */
class AddressBookService {
  /**
   * List contacts with pagination and filtering
   */
  async listContacts(request?: ListContactsRequest): Promise<ListContactsResponse> {
    return apiClient.listContacts(request)
  }

  /**
   * Get a specific contact by ID
   */
  async getContact(contactId: string): Promise<Contact> {
    return apiClient.getContact(contactId)
  }

  /**
   * Search contacts by query
   */
  async searchContacts(request?: SearchContactsRequest): Promise<SearchContactsResponse> {
    return apiClient.searchContacts(request?.query, request?.includeInactive)
  }

  /**
   * Create a new contact
   */
  async createContact(request: CreateContactRequest): Promise<CreateContactResponse> {
    return apiClient.createContact(request)
  }

  /**
   * Update an existing contact
   */
  async updateContact(contactId: string, request: UpdateContactRequest): Promise<UpdateContactResponse> {
    return apiClient.updateContact(contactId, request)
  }

  /**
   * Delete a contact
   */
  async deleteContact(contactId: string): Promise<void> {
    return apiClient.deleteContact(contactId)
  }

  /**
   * Propose a change to a contact (for non-admin users)
   */
  async proposeChange(request: ProposeChangeRequest): Promise<ProposeChangeResponse> {
    return apiClient.proposeChange(request)
  }

  /**
   * List all proposals with filtering
   */
  async listProposals(request?: ListProposalsRequest): Promise<ListProposalsResponse> {
    return apiClient.listProposals(request)
  }

  /**
   * Get a specific proposal by ID
   */
  async getProposal(proposalId: string): Promise<ContactChangeProposal> {
    return apiClient.getProposal(proposalId)
  }

  /**
   * Review (approve/reject) a proposal
   */
  async reviewProposal(proposalId: string, request: ReviewProposalRequest): Promise<ReviewProposalResponse> {
    return apiClient.reviewProposal(proposalId, request)
  }

  /**
   * Import contacts from structured data
   */
  async importContacts(request: ImportContactsRequest): Promise<ImportContactsResponse> {
    return apiClient.importContacts(request)
  }

  /**
   * Import contacts from CSV file
   */
  async importContactsFromFile(file: File, skipDuplicates: boolean = true, encoding: string = 'UTF-8'): Promise<ImportContactsResponse> {
    try {
      // Read file with specified encoding
      const arrayBuffer = await file.arrayBuffer()
      const decoder = new TextDecoder(encoding)
      const csvContent = decoder.decode(arrayBuffer)
      
      // Send as JSON via apiClient
      return await this.importContacts({
        csvContent,
        skipDuplicates
      })
    } catch (error) {
      logger.error('File import failed:', error)
      throw error
    }
  }
}

export const addressBookService = new AddressBookService()
export default addressBookService
