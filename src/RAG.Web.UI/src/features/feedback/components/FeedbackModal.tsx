import React, { useState } from 'react'
import { Modal } from '@/shared/components/ui/Modal'
import { useI18n } from '@/shared/contexts/I18nContext'
import feedbackService from '@/features/feedback/services/feedback.service'
import { useToast } from '@/shared/contexts/ToastContext'

interface FeedbackModalProps {
  isOpen: boolean
  onClose: () => void
}

export function FeedbackModal({ isOpen, onClose }: FeedbackModalProps) {
  const { t } = useI18n()
  const { showSuccess, showError } = useToast()

  const [subject, setSubject] = useState('')
  const [message, setMessage] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  const resetForm = () => {
    setSubject('')
    setMessage('')
  }

  const handleClose = () => {
    if (isSubmitting) return
    resetForm()
    onClose()
  }

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()
    const trimmedSubject = subject.trim()
    const trimmedMessage = message.trim()

    if (!trimmedSubject || !trimmedMessage) {
      showError(t('feedback.modal.validation_required'))
      return
    }

    try {
      setIsSubmitting(true)
      await feedbackService.submitFeedback({
        subject: trimmedSubject,
        message: trimmedMessage
      })
      showSuccess(t('feedback.modal.success'))
      resetForm()
      onClose()
    } catch (error) {
      showError(t('feedback.modal.error'))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title={
        <div className="flex flex-col gap-1">
          <h3 className="text-lg sm:text-xl font-semibold text-gray-900 dark:text-gray-100">
            {t('feedback.modal.title')}
          </h3>
          <p className="text-sm text-gray-600 dark:text-gray-400">
            {t('feedback.modal.subtitle')}
          </p>
        </div>
      }
      size="lg"
    >
      <form onSubmit={handleSubmit} className="p-6">
        <div className="space-y-6 max-w-3xl mx-auto">
          <div className="space-y-2">
            <label htmlFor="feedback-subject" className="block text-sm font-medium text-gray-700 dark:text-gray-200">
              {t('feedback.modal.subject')}
            </label>
            <input
              id="feedback-subject"
              type="text"
              value={subject}
              onChange={(e) => setSubject(e.target.value)}
              className="form-input w-full"
              placeholder={t('feedback.modal.subject_placeholder')}
              maxLength={200}
              disabled={isSubmitting}
            />
            <p className="text-xs text-gray-500 dark:text-gray-400">
              {t('feedback.modal.subject_hint')}
            </p>
          </div>

          <div className="space-y-2">
            <label htmlFor="feedback-message" className="block text-sm font-medium text-gray-700 dark:text-gray-200">
              {t('feedback.modal.message')}
            </label>
            <textarea
              id="feedback-message"
              value={message}
              onChange={(e) => setMessage(e.target.value)}
              className="form-textarea w-full min-h-[220px]"
              placeholder={t('feedback.modal.message_placeholder')}
              maxLength={4000}
              disabled={isSubmitting}
            />
            <div className="flex items-center justify-between text-xs text-gray-500 dark:text-gray-400">
              <span>{t('feedback.modal.message_hint')}</span>
              <span>{t('feedback.modal.characters_left', { count: (4000 - message.length).toString() })}</span>
            </div>
          </div>

          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
            <p className="text-xs text-gray-500 dark:text-gray-400">
              {t('feedback.modal.notice')}
            </p>
            <div className="flex gap-3 justify-end">
              <button
                type="button"
                onClick={handleClose}
                className="btn-secondary"
                disabled={isSubmitting}
              >
                {t('common.cancel')}
              </button>
              <button
                type="submit"
                className="btn-primary"
                disabled={isSubmitting}
              >
                {isSubmitting ? (
                  <span className="flex items-center gap-2">
                    <span className="h-4 w-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
                    {t('common.processing')}
                  </span>
                ) : (
                  t('feedback.modal.submit')
                )}
              </button>
            </div>
          </div>
        </div>
      </form>
    </Modal>
  )
}

