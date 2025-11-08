import React, { useRef, useEffect } from 'react'
import { Bot, User, Loader2 } from 'lucide-react'
import { useMultilingualChat } from '@/features/chat/hooks/useMultilingualChat'
import { useI18n } from '@/shared/contexts/I18nContext'
import { ChatSidebar } from './ChatSidebar'
import { MessageInput } from './MessageInput'
import { MessageSources } from './MessageSources'
import { MarkdownMessage } from './MarkdownMessage'
import { ConfirmModal } from '@/shared/ui/ConfirmModal'
import { formatDateTime, formatRelativeTime } from '@/utils/date'
import type { ChatMessage } from '@/types'

export function ChatInterface() {
  const { t, language: currentLanguage } = useI18n()
  const inputRef = useRef<HTMLTextAreaElement>(null)

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
    useDocumentSearch,
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
    setUseDocumentSearch,
    isNewSession,
    setIsNewSession,
  } = useMultilingualChat()

  // Get the session to delete for displaying in modal
  const sessionToDeleteData = sessions.find(s => s.id === sessionToDelete)
  const sessionToDeleteTitle = sessionToDeleteData?.title || t('chat.new_conversation')

  // Focus input when new session is created
  useEffect(() => {
    if (isNewSession && inputRef.current) {
      // Small delay to ensure the DOM has updated
      setTimeout(() => {
        inputRef.current?.focus()
        setIsNewSession(false)
      }, 100)
    }
  }, [isNewSession, setIsNewSession])

  return (
    <div className="flex flex-col md:flex-row h-[calc(100vh-8rem)] w-[95%] mx-auto bg-white rounded-lg shadow-sm border overflow-hidden">
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
      <div className="flex-1 flex flex-col min-h-0">
        {currentSession ? (
          <>
            {/* Messages */}
            <div className="flex-1 overflow-y-auto p-4 md:p-6 space-y-4 md:space-y-6">
              {currentSession.messages.map((msg: ChatMessage) => (
                <div key={msg.id} className={`flex items-start gap-2 md:gap-3 ${msg.role === 'user' ? 'flex-row-reverse' : ''}`}>
                  <div className={`p-1.5 md:p-2 rounded-full shrink-0 ${msg.role === 'user' ? 'bg-blue-100' : 'bg-primary-100'}`}>
                    {msg.role === 'user' ? (
                      <User className="h-4 w-4 md:h-5 md:w-5 text-blue-600" />
                    ) : (
                      <Bot className="h-4 w-4 md:h-5 md:w-5 text-primary-600" />
                    )}
                  </div>
                  <div className={`max-w-[85%] md:max-w-5xl ${msg.role === 'user' ? 'bg-blue-500 text-white' : 'bg-gray-100'} rounded-lg p-3 md:p-4`}>
                    <MarkdownMessage content={msg.content} isUserMessage={msg.role === 'user'} />

                    {/* Sources for assistant messages */}
                    {msg.role === 'assistant' && msg.sources && msg.sources.length > 0 && (
                      <MessageSources sources={msg.sources} messageRole={msg.role} />
                    )}

                    {/* Timestamp */}
                    <div
                      className={`mt-2 text-xs ${msg.role === 'user' ? 'text-blue-100' : 'text-gray-500'} cursor-help`}
                      title={`Sent at ${formatDateTime(msg.timestamp, currentLanguage)}`}
                    >
                      <span className="font-medium">{formatRelativeTime(msg.timestamp, currentLanguage)}</span>
                      <span className="ml-2 opacity-75">{formatDateTime(msg.timestamp, currentLanguage)}</span>
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
                <div className="flex items-start gap-2 md:gap-3">
                  <div className="p-1.5 md:p-2 rounded-full bg-primary-100 shrink-0">
                    <Bot className="h-4 w-4 md:h-5 md:w-5 text-primary-600" />
                  </div>
                  <div className="bg-gray-100 rounded-lg p-3 md:p-4 min-w-[100px]">
                    <div className="flex items-center gap-2">
                      <Loader2 className="h-4 w-4 animate-spin text-primary-600" />
                      <span className="text-xs md:text-sm text-gray-600">Assistant is typing...</span>
                    </div>
                    <div className="mt-1 text-xs text-gray-500">
                      <span className="font-medium">now</span>
                      <span className="ml-1 md:ml-2 opacity-75 text-[10px] md:text-xs">{formatDateTime(new Date(), currentLanguage)}</span>
                    </div>
                  </div>
                </div>
              )}

              {/* Documents unavailable notice */}
              {!documentsAvailable && currentSession && currentSession.messages.length > 0 && (
                <div className="flex items-start gap-2 md:gap-3 opacity-90">
                  <div className="p-1.5 md:p-2 rounded-full bg-orange-100 shrink-0">
                    <Bot className="h-4 w-4 md:h-5 md:w-5 text-orange-600" />
                  </div>
                  <div className="bg-orange-50 border border-orange-200 rounded-lg p-3 md:p-4 max-w-[85%] md:max-w-3xl">
                    <div className="flex items-center gap-2 mb-2">
                      <span className="text-xs md:text-sm font-medium text-orange-800">
                        {t('chat.documents_unavailable')}
                      </span>
                    </div>
                    <p className="text-xs md:text-sm text-orange-700">
                      {t('chat.documents_unavailable_message')}
                    </p>
                  </div>
                </div>
              )}

              <div ref={messagesEndRef} />
            </div>

            {/* Message Input */}
            <MessageInput
              ref={inputRef}
              message={message}
              onMessageChange={setMessage}
              onSendMessage={handleSendMessage}
              isSending={sendMessageMutation.isPending}
              useDocumentSearch={useDocumentSearch}
              onUseDocumentSearchChange={setUseDocumentSearch}
            />
          </>
        ) : (
          <div className="flex-1 flex items-center justify-center p-4">
            <div className="text-center max-w-md">
              <Bot className="h-12 w-12 md:h-16 md:w-16 text-gray-300 mx-auto mb-3 md:mb-4" />
              <h3 className="text-base md:text-lg font-medium text-gray-900 mb-2">
                {t('chat.title')}
              </h3>
              <p className="text-sm md:text-base text-gray-600 mb-4 md:mb-6 px-2">
                {t('chat.subtitle')}
              </p>
              <button
                onClick={handleNewSession}
                className="btn-primary text-sm md:text-base"
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
        title={t('chat.delete_session_title') || 'Delete Session'}
        message={t('chat.delete_session_confirm', { title: sessionToDeleteTitle }) || `Are you sure you want to delete the session "${sessionToDeleteTitle}"? This action cannot be undone.`}
        confirmText={t('common.delete')}
        cancelText={t('common.cancel')}
        variant="danger"
        isLoading={deleteSessionMutation.isPending}
      />
    </div>
  )
}
