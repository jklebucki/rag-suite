import React, { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Loader2, Inbox, Eye, Trash2 } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import feedbackService from '@/features/feedback/services/feedback.service'
import type { FeedbackFilters, FeedbackItem } from '@/features/feedback/types/feedback'
import { useToast } from '@/shared/hooks/useToast'
import { FeedbackDetailsModal } from '@/features/feedback/components/FeedbackDetailsModal'
import { Modal } from '@/shared/components/ui/Modal'
interface FilterState {
  subject: string
  userId: string
  fromDate: string
  toDate: string
}

const initialFilters: FilterState = {
  subject: '',
  userId: '',
  fromDate: '',
  toDate: ''
}

export function FeedbackAdminPanel() {
  const { t } = useI18n()
  const { showSuccess, showError } = useToast()
  const queryClient = useQueryClient()

  const [filters, setFilters] = useState<FilterState>(initialFilters)
  const [selectedFeedback, setSelectedFeedback] = useState<FeedbackItem | null>(null)
  const [isDetailsOpen, setIsDetailsOpen] = useState(false)
  const [feedbackToDelete, setFeedbackToDelete] = useState<FeedbackItem | null>(null)
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false)

  const queryFilters = useMemo(() => {
    const result: FeedbackFilters & { userEmail?: string } = {}
    if (filters.subject.trim()) result.subject = filters.subject.trim()
    if (filters.userId.trim()) result.userEmail = filters.userId.trim()
    if (filters.fromDate) result.from = new Date(`${filters.fromDate}T00:00:00Z`).toISOString()
    if (filters.toDate) result.to = new Date(`${filters.toDate}T23:59:59Z`).toISOString()
    return result
  }, [filters])

  const feedbackQuery = useQuery<FeedbackItem[]>({
    queryKey: ['feedback', queryFilters],
    queryFn: () => feedbackService.getFeedbackList(queryFilters)
  })
  const feedbackList = feedbackQuery.data ?? []
  const { isLoading, isFetching } = feedbackQuery

  const respondMutation = useMutation({
    mutationFn: ({ id, response }: { id: string; response: string }) =>
      feedbackService.respondToFeedback(id, { response }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['feedback'] })
      showSuccess(t('feedback.admin.details.success'))
      setIsDetailsOpen(false)
    },
    onError: () => {
      showError(t('feedback.admin.details.error'))
    }
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => feedbackService.deleteFeedback(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['feedback'] })
      showSuccess(t('feedback.admin.delete.success'))
      setIsDeleteModalOpen(false)
      setFeedbackToDelete(null)
    },
    onError: () => {
      showError(t('feedback.admin.delete.error'))
    }
  })

  const handleOpenDetails = (feedback: FeedbackItem) => {
    setSelectedFeedback(feedback)
    setIsDetailsOpen(true)
  }

  const handleCloseDetails = () => {
    setIsDetailsOpen(false)
  }

  const handleRespond = async (id: string, response: string) => {
    await respondMutation.mutateAsync({ id, response })
  }

  const handleDeleteClick = (feedback: FeedbackItem) => {
    setFeedbackToDelete(feedback)
    setIsDeleteModalOpen(true)
  }

  const handleConfirmDelete = () => {
    if (!feedbackToDelete || deleteMutation.isPending) return
    deleteMutation.mutate(feedbackToDelete.id)
  }

  const handleCloseDeleteModal = () => {
    if (deleteMutation.isPending) return
    setIsDeleteModalOpen(false)
    setFeedbackToDelete(null)
  }

  const handleFiltersReset = () => {
    setFilters(initialFilters)
  }

  const renderStatus = (feedback: FeedbackItem) => {
    if (feedback.response && feedback.respondedAt) {
      return (
        <span className="inline-flex items-center gap-2 px-2 py-1 rounded-full bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-300 text-xs font-medium">
          {t('feedback.admin.table.responded')}
        </span>
      )
    }

    return (
      <span className="inline-flex items-center gap-2 px-2 py-1 rounded-full bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300 text-xs font-medium">
        {t('feedback.admin.table.pending')}
      </span>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">
            {t('settings.feedback.title')}
          </h1>
          <p className="text-gray-600 dark:text-gray-300">
            {t('settings.feedback.subtitle')}
          </p>
        </div>
      </div>

      <div className="surface p-6 space-y-6">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <div>
            <label htmlFor="feedback-filter-subject" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">
              {t('feedback.admin.filters.subject')}
            </label>
            <input
              id="feedback-filter-subject"
              type="text"
              value={filters.subject}
              onChange={(e) => setFilters(prev => ({ ...prev, subject: e.target.value }))}
              className="form-input w-full"
              placeholder={t('feedback.admin.filters.subject_placeholder')}
            />
          </div>
          <div>
            <label htmlFor="feedback-filter-user" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">
              {t('feedback.admin.filters.user')}
            </label>
            <input
              id="feedback-filter-user"
              type="text"
              value={filters.userId}
              onChange={(e) => setFilters(prev => ({ ...prev, userId: e.target.value }))}
              className="form-input w-full"
              placeholder={t('feedback.admin.filters.user_placeholder')}
            />
          </div>
          <div>
            <label htmlFor="feedback-filter-date-from" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">
              {t('feedback.admin.filters.date_from')}
            </label>
            <input
              id="feedback-filter-date-from"
              type="date"
              value={filters.fromDate}
              onChange={(e) => setFilters(prev => ({ ...prev, fromDate: e.target.value }))}
              className="form-input w-full"
            />
          </div>
          <div>
            <label htmlFor="feedback-filter-date-to" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">
              {t('feedback.admin.filters.date_to')}
            </label>
            <input
              id="feedback-filter-date-to"
              type="date"
              value={filters.toDate}
              onChange={(e) => setFilters(prev => ({ ...prev, toDate: e.target.value }))}
              className="form-input w-full"
            />
          </div>
        </div>

        <div className="flex items-center justify-end gap-3">
          <button
            onClick={handleFiltersReset}
            className="btn-secondary text-sm"
          >
            {t('feedback.admin.filters.clear')}
          </button>
        </div>

        <div className="relative">
          {(isLoading || isFetching) && (
            <div className="absolute inset-0 bg-white/70 dark:bg-gray-900/70 backdrop-blur-sm flex items-center justify-center z-10">
              <div className="flex items-center gap-2 text-primary-600 dark:text-primary-300">
                <Loader2 className="h-5 w-5 animate-spin" />
                <span>{t('feedback.admin.loading')}</span>
              </div>
            </div>
          )}

          {feedbackList.length === 0 ? (
            <div className="border border-dashed border-gray-300 dark:border-gray-600 rounded-xl p-8 text-center">
              <Inbox className="h-12 w-12 text-gray-400 dark:text-gray-500 mx-auto mb-4" />
              <h3 className="text-lg font-semibold text-gray-800 dark:text-gray-200">
                {t('feedback.admin.empty.title')}
              </h3>
              <p className="text-sm text-gray-600 dark:text-gray-400">
                {t('feedback.admin.empty.description')}
              </p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                <thead className="bg-gray-50 dark:bg-gray-800/80">
                  <tr>
                    <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                      {t('feedback.admin.table.subject')}
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                      {t('feedback.admin.table.user')}
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                      {t('feedback.admin.table.created_at')}
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                      {t('feedback.admin.table.response_status')}
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                      {t('feedback.admin.table.actions')}
                    </th>
                  </tr>
                </thead>
                <tbody className="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
                  {feedbackList.map((feedback) => {
                    const primaryUserValue = feedback.userEmail?.trim() ?? ''

                    return (
                      <tr key={feedback.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/80 transition-colors">
                        <td className="px-4 py-3 text-sm text-gray-900 dark:text-gray-100">
                          <div className="font-medium line-clamp-1">{feedback.subject}</div>
                          <div className="text-xs text-gray-500 dark:text-gray-400 line-clamp-1">
                            {feedback.message}
                          </div>
                        </td>
                        <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">
                          <div className="font-medium text-gray-900 dark:text-gray-100 break-all">
                            {primaryUserValue || 'N/A'}
                          </div>
                        </td>
                        <td className="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">
                          {new Date(feedback.createdAt).toLocaleString()}
                        </td>
                        <td className="px-4 py-3 text-sm">
                          {renderStatus(feedback)}
                        </td>
                        <td className="px-4 py-3 text-sm">
                          <div className="flex items-center gap-2">
                            <button
                              onClick={() => handleOpenDetails(feedback)}
                              className="inline-flex items-center justify-center rounded-md border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 p-2 text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
                              aria-label={t('feedback.admin.table.view')}
                              title={t('feedback.admin.table.view')}
                            >
                              <Eye className="h-4 w-4" />
                            </button>
                            <button
                              onClick={() => handleDeleteClick(feedback)}
                              className="inline-flex items-center justify-center rounded-md border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 p-2 text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/30 transition-colors"
                              aria-label={t('feedback.admin.table.delete')}
                              title={t('feedback.admin.table.delete')}
                              disabled={deleteMutation.isPending && feedbackToDelete?.id === feedback.id}
                            >
                              <Trash2 className="h-4 w-4" />
                            </button>
                          </div>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      <FeedbackDetailsModal
        isOpen={isDetailsOpen}
        onClose={handleCloseDetails}
        feedback={selectedFeedback}
        onRespond={handleRespond}
        isResponding={respondMutation.isPending}
      />

      <Modal
        isOpen={isDeleteModalOpen}
        onClose={handleCloseDeleteModal}
        title={
          <div className="flex flex-col gap-1">
            <h3 className="text-lg sm:text-xl font-semibold text-gray-900 dark:text-gray-100">
              {t('feedback.admin.delete.title')}
            </h3>
            <p className="text-sm text-gray-600 dark:text-gray-300">
              {t('feedback.admin.delete.description', { subject: feedbackToDelete?.subject ?? '' })}
            </p>
          </div>
        }
        size="sm"
      >
        <div className="p-6 space-y-4">
          <p className="text-sm text-gray-700 dark:text-gray-200">
            {t('feedback.admin.delete.confirm')}
          </p>
          <div className="flex items-center justify-end gap-3">
            <button
              type="button"
              onClick={handleCloseDeleteModal}
              className="btn-secondary"
              disabled={deleteMutation.isPending}
            >
              {t('common.cancel')}
            </button>
            <button
              type="button"
              onClick={handleConfirmDelete}
              className="btn-primary bg-red-600 hover:bg-red-700 dark:bg-red-500 dark:hover:bg-red-600"
              disabled={deleteMutation.isPending}
            >
              {deleteMutation.isPending ? t('common.processing') : t('feedback.admin.delete.confirm_button')}
            </button>
          </div>
        </div>
      </Modal>
    </div>
  )
}

