/**
 * MessageItem - Optimized message component using React.memo
 * 
 * This component is memoized to prevent unnecessary re-renders when other
 * messages in the list are updated.
 */

import React from 'react'
import { Bot, User } from 'lucide-react'
import { MarkdownMessage } from './MarkdownMessage'
import { MessageSources } from './MessageSources'
import { formatDateTime, formatRelativeTime } from '@/utils/date'
import type { ChatMessage } from '@/features/chat/types/chat'
import type { LanguageCode } from '@/shared/types/i18n'

interface MessageItemProps {
  message: ChatMessage
  currentLanguage: LanguageCode
  lastMessageLanguage?: string | null
  translationStatus?: 'translated' | 'original'
  isLastMessage: boolean
}

export const MessageItem = React.memo<MessageItemProps>(({
  message,
  currentLanguage,
  lastMessageLanguage,
  translationStatus,
  isLastMessage,
}) => {
  return (
    <div className={`flex items-start gap-2 md:gap-3 ${message.role === 'user' ? 'flex-row-reverse' : ''}`}>
      <div className={`p-1.5 md:p-2 rounded-full shrink-0 shadow-sm ${message.role === 'user' ? 'bg-blue-100 dark:bg-blue-900/40' : 'bg-primary-100 dark:bg-primary-900/30'}`}>
        {message.role === 'user' ? (
          <User className="h-4 w-4 md:h-5 md:w-5 text-blue-600 dark:text-blue-300" />
        ) : (
          <Bot className="h-4 w-4 md:h-5 md:w-5 text-primary-600 dark:text-primary-300" />
        )}
      </div>
      <div
        className={`max-w-[85%] md:max-w-5xl rounded-2xl p-3 md:p-4 shadow-sm transition-colors ${
          message.role === 'user'
            ? 'bg-blue-600 text-white'
            : 'bg-white dark:bg-slate-900 border border-gray-100 dark:border-slate-800 text-gray-900 dark:text-gray-100'
        }`}
      >
        <MarkdownMessage content={message.content} isUserMessage={message.role === 'user'} />

        {/* Sources for assistant messages */}
        {message.role === 'assistant' && message.sources && message.sources.length > 0 && (
          <MessageSources sources={message.sources} messageRole={message.role} />
        )}

        {/* Timestamp */}
        <div
          className={`mt-2 text-xs cursor-help ${
            message.role === 'user' ? 'text-blue-100' : 'text-gray-500 dark:text-slate-400'
          }`}
          title={`Sent at ${formatDateTime(message.timestamp, currentLanguage)}`}
        >
          <span className="font-medium">{formatRelativeTime(message.timestamp, currentLanguage)}</span>
          <span className="ml-2 opacity-75">{formatDateTime(message.timestamp, currentLanguage)}</span>
        </div>

        {/* Language detection info */}
        {lastMessageLanguage && isLastMessage && (
          <div className="mt-1 text-xs opacity-75">
            {lastMessageLanguage !== currentLanguage && (
              <span className={message.role === 'user' ? 'text-blue-200' : 'text-blue-600 dark:text-blue-300'}>
                Detected: {lastMessageLanguage} • Response: {currentLanguage}
              </span>
            )}
            {translationStatus === 'translated' && (
              <span className={`ml-2 ${message.role === 'user' ? 'text-green-200' : 'text-green-600 dark:text-green-400'}`}>
                ✓ Translated
              </span>
            )}
          </div>
        )}
      </div>
    </div>
  )
}, (prevProps, nextProps) => {
  // Custom comparison function for better performance
  return (
    prevProps.message.id === nextProps.message.id &&
    prevProps.message.content === nextProps.message.content &&
    prevProps.message.timestamp === nextProps.message.timestamp &&
    prevProps.currentLanguage === nextProps.currentLanguage &&
    prevProps.lastMessageLanguage === nextProps.lastMessageLanguage &&
    prevProps.translationStatus === nextProps.translationStatus &&
    prevProps.isLastMessage === nextProps.isLastMessage
  )
})

MessageItem.displayName = 'MessageItem'

