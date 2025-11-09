// AddressBook - Main component for managing contacts
import React, { useState, useEffect } from 'react'
import { useAuth } from '@/shared/contexts/AuthContext'
import { useI18n } from '@/shared/contexts/I18nContext'
import addressBookService from '@/features/address-book/services/addressBook.service'
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
import { ActionModal, ActionModalVariant } from '@/shared/components/ui/ActionModal'
import { DeleteConfirmationModal } from '@/shared/components/common/DeleteConfirmationModal'

type TabType = 'contacts' | 'import' | 'proposals'

interface AlertState {
  title: React.ReactNode
  message: React.ReactNode
  variant?: ActionModalVariant
}

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
  const [alertState, setAlertState] = useState<AlertState | null>(null)
  const [contactToDelete, setContactToDelete] = useState<ContactListItem | null>(null)
  const [isDeleteProcessing, setIsDeleteProcessing] = useState(false)

  const showAlert = (state: AlertState) => setAlertState(state)
  const closeAlert = () => setAlertState(null)

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
      showAlert({
        title: t('common.success'),
        message: t('addressBook.messages.proposalSubmitted'),
        variant: 'success'
      })
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
      showAlert({
        title: t('common.success'),
        message: t('addressBook.messages.proposalSubmitted'),
        variant: 'success'
      })
    }
    setIsFormOpen(false)
    setEditingContact(null)
  }

  const handleDeleteContact = (contact: ContactListItem) => {
    setContactToDelete(contact)
  }

  const confirmDeleteContact = async () => {
    if (!contactToDelete) return

    setIsDeleteProcessing(true)
    try {
      if (canModify) {
        await addressBookService.deleteContact(contactToDelete.id)
        await loadContacts()
        setContactToDelete(null)
      } else {
        // Propose deletion
        await addressBookService.proposeChange({
          contactId: contactToDelete.id,
          proposalType: ChangeProposalType.Delete,
          proposedData: {
            firstName: contactToDelete.firstName,
            lastName: contactToDelete.lastName
          },
          reason: 'Contact deletion proposal'
        })
        setContactToDelete(null)
        showAlert({
          title: t('common.success'),
          message: t('addressBook.messages.deletionProposalSubmitted'),
          variant: 'success'
        })
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : t('addressBook.messages.unknownError')
      showAlert({
        title: t('common.error'),
        message: `${t('addressBook.messages.failedToDelete')}: ${errorMessage}`,
        variant: 'error'
      })
    } finally {
      setIsDeleteProcessing(false)
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
  const deleteModalDetails =
    contactToDelete
      ? [
          contactToDelete.email
            ? { label: t('addressBook.table.email'), value: contactToDelete.email }
            : null,
          contactToDelete.mobilePhone
            ? { label: t('addressBook.table.mobilePhone'), value: contactToDelete.mobilePhone }
            : null,
          contactToDelete.location
            ? { label: t('addressBook.table.location'), value: contactToDelete.location }
            : null
        ].filter((detail): detail is { label: string; value: string } => Boolean(detail))
      : []

  return (
    <div className="max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8">
      <div className="mb-6">
        <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100">{t('addressBook.title')}</h1>
        <p className="text-gray-600 dark:text-gray-300 mt-1">
          {t('addressBook.subtitle')}
        </p>
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200 dark:border-slate-700 mb-6">
        <nav className="flex gap-6 whitespace-nowrap overflow-x-auto scrollbar-hide">
          <button
            onClick={() => setActiveTab('contacts')}
            className={`py-3 px-1 border-b-2 font-medium text-sm transition-colors ${
              activeTab === 'contacts'
                ? 'border-primary-500 text-primary-600 dark:text-primary-300'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300 dark:text-gray-400 dark:hover:text-gray-200 dark:hover:border-slate-600'
            }`}
          >
            {t('addressBook.tabs.contacts')} ({contacts.length})
          </button>
          {canModify && (
            <>
              <button
                onClick={() => setActiveTab('import')}
                className={`py-3 px-1 border-b-2 font-medium text-sm transition-colors ${
                  activeTab === 'import'
                    ? 'border-primary-500 text-primary-600 dark:text-primary-300'
                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300 dark:text-gray-400 dark:hover:text-gray-200 dark:hover:border-slate-600'
                }`}
              >
                {t('addressBook.tabs.import')}
              </button>
              <button
                onClick={() => setActiveTab('proposals')}
                className={`py-3 px-1 border-b-2 font-medium text-sm transition-colors relative ${
                  activeTab === 'proposals'
                    ? 'border-primary-500 text-primary-600 dark:text-primary-300'
                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300 dark:text-gray-400 dark:hover:text-gray-200 dark:hover:border-slate-600'
                }`}
              >
                {t('addressBook.tabs.proposals')}
                {pendingProposalsCount > 0 && (
                  <span className="ml-2 inline-flex items-center justify-center w-5 h-5 text-xs font-semibold text-white bg-red-500 dark:bg-red-600 rounded-full">
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
        <div className="surface-muted border border-red-200 dark:border-red-700 text-red-700 dark:text-red-300 px-4 py-3 rounded-xl mb-6">
          {error}
        </div>
      )}

      {/* Tab Content */}
      {activeTab === 'contacts' && (
        <div className="space-y-4">
          <div className="flex flex-wrap justify-between items-center gap-3">
            <div className="text-sm text-gray-600 dark:text-gray-300">
              {isAuthenticated
                ? canModify
                  ? t('addressBook.permissions.admin')
                  : t('addressBook.permissions.user')
                : t('addressBook.permissions.guest')}
            </div>
            {isAuthenticated && (
              <button
                onClick={openCreateForm}
                className="btn-primary"
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
        <div className="max-w-3xl space-y-4">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">{t('addressBook.import.title')}</h2>
          <ContactImport onImport={handleImport} onClose={() => setActiveTab('contacts')} />
        </div>
      )}

      {activeTab === 'proposals' && canModify && (
        <div className="space-y-4">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">
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

      <DeleteConfirmationModal
        isOpen={!!contactToDelete}
        onClose={() => {
          if (isDeleteProcessing) return
          setContactToDelete(null)
        }}
        onConfirm={confirmDeleteContact}
        isLoading={isDeleteProcessing}
        title={t('addressBook.messages.deleteConfirmTitle')}
        message={t('addressBook.deleteConfirm')}
        itemName={
          contactToDelete
            ? `${contactToDelete.firstName} ${contactToDelete.lastName}`.trim()
            : ''
        }
        details={deleteModalDetails.length ? deleteModalDetails : undefined}
        confirmText={t('common.delete')}
        cancelText={t('common.cancel')}
      />

      {alertState && (
        <ActionModal
          isOpen
          onClose={closeAlert}
          title={alertState.title}
          message={alertState.message}
          confirmText={t('common.close')}
          cancelText={undefined}
          hideCancel
          variant={alertState.variant}
          size="sm"
        />
      )}
    </div>
  )
}
