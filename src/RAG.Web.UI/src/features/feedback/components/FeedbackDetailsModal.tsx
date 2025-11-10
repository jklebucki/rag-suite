import React, { useEffect, useMemo, useState } from 'react'
import { Modal } from '@/shared/components/ui/Modal'
import type { FeedbackItem } from '@/features/feedback/types/feedback'
import { useI18n } from '@/shared/contexts/I18nContext'

interface FeedbackDetailsModalProps {
  isOpen: boolean
  onClose: () => void
  feedback: FeedbackItem | null
  onRespond: (id: string, response: string) => Promise<void>
  isResponding: boolean
}

export function FeedbackDetailsModal({
  isOpen,
  onClose,
  feedback,
  onRespond,
  isResponding
}: FeedbackDetailsModalProps) {
  const { t } = useI18n()
  const [responseText, setResponseText] = useState('')

  useEffect(() => {
    if (feedback) {
      setResponseText(feedback.response ?? '')
    }
  }, [feedback])

  const formattedCreatedAt = useMemo(() => {
    if (!feedback) return ''
    return new Date(feedback.createdAt).toLocaleString()
  }, [feedback])

  const formattedRespondedAt = useMemo(() => {
    if (!feedback?.respondedAt) return ''
    return new Date(feedback.respondedAt).toLocaleString()
  }, [feedback])

  if (!feedback) {
    return null
  }

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()
    await onRespond(feedback.id, responseText)
  }

  const trimmedResponse = responseText.trim()
  const trimmedOriginal = (feedback.response ?? '').trim()
  const isSaveDisabled = trimmedResponse.length === 0 || trimmedResponse === trimmedOriginal || isResponding
  const userEmail = feedback.userEmail?.trim()

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={t('feedback.admin.details.title', { subject: feedback.subject })}
      size="lg"
    >
      <form onSubmit={handleSubmit} className="p-6 space-y-6">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <p className="text-sm font-medium text-gray-500 dark:text-gray-400">
              {t('feedback.admin.details.user')}
            </p>
            <p className="text-base text-gray-900 dark:text-gray-100 break-all">
              {userEmail ?? 'N/A'}
            </p>
          </div>
          <div>
            <p className="text-sm font-medium text-gray-500 dark:text-gray-400">
              {t('feedback.admin.details.created_at')}
            </p>
            <p className="text-base text-gray-900 dark:text-gray-100">
              {formattedCreatedAt}
            </p>
          </div>
          {feedback.respondedAt && (
            <div>
              <p className="text-sm font-medium text-gray-500 dark:text-gray-400">
                {t('feedback.admin.details.responded_at')}
              </p>
              <p className="text-base text-gray-900 dark:text-gray-100">
                {formattedRespondedAt}
              </p>
            </div>
          )}
          {feedback.responseAuthorEmail && (
            <div>
              <p className="text-sm font-medium text-gray-500 dark:text-gray-400">
                {t('feedback.admin.details.response_author')}
              </p>
              <p className="text-base text-gray-900 dark:text-gray-100 break-all">
                {feedback.responseAuthorEmail}
              </p>
            </div>
          )}
        </div>

        <div>
          <p className="text-sm font-medium text-gray-500 dark:text-gray-400 mb-2">
            {t('feedback.admin.details.message_label')}
          </p>
          <div className="p-4 bg-gray-50 dark:bg-gray-800/80 rounded-lg border border-gray-200 dark:border-gray-700 text-gray-800 dark:text-gray-200 whitespace-pre-wrap">
            {feedback.message}
          </div>
        </div>

        <div className="space-y-2">
          <label htmlFor="feedback-response" className="text-sm font-medium text-gray-700 dark:text-gray-200">
            {t('feedback.admin.details.response_label')}
          </label>
          <textarea
            id="feedback-response"
            value={responseText}
            onChange={(e) => setResponseText(e.target.value)}
            className="form-textarea w-full min-h-[160px]"
            placeholder={t('feedback.admin.details.respond_placeholder')}
          />
        </div>

        <div className="flex justify-end gap-3">
          <button
            type="button"
            onClick={onClose}
            className="btn-secondary"
            disabled={isResponding}
          >
            {t('common.cancel')}
          </button>
          <button
            type="submit"
            className="btn-primary"
            disabled={isSaveDisabled}
          >
            {isResponding ? (
              <span className="flex items-center gap-2">
                <span className="h-4 w-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
                {t('common.processing')}
              </span>
            ) : (
              t('feedback.admin.details.save')
            )}
          </button>
        </div>
      </form>
    </Modal>
  )
}

