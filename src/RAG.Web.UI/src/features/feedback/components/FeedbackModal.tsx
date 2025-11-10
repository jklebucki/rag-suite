import React, { useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { Modal } from '@/shared/components/ui/Modal'
import { useI18n } from '@/shared/contexts/I18nContext'
import feedbackService from '@/features/feedback/services/feedback.service'
import { useToast } from '@/shared/contexts/ToastContext'

type AttachmentDraft = {
  id: string
  fileName: string
  contentType: string
  dataUrl: string
  dataBase64: string
  size: number
}

const MAX_ATTACHMENT_SIZE = 5 * 1024 * 1024

interface FeedbackModalProps {
  isOpen: boolean
  onClose: () => void
}

export function FeedbackModal({ isOpen, onClose }: FeedbackModalProps) {
  const { t } = useI18n()
  const { showSuccess, showError } = useToast()
  const queryClient = useQueryClient()

  const [subject, setSubject] = useState('')
  const [message, setMessage] = useState('')
  const [attachments, setAttachments] = useState<AttachmentDraft[]>([])
  const [isSubmitting, setIsSubmitting] = useState(false)

  const resetForm = () => {
    setSubject('')
    setMessage('')
    setAttachments([])
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
        message: trimmedMessage,
        attachments: attachments.map((attachment) => ({
          fileName: attachment.fileName,
          contentType: attachment.contentType,
          dataBase64: attachment.dataBase64
        }))
      })
      showSuccess(t('feedback.modal.success'))
      queryClient.invalidateQueries({ queryKey: ['my-feedback'] })
      resetForm()
      onClose()
    } catch {
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

          <AttachmentPicker
            attachments={attachments}
            onAttachmentsChange={setAttachments}
            isSubmitting={isSubmitting}
          />

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

interface AttachmentPickerProps {
  attachments: AttachmentDraft[]
  onAttachmentsChange: (attachments: AttachmentDraft[]) => void
  isSubmitting: boolean
}

function AttachmentPicker({ attachments, onAttachmentsChange, isSubmitting }: AttachmentPickerProps) {
  const { t } = useI18n()
  const { showError } = useToast()

  const handleFileChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(event.target.files ?? [])
    event.target.value = ''

    if (files.length === 0) {
      return
    }

    if (attachments.length + files.length > 5) {
      showError(t('feedback.modal.attachments_limit'))
      return
    }

    const processed: AttachmentDraft[] = []
    const maxSizeLabel = formatBytes(MAX_ATTACHMENT_SIZE)

    for (const file of files) {
      if (file.size > MAX_ATTACHMENT_SIZE) {
        showError(t('feedback.modal.attachments_too_large', { size: maxSizeLabel }))
        continue
      }

      try {
        const { dataUrl, base64 } = await readFileAsDataUrl(file)
        processed.push({
          id: generateId(),
          fileName: file.name,
          contentType: file.type || 'application/octet-stream',
          dataUrl,
          dataBase64: base64,
          size: file.size
        })
      } catch {
        showError(t('feedback.modal.attachments_error'))
      }
    }

    if (processed.length > 0) {
      onAttachmentsChange([...attachments, ...processed])
    }
  }

  const handleRemove = (id: string) => {
    onAttachmentsChange(attachments.filter((item) => item.id !== id))
  }

  return (
    <div className="space-y-3">
      <div className="space-y-2">
        <label htmlFor="feedback-attachments" className="block text-sm font-medium text-gray-700 dark:text-gray-200">
          {t('feedback.modal.attachments')}
        </label>
        <input
          id="feedback-attachments"
          type="file"
          multiple
          onChange={handleFileChange}
          disabled={isSubmitting || attachments.length >= 5}
          className="block w-full text-sm text-gray-700 dark:text-gray-200 file:mr-3 file:rounded-lg file:border-0 file:bg-primary-500 file:px-4 file:py-2 file:text-sm file:font-medium file:text-white file:cursor-pointer hover:file:bg-primary-600 disabled:file:cursor-not-allowed"
          accept="*/*"
        />
        <p className="text-xs text-gray-500 dark:text-gray-400">
          {t('feedback.modal.attachments_hint', { count: '5', size: formatBytes(MAX_ATTACHMENT_SIZE) })}
        </p>
      </div>

      {attachments.length > 0 && (
        <ul className="space-y-2">
          {attachments.map((attachment) => (
            <li
              key={attachment.id}
              className="flex items-center justify-between rounded-lg border border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/60 px-3 py-2 text-sm text-gray-700 dark:text-gray-200"
            >
              <div className="flex flex-col">
                <span className="font-medium break-all">{attachment.fileName}</span>
                <span className="text-xs text-gray-500 dark:text-gray-400">
                  {formatBytes(attachment.size)} • {attachment.contentType}
                </span>
              </div>
              <button
                type="button"
                onClick={() => handleRemove(attachment.id)}
                className="text-xs font-medium text-red-600 hover:text-red-500 dark:text-red-400 dark:hover:text-red-300"
              >
                {t('feedback.modal.attachments_remove')}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

function readFileAsDataUrl(file: File): Promise<{ dataUrl: string; base64: string }> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = () => {
      const result = reader.result as string
      const base64Index = result.indexOf(',')
      const base64 = base64Index >= 0 ? result.slice(base64Index + 1) : result
      resolve({ dataUrl: result, base64 })
    }
    reader.onerror = () => reject(reader.error)
    reader.readAsDataURL(file)
  })
}

function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  const value = bytes / Math.pow(k, i)
  return `${value.toFixed(value > 100 ? 0 : 1)} ${sizes[i]}`
}

function generateId(): string {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
    return crypto.randomUUID()
  }
  return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`
}
