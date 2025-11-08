import React from 'react'
import { Send, FileSearch } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'

interface MessageInputProps {
  message: string
  onMessageChange: (message: string) => void
  onSendMessage: (e: React.FormEvent) => void
  isSending: boolean
  useDocumentSearch: boolean
  onUseDocumentSearchChange: (value: boolean) => void
}

export const MessageInput = React.forwardRef<HTMLTextAreaElement, MessageInputProps>(({
  message,
  onMessageChange,
  onSendMessage,
  isSending,
  useDocumentSearch,
  onUseDocumentSearchChange,
}, ref) => {
  const { t } = useI18n()
  const textareaRef = React.useRef<HTMLTextAreaElement>(null)
  const actualRef = (ref as React.RefObject<HTMLTextAreaElement>) || textareaRef

  // Auto-resize textarea
  React.useEffect(() => {
    const textarea = actualRef.current
    if (textarea) {
      textarea.style.height = 'auto'
      const scrollHeight = textarea.scrollHeight
      const lineHeight = 24 // approximate line height in pixels
      const maxHeight = lineHeight * 8 // 4 lines max
      textarea.style.height = Math.min(scrollHeight, maxHeight) + 'px'
    }
  }, [message, actualRef])

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      onSendMessage(e)
    }
  }

  return (
    <div className="border-t border-gray-200 p-3 md:p-4 bg-white">
      {/* Document Search Toggle */}
      <div className="mb-2 md:mb-3 flex items-center gap-2">
        <label className="flex items-center gap-2 text-xs md:text-sm text-gray-600 cursor-pointer select-none">
          <input
            type="checkbox"
            checked={useDocumentSearch}
            onChange={(e) => onUseDocumentSearchChange(e.target.checked)}
            className="rounded border-gray-300 text-primary-600 focus:ring-primary-500 h-4 w-4"
            disabled={isSending}
          />
          <FileSearch className="h-4 w-4 shrink-0" />
          <span className="truncate">{t('chat.useDocumentSearch')}</span>
        </label>
      </div>

      <form onSubmit={onSendMessage} className="flex gap-2 md:gap-3">
        <textarea
          ref={actualRef}
          value={message}
          onChange={(e) => onMessageChange(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder={t('chat.input.placeholder')}
          className="flex-1 min-w-0 border border-gray-300 rounded-lg px-3 py-2 md:px-4 md:py-3 text-sm md:text-base focus:ring-2 focus:ring-primary-500 focus:border-transparent transition-shadow resize-none overflow-y-auto"
          disabled={isSending}
          rows={1}
        />
        <button
          type="submit"
          disabled={!message.trim() || isSending}
          className="btn-primary disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2 px-3 py-2 md:px-4 md:py-3 shrink-0"
          aria-label={t('chat.send')}
          title={t('chat.send')}
        >
          <Send className="h-4 w-4 md:h-5 md:w-5" />
          <span className="hidden sm:inline">{t('chat.send')}</span>
        </button>
      </form>
    </div>
  )
})

MessageInput.displayName = 'MessageInput'
