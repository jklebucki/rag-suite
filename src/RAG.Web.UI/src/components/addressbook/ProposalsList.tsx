// ProposalsList - Display and manage contact change proposals
import React, { useState } from 'react'
import type { ProposalListItem } from '@/types/addressbook'
import { ChangeProposalType, ProposalStatus } from '@/types/addressbook'
import { useI18n } from '@/contexts/I18nContext'

interface ProposalsListProps {
  proposals: ProposalListItem[]
  onReview?: (proposalId: string, approved: boolean, comment?: string) => Promise<void>
  canReview: boolean // Admin/PowerUser can review
  loading?: boolean
}

export const ProposalsList: React.FC<ProposalsListProps> = ({
  proposals,
  onReview,
  canReview,
  loading = false
}) => {
  const { t } = useI18n()
  const [reviewingId, setReviewingId] = useState<string | null>(null)
  const [reviewComment, setReviewComment] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  const getProposalTypeLabel = (type: ChangeProposalType): string => {
    switch (type) {
      case ChangeProposalType.Create:
        return t('addressBook.proposals.type.create')
      case ChangeProposalType.Update:
        return t('addressBook.proposals.type.update')
      case ChangeProposalType.Delete:
        return t('addressBook.proposals.type.delete')
      default:
        return 'Unknown'
    }
  }

  const getProposalTypeBadge = (type: ChangeProposalType): string => {
    switch (type) {
      case ChangeProposalType.Create:
        return 'bg-green-100 text-green-800'
      case ChangeProposalType.Update:
        return 'bg-blue-100 text-blue-800'
      case ChangeProposalType.Delete:
        return 'bg-red-100 text-red-800'
      default:
        return 'bg-gray-100 text-gray-800'
    }
  }

  const getStatusLabel = (status: ProposalStatus): string => {
    switch (status) {
      case ProposalStatus.Pending:
        return t('addressBook.proposals.status.pending')
      case ProposalStatus.Approved:
        return t('addressBook.proposals.status.approved')
      case ProposalStatus.Rejected:
        return t('addressBook.proposals.status.rejected')
      case ProposalStatus.Applied:
        return t('addressBook.proposals.status.applied')
      default:
        return 'Unknown'
    }
  }

  const getStatusBadge = (status: ProposalStatus): string => {
    switch (status) {
      case ProposalStatus.Pending:
        return 'bg-yellow-100 text-yellow-800'
      case ProposalStatus.Approved:
        return 'bg-green-100 text-green-800'
      case ProposalStatus.Rejected:
        return 'bg-red-100 text-red-800'
      case ProposalStatus.Applied:
        return 'bg-blue-100 text-blue-800'
      default:
        return 'bg-gray-100 text-gray-800'
    }
  }

  const handleReview = async (proposalId: string, approved: boolean) => {
    if (!onReview) return

    setIsSubmitting(true)
    try {
      await onReview(proposalId, approved, reviewComment || undefined)
      setReviewingId(null)
      setReviewComment('')
    } catch (err) {
      console.error('Review failed:', err)
      alert(t('addressBook.proposals.failedToReview') + ': ' + (err instanceof Error ? err.message : 'Unknown error'))
    } finally {
      setIsSubmitting(false)
    }
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="text-gray-500">{t('addressBook.proposals.loading')}</div>
      </div>
    )
  }

  if (proposals.length === 0) {
    return (
      <div className="bg-gray-50 border border-gray-200 rounded-lg p-8 text-center">
        <p className="text-gray-600">{t('addressBook.proposals.noProposalsFound')}</p>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      {proposals.map((proposal) => (
        <div
          key={proposal.id}
          className="bg-white border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow"
        >
          <div className="flex items-start justify-between mb-3">
            <div className="flex-1">
              <div className="flex items-center gap-2 mb-1">
                <span
                  className={`px-2 py-1 rounded-full text-xs font-medium ${getProposalTypeBadge(
                    proposal.proposalType
                  )}`}
                >
                  {getProposalTypeLabel(proposal.proposalType)}
                </span>
                <span
                  className={`px-2 py-1 rounded-full text-xs font-medium ${getStatusBadge(
                    proposal.status
                  )}`}
                >
                  {getStatusLabel(proposal.status)}
                </span>
              </div>
              <h3 className="font-medium text-gray-900">
                {proposal.contactName || t('addressBook.proposals.newContact')}
              </h3>
              <p className="text-sm text-gray-600 mt-1">
                {t('addressBook.proposals.proposedBy')} <span className="font-medium">{proposal.proposedByUserName || proposal.proposedByUserId}</span>
                {' • '}
                {new Date(proposal.proposedAt).toLocaleString()}
              </p>
              {proposal.reason && (
                <p className="text-sm text-gray-700 mt-2 italic">
                  {t('addressBook.proposals.reason')}: {proposal.reason}
                </p>
              )}
              {proposal.reviewedByUserName && proposal.reviewedAt && (
                <p className="text-sm text-gray-600 mt-2">
                  {t('addressBook.proposals.reviewedBy')} <span className="font-medium">{proposal.reviewedByUserName}</span>
                  {' • '}
                  {new Date(proposal.reviewedAt).toLocaleString()}
                </p>
              )}
            </div>
          </div>

          {/* Review Actions (only for Pending proposals if user can review) */}
          {canReview && proposal.status === ProposalStatus.Pending && (
            <div className="mt-3 pt-3 border-t border-gray-200">
              {reviewingId === proposal.id ? (
                <div className="space-y-3">
                  <div>
                    <label htmlFor="review-comment" className="block text-sm font-medium text-gray-700 mb-1">
                      {t('addressBook.proposals.comment')}
                    </label>
                    <textarea
                      id="review-comment"
                      value={reviewComment}
                      onChange={(e) => setReviewComment(e.target.value)}
                      rows={2}
                      placeholder={t('addressBook.proposals.commentPlaceholder')}
                      className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent text-sm"
                    />
                  </div>
                  <div className="flex gap-2">
                    <button
                      onClick={() => handleReview(proposal.id, true)}
                      disabled={isSubmitting}
                      className="px-4 py-2 text-white bg-green-600 rounded-lg hover:bg-green-700 disabled:opacity-50 text-sm font-medium"
                    >
                      {isSubmitting ? t('addressBook.proposals.processing') : t('addressBook.proposals.approveAndApply')}
                    </button>
                    <button
                      onClick={() => handleReview(proposal.id, false)}
                      disabled={isSubmitting}
                      className="px-4 py-2 text-white bg-red-600 rounded-lg hover:bg-red-700 disabled:opacity-50 text-sm font-medium"
                    >
                      {isSubmitting ? t('addressBook.proposals.processing') : t('addressBook.proposals.reject')}
                    </button>
                    <button
                      onClick={() => {
                        setReviewingId(null)
                        setReviewComment('')
                      }}
                      disabled={isSubmitting}
                      className="px-4 py-2 text-gray-700 bg-gray-100 rounded-lg hover:bg-gray-200 disabled:opacity-50 text-sm"
                    >
                      {t('addressBook.proposals.cancel')}
                    </button>
                  </div>
                </div>
              ) : (
                <button
                  onClick={() => setReviewingId(proposal.id)}
                  className="text-blue-600 hover:text-blue-800 font-medium text-sm"
                >
                  {t('addressBook.proposals.reviewProposal')}
                </button>
              )}
            </div>
          )}
        </div>
      ))}
    </div>
  )
}
