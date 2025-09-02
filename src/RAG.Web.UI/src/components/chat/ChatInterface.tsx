import React from 'react'
import { Bot, User, Loader2 } from 'lucide-react'
import { useMultilingualChat } from '@/hooks/useMultilingualChat'
import { useI18n } from '@/contexts/I18nContext'
import { ChatSidebar } from './ChatSidebar'
import { MessageInput } from './MessageInput'
import { MessageSources } from './MessageSources'
import { ConfirmModal } from '@/components/ui/ConfirmModal'
import { formatDateTime, formatRelativeTime } from '@/utils/date'
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
                      <span className="ml-2 opacity-75">{formatDateTime(new Date(), currentLanguage)}</span>
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