import { useState, useRef, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/services/api'
import { useToastContext } from '@/contexts/ToastContext'
import { useI18n } from '@/contexts/I18nContext'
import type { MultilingualChatRequest, MultilingualChatResponse, ChatSession, ChatMessage } from '@/types'

export function useMultilingualChat() {
  const [currentSessionId, setCurrentSessionId] = useState<string | null>(null)
  const [message, setMessage] = useState('')
  const [isTyping, setIsTyping] = useState(false)
  const [sessionToDelete, setSessionToDelete] = useState<string | null>(null)
  const [lastResponse, setLastResponse] = useState<MultilingualChatResponse | null>(null)
  const messagesEndRef = useRef<HTMLDivElement>(null)
  const queryClient = useQueryClient()
  const { showError, showSuccess } = useToastContext()
  const { language: currentLanguage } = useI18n()

  // Get chat sessions (reuse existing endpoint)
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

  // Send multilingual message mutation
  const sendMultilingualMessageMutation = useMutation({
    mutationFn: ({ sessionId, request }: { sessionId: string; request: MultilingualChatRequest }) =>
      apiClient.sendMultilingualMessage(sessionId, request),
    onSuccess: (response) => {
      setLastResponse(response)
      queryClient.invalidateQueries({ queryKey: ['chat-session', currentSessionId] })
      setMessage('')
      setIsTyping(false)
      
      // Show language detection info if available
      if (response.detectedLanguage !== currentLanguage) {
        showSuccess(
          'Language detected', 
          `Message detected as ${response.detectedLanguage}, responded in ${response.responseLanguage}`
        )
      }
    },
    onError: (error) => {
      console.error('Failed to send multilingual message:', error)
      showError('Failed to send message', 'Please check your connection and try again')
      setIsTyping(false)
    },
  })

  // Create new session mutation (reuse existing endpoint)
  const createSessionMutation = useMutation({
    mutationFn: (title?: string) => apiClient.createChatSession(title),
    onSuccess: (newSession) => {
      console.log('Session created:', newSession)
      queryClient.invalidateQueries({ queryKey: ['chat-sessions'] })
      setCurrentSessionId(newSession.id)
    },
    onError: (error) => {
      console.error('Failed to create session:', error)
      showError('Failed to create new session', 'Please try again')
    },
  })

  // Delete session mutation (reuse existing endpoint)
  const deleteSessionMutation = useMutation({
    mutationFn: (sessionId: string) => apiClient.deleteChatSession(sessionId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['chat-sessions'] })
      setSessionToDelete(null)
      if (currentSessionId === sessionToDelete) {
        setCurrentSessionId(null)
      }
      showSuccess('Session deleted', 'Chat session has been deleted successfully')
    },
    onError: (error) => {
      console.error('Failed to delete session:', error)
      showError('Failed to delete session', 'Please try again')
      setSessionToDelete(null)
    },
  })

  const handleSendMessage = async () => {
    if (!message.trim() || !currentSessionId) return

    setIsTyping(true)
    
    const request: MultilingualChatRequest = {
      message: message.trim(),
      language: currentLanguage,
      responseLanguage: currentLanguage,
      enableTranslation: true,
      metadata: {
        uiLanguage: currentLanguage,
        timestamp: new Date().toISOString()
      }
    }

    sendMultilingualMessageMutation.mutate({ sessionId: currentSessionId, request })
  }

  const handleCreateSession = async () => {
    createSessionMutation.mutate(undefined)
  }

  const handleNewSession = async () => {
    createSessionMutation.mutate(undefined)
  }

  const handleDeleteSession = (sessionId: string) => {
    setSessionToDelete(sessionId)
  }

  const confirmDeleteSession = () => {
    if (sessionToDelete) {
      deleteSessionMutation.mutate(sessionToDelete)
    }
  }

  const cancelDeleteSession = () => {
    setSessionToDelete(null)
  }

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleSendMessage()
    }
  }

  // Auto-scroll to bottom when new messages arrive
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [currentSession?.messages])

  // Set first session as current if none selected
  useEffect(() => {
    if (!currentSessionId && sessions.length > 0) {
      setCurrentSessionId(sessions[0].id)
    }
  }, [sessions, currentSessionId])

  return {
    // State
    currentSessionId,
    setCurrentSessionId,
    message,
    setMessage,
    isTyping,
    sessions,
    currentSession,
    lastResponse,
    messagesEndRef,
    sessionToDelete,
    
    // Multilingual specific data
    lastMessageLanguage: lastResponse?.detectedLanguage,
    translationStatus: lastResponse?.wasTranslated ? 'translated' : 'original',
    
    // Mutations for compatibility with ChatInterface
    sendMessageMutation: sendMultilingualMessageMutation,
    createSessionMutation,
    deleteSessionMutation,
    
    // Actions
    handleSendMessage,
    handleCreateSession,
    handleNewSession,
    handleDeleteSession,
    confirmDeleteSession,
    cancelDeleteSession,
    handleKeyPress,
    
    // Loading states
    isCreatingSession: createSessionMutation.isPending,
    isDeletingSession: deleteSessionMutation.isPending,
    isSendingMessage: sendMultilingualMessageMutation.isPending,
  }
}
