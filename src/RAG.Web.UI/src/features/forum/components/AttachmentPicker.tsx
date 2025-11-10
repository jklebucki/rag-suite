import React from 'react'
import { useI18n } from '@/shared/contexts/I18nContext'
import { useToast } from '@/shared/contexts/ToastContext'
import type { UploadAttachment } from '../types/forum'

const MAX_ATTACHMENTS = 5
const MAX_ATTACHMENT_SIZE = 5 * 1024 * 1024

export interface AttachmentDraft extends UploadAttachment {
  id: string
  dataUrl: string
}

interface AttachmentPickerProps {
  attachments: AttachmentDraft[]
  onAttachmentsChange: (attachments: AttachmentDraft[]) => void
  disabled?: boolean
  inputId: string
}

export function AttachmentPicker({ attachments, onAttachmentsChange, disabled = false, inputId }: AttachmentPickerProps) {
  const { t } = useI18n()
  const { showError } = useToast()

  const handleFileChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(event.target.files ?? [])
    event.target.value = ''

    if (files.length === 0) {
      return
    }

    if (attachments.length + files.length > MAX_ATTACHMENTS) {
      showError(t('forum.attachments.limit', { count: String(MAX_ATTACHMENTS) }))
      return
    }

    const processed: AttachmentDraft[] = []

    for (const file of files) {
      if (file.size > MAX_ATTACHMENT_SIZE) {
        showError(t('forum.attachments.tooLarge', { size: formatBytes(MAX_ATTACHMENT_SIZE) }))
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
          size: file.size,
        })
      } catch {
        showError(t('forum.attachments.error'))
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
        <label htmlFor={inputId} className="block text-sm font-medium text-gray-700 dark:text-gray-200">
          {t('forum.attachments.label')}
        </label>
        <input
          id={inputId}
          type="file"
          multiple
          onChange={handleFileChange}
          disabled={disabled || attachments.length >= MAX_ATTACHMENTS}
          className="block w-full text-sm text-gray-700 dark:text-gray-200 file:mr-3 file:rounded-lg file:border-0 file:bg-primary-500 file:px-4 file:py-2 file:text-sm file:font-medium file:text-white file:cursor-pointer hover:file:bg-primary-600 disabled:file:cursor-not-allowed"
          accept="*/*"
        />
        <p className="text-xs text-gray-500 dark:text-gray-400">
          {t('forum.attachments.hint', {
            count: String(MAX_ATTACHMENTS),
            size: formatBytes(MAX_ATTACHMENT_SIZE),
          })}
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
                {t('forum.attachments.remove')}
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

function generateId(): string {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
    return crypto.randomUUID()
  }
  return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`
}

function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  const value = bytes / Math.pow(k, i)
  return `${value.toFixed(value > 100 ? 0 : 1)} ${sizes[i]}`
}

