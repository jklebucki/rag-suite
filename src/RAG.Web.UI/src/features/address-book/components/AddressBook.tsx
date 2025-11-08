// AddressBook - Main component for managing contacts
import React, { useState, useEffect } from 'react'
import { useAuth } from '@/shared/contexts/AuthContext'
import { useI18n } from '@/shared/contexts/I18nContext'
import addressBookService from '@/features/address-book/services/addressBookService'
import { ContactsTable } from './ContactsTable'
import { ContactForm } from './ContactForm'
import { ContactImport } from './ContactImport'
import { ProposalsList } from './ProposalsList'
import type {
  ContactListItem,
  CreateContactRequest,
  UpdateContactRequest,
  ProposalListItem
} from '@/features/address-book/types/addressbook'
import { ChangeProposalType, ProposalStatus } from '@/features/address-book/types/addressbook'
import { logger } from '@/utils/logger'

type TabType = 'contacts' | 'import' | 'proposals'

export function AddressBook() {
  const { user, isAuthenticated } = useAuth()
  const { t } = useI18n()
  const [activeTab, setActiveTab] = useState<TabType>('contacts')
  const [contacts, setContacts] = useState<ContactListItem[]>([])
  const [proposals, setProposals] = useState<ProposalListItem[]>([])
  const [loading, setLoading] = useState(true)
  const [proposalsLoading, setProposalsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Form/Modal states
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingContact, setEditingContact] = useState<ContactListItem | null>(null)
  const [proposingContact, setProposingContact] = useState<ContactListItem | null>(null)

  // Check if user can modify directly (Admin/PowerUser)
  const canModify = !!(isAuthenticated && (user?.roles?.includes('Admin') || user?.roles?.includes('PowerUser')))

  // Load contacts on mount
  useEffect(() => {
    loadContacts()
  }, [])

  // Load proposals when switching to proposals tab
  useEffect(() => {
    if (activeTab === 'proposals' && canModify) {
      loadProposals()
    }
  }, [activeTab, canModify])

  const loadContacts = async () => {
    setLoading(true)
    setError(null)
    try {
      const response = await addressBookService.listContacts({ includeInactive: false })
      setContacts(response.contacts)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load contacts')
    } finally {
      setLoading(false)
    }
  }

  const loadProposals = async () => {
    setProposalsLoading(true)
    try {
      const response = await addressBookService.listProposals({
        status: ProposalStatus.Pending // Only show pending proposals
      })
      setProposals(response.proposals)
    } catch (err) {
      logger.error('Failed to load proposals:', err)
    } finally {
      setProposalsLoading(false)
    }
  }

  const handleCreateContact = async (data: CreateContactRequest) => {
    if (canModify) {
      await addressBookService.createContact(data)
      await loadContacts()
    } else {
      // Propose change
      await addressBookService.proposeChange({
        proposalType: ChangeProposalType.Create,
        proposedData: data,
        reason: 'New contact proposal'
      })
      alert('Your proposal has been submitted for review')
    }
    setIsFormOpen(false)
  }

  const handleUpdateContact = async (data: UpdateContactRequest) => {
    if (!editingContact) return

    if (canModify) {
      await addressBookService.updateContact(editingContact.id, data)
      await loadContacts()
    } else {
      // Propose change
      await addressBookService.proposeChange({
        contactId: editingContact.id,
        proposalType: ChangeProposalType.Update,
        proposedData: data,
        reason: 'Contact update proposal'
      })
      alert('Your proposal has been submitted for review')
    }
    setIsFormOpen(false)
    setEditingContact(null)
  }

  const handleDeleteContact = async (contact: ContactListItem) => {
    if (!confirm(`Delete contact ${contact.firstName} ${contact.lastName}?`)) return

    try {
      if (canModify) {
        await addressBookService.deleteContact(contact.id)
        await loadContacts()
      } else {
        // Propose deletion
        await addressBookService.proposeChange({
          contactId: contact.id,
          proposalType: ChangeProposalType.Delete,
          proposedData: {
            firstName: contact.firstName,
            lastName: contact.lastName
          },
          reason: 'Contact deletion proposal'
        })
        alert('Your deletion proposal has been submitted for review')
      }
    } catch (err) {
      alert('Failed to delete contact: ' + (err instanceof Error ? err.message : 'Unknown error'))
    }
  }

  const handleProposeChange = (contact: ContactListItem) => {
    setProposingContact(contact)
    setEditingContact(contact)
    setIsFormOpen(true)
  }

  const handleImport = async (file: File, skipDuplicates: boolean, encoding: string) => {
    const result = await addressBookService.importContactsFromFile(file, skipDuplicates, encoding)
    await loadContacts()
    return result
  }

  const handleReviewProposal = async (proposalId: string, approved: boolean, comment?: string) => {
    await addressBookService.reviewProposal(proposalId, {
      decision: approved ? ProposalStatus.Approved : ProposalStatus.Rejected,
      reviewComment: comment || null
    })
    await loadProposals()
    await loadContacts() // Refresh contacts if approved
  }

  const openCreateForm = () => {
    setEditingContact(null)
    setProposingContact(null)
    setIsFormOpen(true)
  }

  const openEditForm = (contact: ContactListItem) => {
    setEditingContact(contact)
    setProposingContact(null)
    setIsFormOpen(true)
  }

  const closeForm = () => {
    setIsFormOpen(false)
    setEditingContact(null)
    setProposingContact(null)
  }

  const pendingProposalsCount = proposals.filter((p) => p.status === ProposalStatus.Pending).length

  return (
    <div className="max-w-7xl max-w-[95%]">
      <div className="mb-6">
        <h1 className="text-3xl font-bold text-gray-900">{t('addressBook.title')}</h1>
        <p className="text-gray-600 mt-1">
          {t('addressBook.subtitle')}
        </p>
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200 mb-6">
        <nav className="flex gap-8">
          <button
            onClick={() => setActiveTab('contacts')}
            className={`py-4 px-1 border-b-2 font-medium text-sm transition-colors ${
              activeTab === 'contacts'
                ? 'border-blue-500 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            {t('addressBook.tabs.contacts')} ({contacts.length})
          </button>
          {canModify && (
            <>
              <button
                onClick={() => setActiveTab('import')}
                className={`py-4 px-1 border-b-2 font-medium text-sm transition-colors ${
                  activeTab === 'import'
                    ? 'border-blue-500 text-blue-600'
                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                }`}
              >
                {t('addressBook.tabs.import')}
              </button>
              <button
                onClick={() => setActiveTab('proposals')}
                className={`py-4 px-1 border-b-2 font-medium text-sm transition-colors relative ${
                  activeTab === 'proposals'
                    ? 'border-blue-500 text-blue-600'
                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                }`}
              >
                {t('addressBook.tabs.proposals')}
                {pendingProposalsCount > 0 && (
                  <span className="ml-2 inline-flex items-center justify-center w-5 h-5 text-xs font-bold text-white bg-red-500 rounded-full">
                    {pendingProposalsCount}
                  </span>
                )}
              </button>
            </>
          )}
        </nav>
      </div>

      {/* Error Message */}
      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-6">
          {error}
        </div>
      )}

      {/* Tab Content */}
      {activeTab === 'contacts' && (
        <div>
          <div className="flex justify-between items-center mb-4">
            <div className="text-sm text-gray-600">
              {isAuthenticated
                ? canModify
                  ? t('addressBook.permissions.admin')
                  : t('addressBook.permissions.user')
                : t('addressBook.permissions.guest')}
            </div>
            {isAuthenticated && (
              <button
                onClick={openCreateForm}
                className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 font-medium"
              >
                + {t('addressBook.addContact')}
              </button>
            )}
          </div>

          <ContactsTable
            contacts={contacts}
            onEdit={canModify ? openEditForm : undefined}
            onDelete={canModify ? handleDeleteContact : undefined}
            onProposeChange={!canModify && isAuthenticated ? handleProposeChange : undefined}
            canModify={canModify}
            isAuthenticated={isAuthenticated}
            loading={loading}
          />
        </div>
      )}

      {activeTab === 'import' && canModify && (
        <div className="max-w-3xl">
          <h2 className="text-xl font-semibold text-gray-900 mb-4">{t('addressBook.import.title')}</h2>
          <ContactImport onImport={handleImport} onClose={() => setActiveTab('contacts')} />
        </div>
      )}

      {activeTab === 'proposals' && canModify && (
        <div>
          <h2 className="text-xl font-semibold text-gray-900 mb-4">
            {t('addressBook.proposals.title')} ({pendingProposalsCount})
          </h2>
          <ProposalsList
            proposals={proposals}
            onReview={handleReviewProposal}
            canReview={canModify}
            loading={proposalsLoading}
          />
        </div>
      )}

      {/* Contact Form Modal */}
      {isFormOpen && (
        <ContactForm
          contact={editingContact}
          isOpen={isFormOpen}
          onClose={closeForm}
          onSubmit={(data) => editingContact ? handleUpdateContact(data as UpdateContactRequest) : handleCreateContact(data as CreateContactRequest)}
          canModify={canModify}
          title={
            proposingContact
              ? `Propose Change for ${proposingContact.firstName} ${proposingContact.lastName}`
              : undefined
          }
        />
      )}
    </div>
  )
}
