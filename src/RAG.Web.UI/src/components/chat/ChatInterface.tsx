import React from 'react'
import { Bot, User, Loader2 } from 'lucide-react'
import { useChat } from '@/hooks/useChat'
import { ChatSidebar } from './ChatSidebar'
import { MessageInput } from './MessageInput'
import type { ChatMessage } from '@/types'

export function ChatInterface() {
  const {
    currentSessionId,
    message,
    isTyping,
    messagesEndRef,
    sessions,
    currentSession,
    sendMessageMutation,
    createSessionMutation,
    handleSendMessage,
    handleNewSession,
    handleDeleteSession,
    setMessage,
    setCurrentSessionId,
  } = useChat()

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
              {currentSession.messages.map((msg) => (
                <ChatMessageItem key={msg.id} message={msg} />
              ))}
              {isTyping && (
                <div className="flex items-start gap-3">
                  <div className="p-2 rounded-full bg-primary-100">
                    <Bot className="h-5 w-5 text-primary-600" />
                  </div>
                  <div className="bg-gray-100 rounded-lg p-3">
                    <Loader2 className="h-4 w-4 animate-spin" />
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
                Welcome to RAG Suite Chat
              </h3>
              <p className="text-gray-600 mb-6 max-w-md">
                Start a new conversation or select an existing chat session to continue.
              </p>
              <button
                onClick={handleNewSession}
                className="btn-primary"
              >
                Start New Chat
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

interface ChatMessageItemProps {
  message: ChatMessage
}

function ChatMessageItem({ message }: ChatMessageItemProps) {
  const isUser = message.role === 'user'

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
            <p className="font-medium mb-1">Sources:</p>
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