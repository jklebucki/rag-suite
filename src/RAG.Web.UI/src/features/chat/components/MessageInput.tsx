import React from 'react'
import { FileSearch, FileText, Loader2, Paperclip, Send, X } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import useAutoGrowTextarea from '../hooks/useAutoGrowTextarea'
import type { ChatAttachmentDraft, ChatContextUsage } from '@/features/chat/types/chat'

interface MessageInputProps {
  message: string
  onMessageChange: (message: string) => void
  onSendMessage: (e: React.FormEvent) => void
  isSending: boolean
  useDocumentSearch: boolean
  onUseDocumentSearchChange: (value: boolean) => void
  contextUsage?: ChatContextUsage | null
  attachments?: ChatAttachmentDraft[]
  onAttachFiles?: (files: File[]) => void
  onRemoveAttachment?: (attachmentId: string) => void
  isUploadingAttachments?: boolean
}

const TEXT_ATTACHMENT_ACCEPT = [
  '.txt', '.md', '.markdown', '.csv', '.tsv', '.json', '.yaml', '.yml', '.xml', '.log', '.ini', '.env',
  '.sql', '.html', '.htm', '.css', '.js', '.jsx', '.ts', '.tsx', '.cs', '.py', '.sh', '.ps1'
].join(',')

export const MessageInput = React.forwardRef<HTMLTextAreaElement, MessageInputProps>(({
  message,
  onMessageChange,
  onSendMessage,
  isSending,
  useDocumentSearch,
  onUseDocumentSearchChange,
  contextUsage,
  attachments = [],
  onAttachFiles,
  onRemoveAttachment,
  isUploadingAttachments = false,
}, ref) => {
  const { t } = useI18n()
  const textareaRef = React.useRef<HTMLTextAreaElement>(null)
  const fileInputRef = React.useRef<HTMLInputElement>(null)
  const actualRef = (ref as React.RefObject<HTMLTextAreaElement>) || textareaRef
  const isContextLimitExceeded = contextUsage?.isLimitExceeded ?? false
  const isInputDisabled = isSending || isContextLimitExceeded

  // Use shared hook to auto-grow the textarea starting from 3 rows up to 10 rows
  // After hitting 10 rows the textarea will show an internal scrollbar
  // and the parent chat container will continue to expand (layout uses flex)
  // The hook calculates line-height and clamps the height accordingly.
  // Keep `resize-none` to prevent manual resizing by the user.
  useAutoGrowTextarea(actualRef, message, { minRows: 3, maxRows: 10 })

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      onSendMessage(e)
    }
  }

  const handleAttachClick = () => {
    fileInputRef.current?.click()
  }

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(event.target.files ?? [])
    event.target.value = ''
    if (files.length > 0) {
      onAttachFiles?.(files)
    }
  }

  const usageTone = getContextUsageTone(contextUsage?.percentUsed ?? 0, isContextLimitExceeded)

  return (
    <div className="border-t border-gray-200 dark:border-slate-800 p-3 md:p-4 bg-white dark:bg-slate-900 transition-colors">
      {contextUsage && (
        <div className="mb-2">
          <div className="flex items-center justify-between gap-3 text-xs text-gray-600 dark:text-gray-300">
            <span className="font-medium">
              {t('chat.context_usage', {
                percent: String(contextUsage.percentUsed),
                used: formatTokenCount(contextUsage.usedTokens),
                limit: formatTokenCount(contextUsage.limitTokens),
              })}
            </span>
            <span className={usageTone.textClass}>
              {contextUsage.percentUsed}%
            </span>
          </div>
          <div className="mt-1 h-1.5 overflow-hidden rounded-full bg-gray-200 dark:bg-slate-800">
            <div
              className={`h-full rounded-full transition-all ${usageTone.barClass}`}
              style={{ width: `${Math.min(100, Math.max(0, contextUsage.percentUsed))}%` }}
            />
          </div>
          {isContextLimitExceeded && (
            <p className="mt-1 text-xs text-red-600 dark:text-red-400">
              {t('chat.context_limit_reached')}
            </p>
          )}
        </div>
      )}

      <form onSubmit={onSendMessage} className="rounded-2xl border border-gray-200 dark:border-slate-700 bg-white dark:bg-slate-950 shadow-sm focus-within:border-primary-400 dark:focus-within:border-primary-500 transition-colors">
        {attachments.length > 0 && (
          <div className="flex flex-wrap gap-2 border-b border-gray-100 dark:border-slate-800 px-3 pt-3 pb-2">
            {attachments.map((attachment) => (
              <div
                key={attachment.id}
                className="inline-flex max-w-full items-center gap-2 rounded-lg border border-gray-200 dark:border-slate-700 bg-gray-50 dark:bg-slate-900 px-2.5 py-1.5 text-xs text-gray-700 dark:text-gray-200"
                title={`${attachment.fileName} · ${formatTokenCount(attachment.tokenCount)} tokens`}
              >
                <FileText className="h-3.5 w-3.5 shrink-0 text-primary-600 dark:text-primary-300" />
                <span className="max-w-[12rem] truncate font-medium">{attachment.fileName}</span>
                <span className="shrink-0 text-gray-500 dark:text-gray-400">
                  {formatTokenCount(attachment.tokenCount)}
                </span>
                <button
                  type="button"
                  onClick={() => onRemoveAttachment?.(attachment.id)}
                  disabled={isSending}
                  className="rounded p-0.5 text-gray-500 hover:bg-gray-200 hover:text-gray-800 disabled:cursor-not-allowed disabled:opacity-50 dark:text-gray-400 dark:hover:bg-slate-800 dark:hover:text-gray-100"
                  aria-label={t('chat.attachments.remove', { fileName: attachment.fileName })}
                  title={t('chat.attachments.remove', { fileName: attachment.fileName })}
                >
                  <X className="h-3.5 w-3.5" />
                </button>
              </div>
            ))}
          </div>
        )}

        <textarea
          ref={actualRef}
          value={message}
          onChange={(e) => onMessageChange(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder={isContextLimitExceeded ? t('chat.context_limit_reached') : t('chat.input.placeholder')}
          className="w-full min-w-0 resize-none bg-transparent px-3 py-3 text-sm text-gray-900 outline-none placeholder:text-gray-400 disabled:cursor-not-allowed disabled:opacity-60 dark:text-gray-100 dark:placeholder:text-gray-500 md:px-4 md:text-base"
          disabled={isInputDisabled}
          rows={3}
        />

        <div className="flex flex-col gap-2 border-t border-gray-100 dark:border-slate-800 px-3 py-2 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex min-w-0 flex-wrap items-center gap-2">
            <input
              ref={fileInputRef}
              type="file"
              multiple
              accept={TEXT_ATTACHMENT_ACCEPT}
              onChange={handleFileChange}
              className="hidden"
              disabled={isInputDisabled || isUploadingAttachments}
            />
            <button
              type="button"
              onClick={handleAttachClick}
              disabled={isInputDisabled || isUploadingAttachments}
              className="inline-flex h-9 w-9 items-center justify-center rounded-lg text-gray-600 hover:bg-gray-100 hover:text-gray-900 disabled:cursor-not-allowed disabled:opacity-50 dark:text-gray-300 dark:hover:bg-slate-800 dark:hover:text-gray-100"
              aria-label={t('chat.attachments.add')}
              title={t('chat.attachments.add_hint')}
            >
              {isUploadingAttachments ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <Paperclip className="h-4 w-4" />
              )}
            </button>

            <label className="flex min-w-0 items-center gap-2 text-xs md:text-sm text-gray-600 dark:text-gray-300 cursor-pointer select-none">
              <input
                type="checkbox"
                checked={useDocumentSearch}
                onChange={(e) => onUseDocumentSearchChange(e.target.checked)}
                className="form-checkbox"
                disabled={isInputDisabled}
              />
              <FileSearch className="h-4 w-4 shrink-0" />
              <span className="truncate">{t('chat.useDocumentSearch')}</span>
            </label>
          </div>

          <button
            type="submit"
            disabled={!message.trim() || isInputDisabled}
            className="btn-primary disabled:opacity-50 disabled:cursor-not-allowed inline-flex items-center justify-center gap-2 px-3 py-2 md:px-4 shrink-0"
            aria-label={t('chat.send')}
            title={t('chat.send')}
          >
            <Send className="h-4 w-4 md:h-5 md:w-5" />
            <span className="hidden sm:inline">{t('chat.send')}</span>
          </button>
        </div>
      </form>
    </div>
  )
})

MessageInput.displayName = 'MessageInput'

function getContextUsageTone(percent: number, isLimitExceeded: boolean) {
  if (isLimitExceeded || percent >= 90) {
    return {
      barClass: 'bg-red-500',
      textClass: 'text-red-600 dark:text-red-400 font-semibold',
    }
  }

  if (percent >= 70) {
    return {
      barClass: 'bg-amber-500',
      textClass: 'text-amber-700 dark:text-amber-300 font-semibold',
    }
  }

  return {
    barClass: 'bg-primary-500',
    textClass: 'text-gray-600 dark:text-gray-300',
  }
}

function formatTokenCount(value: number): string {
  if (value >= 1000) {
    return `${(value / 1000).toFixed(value >= 10000 ? 0 : 1)}k`
  }

  return String(value)
}
