import React, { useState, useRef, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Send, Plus, Trash2, Bot, User, Loader2 } from 'lucide-react'
import { apiClient } from '@/services/api'
import type { ChatMessage, ChatRequest } from '@/types'

export function ChatInterface() {
  const [currentSessionId, setCurrentSessionId] = useState<string | null>(null)
  const [message, setMessage] = useState('')
  const [isTyping, setIsTyping] = useState(false)
  const messagesEndRef = useRef<HTMLDivElement>(null)
  const queryClient = useQueryClient()

  // Get chat sessions
  const { data: sessions = [] } = useQuery({
    queryKey: ['chat-sessions'],
    queryFn: () => apiClient.getChatSessions(),
  })

  // Get current session messages
  const { data: currentSession } = useQuery({
    queryKey: ['chat-session', currentSessionId],
    queryFn: () => currentSessionId ? apiClient.getChatSession(currentSessionId) : null,
    enabled: !!currentSessionId,
  })

  // Send message mutation
  const sendMessageMutation = useMutation({
    mutationFn: ({ sessionId, request }: { sessionId: string; request: ChatRequest }) =>
      apiClient.sendMessage(sessionId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['chat-session', currentSessionId] })
      setMessage('')
      setIsTyping(false)
    },
  })

  // Create new session mutation
  const createSessionMutation = useMutation({
    mutationFn: () => apiClient.createChatSession(),
    onSuccess: (newSession) => {
      queryClient.invalidateQueries({ queryKey: ['chat-sessions'] })
      setCurrentSessionId(newSession.id)
    },
  })

  // Delete session mutation
  const deleteSessionMutation = useMutation({
    mutationFn: (sessionId: string) => apiClient.deleteChatSession(sessionId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['chat-sessions'] })
      if (currentSessionId && deleteSessionMutation.variables === currentSessionId) {
        setCurrentSessionId(null)
      }
    },
  })

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!message.trim()) return

    setIsTyping(true)

    // Create session if none exists
    let sessionId = currentSessionId
    if (!sessionId) {
      try {
        const newSession = await createSessionMutation.mutateAsync()
        sessionId = newSession.id
        setCurrentSessionId(sessionId)
      } catch (error) {
        setIsTyping(false)
        return
      }
    }

    sendMessageMutation.mutate({
      sessionId: sessionId!,
      request: {
        message: message.trim(),
        useRag: true,
      },
    })
  }

  const handleNewSession = () => {
    createSessionMutation.mutate()
  }

  const handleDeleteSession = (sessionId: string) => {
    if (confirm('Are you sure you want to delete this chat session?')) {
      deleteSessionMutation.mutate(sessionId)
    }
  }

  // Auto-scroll to bottom
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [currentSession?.messages])

  return (
    <div className="flex h-[calc(100vh-8rem)] max-w-7xl mx-auto bg-white rounded-lg shadow-sm border overflow-hidden">
      {/* Sidebar - Chat Sessions */}
      <div className="w-80 border-r border-gray-200 flex flex-col">
        <div className="p-4 border-b border-gray-200">
          <button
            onClick={handleNewSession}
            disabled={createSessionMutation.isPending}
            className="w-full btn-primary flex items-center justify-center gap-2"
          >
            <Plus className="h-4 w-4" />
            New Chat
          </button>
        </div>

        <div className="flex-1 overflow-y-auto">
          <div className="p-4 space-y-2">
            {sessions.map((session) => (
              <div
                key={session.id}
                className={`group flex items-center justify-between p-3 rounded-lg cursor-pointer transition-colors ${
                  currentSessionId === session.id
                    ? 'bg-primary-50 border border-primary-200'
                    : 'hover:bg-gray-50'
                }`}
                onClick={() => setCurrentSessionId(session.id)}
              >
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-gray-900 truncate">
                    {session.title || 'New Chat'}
                  </p>
                  <p className="text-xs text-gray-500">
                    {new Date(session.updatedAt).toLocaleDateString()}
                  </p>
                </div>
                <button
                  onClick={(e) => {
                    e.stopPropagation()
                    handleDeleteSession(session.id)
                  }}
                  className="opacity-0 group-hover:opacity-100 p-1 rounded hover:bg-red-100 text-red-600 transition-opacity"
                  title="Delete chat session"
                >
                  <Trash2 className="h-4 w-4" />
                </button>
              </div>
            ))}
          </div>
        </div>
      </div>

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
            <div className="border-t border-gray-200 p-4">
              <form onSubmit={handleSendMessage} className="flex gap-3">
                <input
                  type="text"
                  value={message}
                  onChange={(e) => setMessage(e.target.value)}
                  placeholder="Ask me anything about your knowledge base..."
                  className="flex-1 border border-gray-300 rounded-lg px-4 py-3 focus:ring-2 focus:ring-primary-500 focus:border-transparent"
                  disabled={sendMessageMutation.isPending}
                />
                <button
                  type="submit"
                  disabled={!message.trim() || sendMessageMutation.isPending}
                  className="btn-primary disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
                >
                  <Send className="h-4 w-4" />
                  Send
                </button>
              </form>
            </div>
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
