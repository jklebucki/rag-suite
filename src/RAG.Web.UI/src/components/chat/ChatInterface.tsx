import React from 'react'
import { Bot, User, Loader2 } from 'lucide-react'
import { useMultilingualChat } from '@/hooks/useMultilingualChat'
import { useI18n } from '@/contexts/I18nContext'
import { ChatSidebar } from './ChatSidebar'
import { MessageInput } from './MessageInput'
import { ConfirmModal } from '@/components/ui/ConfirmModal'
import type { ChatMessage } from '@/types'

export function ChatInterface() {
  const { t, language: currentLanguage } = useI18n()
  const {
    currentSessionId,
    message,
    isTyping,
    messagesEndRef,
    sessionToDelete,
    sessions,
    currentSession,
    lastMessageLanguage,
    translationStatus,
    documentsAvailable,
    sendMessageMutation,
    createSessionMutation,
    deleteSessionMutation,
    handleSendMessage,
    handleNewSession,
    handleDeleteSession,
    confirmDeleteSession,
    cancelDeleteSession,
    setMessage,
    setCurrentSessionId,
  } = useMultilingualChat()

  // Format timestamp as YYYY-MM-DD HH:mm:ss
  const formatTimestamp = (timestamp: Date | string) => {
    const date = typeof timestamp === 'string' ? new Date(timestamp) : timestamp
    return date.toISOString().slice(0, 19).replace('T', ' ')
  }

  // Format timestamp for relative time (e.g., "2 minutes ago")
  const formatRelativeTime = (timestamp: Date | string) => {
    const date = typeof timestamp === 'string' ? new Date(timestamp) : timestamp
    const now = new Date()
    const diffInMs = now.getTime() - date.getTime()
    const diffInSeconds = Math.floor(diffInMs / 1000)
    const diffInMinutes = Math.floor(diffInSeconds / 60)
    const diffInHours = Math.floor(diffInMinutes / 60)
    const diffInDays = Math.floor(diffInHours / 24)

    if (diffInSeconds < 60) {
      return 'just now'
    } else if (diffInMinutes < 60) {
      return `${diffInMinutes} minute${diffInMinutes !== 1 ? 's' : ''} ago`
    } else if (diffInHours < 24) {
      return `${diffInHours} hour${diffInHours !== 1 ? 's' : ''} ago`
    } else if (diffInDays < 7) {
      return `${diffInDays} day${diffInDays !== 1 ? 's' : ''} ago`
    } else {
      return formatTimestamp(date)
    }
  }

  return (
    <div className="flex h-[calc(100vh-8rem)] max-w-7xl mx-auto bg-white rounded-lg shadow-sm border overflow-hidden">
      {/* Sidebar - Chat Sessions */}
      <ChatSidebar
        sessions={sessions}
        currentSessionId={currentSessionId}
        onNewSession={handleNewSession}
        onSelectSession={setCurrentSessionId}
        onDeleteSession={handleDeleteSession}
        isCreatingSession={createSessionMutation.isPending}
      />

      {/* Main Chat Area */}
      <div className="flex-1 flex flex-col">
        {currentSession ? (
          <>
            {/* Messages */}
            <div className="flex-1 overflow-y-auto p-6 space-y-6">
              {currentSession.messages.map((msg: ChatMessage) => (
                <div key={msg.id} className={`flex items-start gap-3 ${msg.role === 'user' ? 'flex-row-reverse' : ''}`}>
                  <div className={`p-2 rounded-full ${msg.role === 'user' ? 'bg-blue-100' : 'bg-primary-100'}`}>
                    {msg.role === 'user' ? (
                      <User className="h-5 w-5 text-blue-600" />
                    ) : (
                      <Bot className="h-5 w-5 text-primary-600" />
                    )}
                  </div>
                  <div className={`max-w-3xl ${msg.role === 'user' ? 'bg-blue-500 text-white' : 'bg-gray-100'} rounded-lg p-4`}>
                    <div className="whitespace-pre-wrap">{msg.content}</div>
                    
                    {/* Timestamp */}
                    <div 
                      className={`mt-2 text-xs ${msg.role === 'user' ? 'text-blue-100' : 'text-gray-500'} cursor-help`}
                      title={`Sent at ${formatTimestamp(msg.timestamp)}`}
                    >
                      <span className="font-medium">{formatRelativeTime(msg.timestamp)}</span>
                      <span className="ml-2 opacity-75">{formatTimestamp(msg.timestamp)}</span>
                    </div>
                    
                    {/* Language detection info */}
                    {lastMessageLanguage && msg.id === currentSession.messages[currentSession.messages.length - 1]?.id && (
                      <div className="mt-1 text-xs opacity-75">
                        {lastMessageLanguage !== currentLanguage && (
                          <span className={msg.role === 'user' ? 'text-blue-200' : 'text-blue-600'}>
                            Detected: {lastMessageLanguage} • Response: {currentLanguage}
                          </span>
                        )}
                        {translationStatus === 'translated' && (
                          <span className={`ml-2 ${msg.role === 'user' ? 'text-green-200' : 'text-green-600'}`}>
                            ✓ Translated
                          </span>
                        )}
                      </div>
                    )}
                  </div>
                </div>
              ))}
              {isTyping && (
                <div className="flex items-start gap-3">
                  <div className="p-2 rounded-full bg-primary-100">
                    <Bot className="h-5 w-5 text-primary-600" />
                  </div>
                  <div className="bg-gray-100 rounded-lg p-4 min-w-[100px]">
                    <div className="flex items-center gap-2">
                      <Loader2 className="h-4 w-4 animate-spin text-primary-600" />
                      <span className="text-sm text-gray-600">Assistant is typing...</span>
                    </div>
                    <div className="mt-1 text-xs text-gray-500">
                      <span className="font-medium">now</span>
                      <span className="ml-2 opacity-75">{formatTimestamp(new Date())}</span>
                    </div>
                  </div>
                </div>
              )}
              
              {/* Documents unavailable notice */}
              {!documentsAvailable && currentSession && currentSession.messages.length > 0 && (
                <div className="flex items-start gap-3 opacity-90">
                  <div className="p-2 rounded-full bg-orange-100">
                    <Bot className="h-5 w-5 text-orange-600" />
                  </div>
                  <div className="bg-orange-50 border border-orange-200 rounded-lg p-4 max-w-3xl">
                    <div className="flex items-center gap-2 mb-2">
                      <span className="text-sm font-medium text-orange-800">
                        {t('chat.documents_unavailable')}
                      </span>
                    </div>
                    <p className="text-sm text-orange-700">
                      {t('chat.documents_unavailable_message')}
                    </p>
                  </div>
                </div>
              )}
              
              <div ref={messagesEndRef} />
            </div>

            {/* Message Input */}
            <MessageInput
              message={message}
              onMessageChange={setMessage}
              onSendMessage={handleSendMessage}
              isSending={sendMessageMutation.isPending}
            />
          </>
        ) : (
          <div className="flex-1 flex items-center justify-center">
            <div className="text-center">
              <Bot className="h-16 w-16 text-gray-300 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">
                {t('chat.title')}
              </h3>
              <p className="text-gray-600 mb-6 max-w-md">
                {t('chat.subtitle')}
              </p>
              <button
                onClick={handleNewSession}
                className="btn-primary"
              >
                {t('chat.new_session')}
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Delete Confirmation Modal */}
      <ConfirmModal
        isOpen={!!sessionToDelete}
        onClose={cancelDeleteSession}
        onConfirm={confirmDeleteSession}
        title={t('chat.sessions')}
        message={t('chat.no_sessions')}
        confirmText={t('common.delete')}
        cancelText={t('common.cancel')}
        variant="danger"
        isLoading={deleteSessionMutation.isPending}
      />
    </div>
  )
}

interface ChatMessageItemProps {
  message: ChatMessage
}

function ChatMessageItem({ message }: ChatMessageItemProps) {
  const isUser = message.role === 'user'
  const { t } = useI18n()

  return (
    <div className={`flex gap-3 ${isUser ? 'justify-end' : 'justify-start'}`}>
      {!isUser && (
        <div className="p-2 rounded-full bg-primary-100 flex-shrink-0">
          <Bot className="h-5 w-5 text-primary-600" />
        </div>
      )}

      <div className={`max-w-3xl ${isUser ? 'order-1' : ''}`}>
        <div
          className={`rounded-lg p-4 ${
            isUser
              ? 'bg-primary-500 text-white'
              : 'bg-gray-100 text-gray-900'
          }`}
        >
          <p className="whitespace-pre-wrap">{message.content}</p>
        </div>

        {message.sources && message.sources.length > 0 && (
          <div className="mt-2 text-xs text-gray-500">
            <p className="font-medium mb-1">{t('chat.sources')}:</p>
            <div className="space-y-1">
              {message.sources.slice(0, 3).map((source, index) => (
                <div key={index} className="flex items-center gap-2">
                  <span className="w-4 h-4 bg-primary-100 text-primary-600 rounded-full flex items-center justify-center text-xs">
                    {index + 1}
                  </span>
                  <span className="truncate">{source.title}</span>
                </div>
              ))}
            </div>
          </div>
        )}

        <p className="text-xs text-gray-500 mt-2">
          {new Date(message.timestamp).toLocaleTimeString()}
        </p>
      </div>

      {isUser && (
        <div className="p-2 rounded-full bg-gray-100 flex-shrink-0">
          <User className="h-5 w-5 text-gray-600" />
        </div>
      )}
    </div>
  )
}