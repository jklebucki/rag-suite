import React from 'react'
import { Send, FileSearch } from 'lucide-react'
import { useI18n } from '@/contexts/I18nContext'

interface MessageInputProps {
  message: string
  onMessageChange: (message: string) => void
  onSendMessage: (e: React.FormEvent) => void
  isSending: boolean
  useDocumentSearch: boolean
  onUseDocumentSearchChange: (value: boolean) => void
}

export const MessageInput = React.forwardRef<HTMLInputElement, MessageInputProps>(({
  message,
  onMessageChange,
  onSendMessage,
  isSending,
  useDocumentSearch,
  onUseDocumentSearchChange,
}, ref) => {
  const { t } = useI18n()

  return (
    <div className="border-t border-gray-200 p-4">
      {/* Document Search Toggle */}
      <div className="mb-3 flex items-center gap-2">
        <label className="flex items-center gap-2 text-sm text-gray-600 cursor-pointer">
          <input
            type="checkbox"
            checked={useDocumentSearch}
            onChange={(e) => onUseDocumentSearchChange(e.target.checked)}
            className="rounded border-gray-300 text-primary-600 focus:ring-primary-500"
            disabled={isSending}
          />
          <FileSearch className="h-4 w-4" />
          {t('chat.useDocumentSearch')}
        </label>
      </div>

      <form onSubmit={onSendMessage} className="flex gap-3">
        <input
          ref={ref}
          type="text"
          value={message}
          onChange={(e) => onMessageChange(e.target.value)}
          placeholder={t('chat.input.placeholder')}
          className="flex-1 border border-gray-300 rounded-lg px-4 py-3 focus:ring-2 focus:ring-primary-500 focus:border-transparent"
          disabled={isSending}
        />
        <button
          type="submit"
          disabled={!message.trim() || isSending}
          className="btn-primary disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
        >
          <Send className="h-4 w-4" />
          {t('chat.send')}
        </button>
      </form>
    </div>
  )
})
