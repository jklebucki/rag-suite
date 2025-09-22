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
  const [documentsAvailable, setDocumentsAvailable] = useState<boolean>(true)
  const [useDocumentSearch, setUseDocumentSearch] = useState<boolean>(false)
  const [isNewSession, setIsNewSession] = useState<boolean>(false)
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
    mutationFn: ({ sessionId, request }: { sessionId: string; request: MultilingualChatRequest }) => {
      console.log('Calling sendMultilingualMessage API with:', { sessionId, request })
      return apiClient.sendMultilingualMessage(sessionId, request)
    },
    retry: false, // 🆕 No retry to prevent double sending
    onSuccess: (response) => {
      console.log('sendMultilingualMessage success:', response)
      setLastResponse(response)

      // Check if documents are available from metadata
      if (response.metadata && response.metadata.documentsAvailable !== undefined) {
        setDocumentsAvailable(response.metadata.documentsAvailable as boolean)
      }

      // Refresh the entire session to get the real messages from server
      queryClient.invalidateQueries({ queryKey: ['chat-session', currentSessionId] })
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
    mutationFn: ({ title, language }: { title?: string; language?: string }) => apiClient.createChatSession(title, language),
    onSuccess: (newSession) => {
      console.log('Session created:', newSession)
      queryClient.invalidateQueries({ queryKey: ['chat-sessions'] })
      setCurrentSessionId(newSession.id)
      setIsNewSession(true)
    },
    onError: (error) => {
      console.error('Failed to create session:', error)
      showError('Failed to create new session', 'Please try again')
    },
  })

  // Delete session mutation (reuse existing endpoint)
  const deleteSessionMutation = useMutation({
    mutationFn: (sessionId: string) => apiClient.deleteChatSession(sessionId),
    onSuccess: (_, deletedSessionId) => {
      queryClient.invalidateQueries({ queryKey: ['chat-sessions'] })
      setSessionToDelete(null)
      // Clear current session if it was the one that got deleted
      if (currentSessionId === deletedSessionId) {
        setCurrentSessionId(null)
        // Also clear the current session data from cache
        queryClient.setQueryData(['chat-session', deletedSessionId], null)
      }
      showSuccess('Session deleted', 'Chat session has been deleted successfully')
    },
    onError: (error) => {
      console.error('Failed to delete session:', error)
      showError('Failed to delete session', 'Please try again')
      setSessionToDelete(null)
    },
  })

  const handleSendMessage = async (e?: React.FormEvent) => {
    if (e) {
      e.preventDefault()
    }

    console.log('handleSendMessage called', { message: message.trim(), currentSessionId, isTyping })

    if (!message.trim()) {
      console.log('Message is empty, aborting')
      return
    }

    if (!currentSessionId) {
      console.log('No current session, aborting')
      return
    }

    // 🆕 Prevent double sending when already sending
    if (isTyping || sendMultilingualMessageMutation.isPending) {
      console.log('Already sending message, aborting')
      return
    }

    const userMessage = message.trim()
    const messageTimestamp = new Date()

    // Create temporary user message to show immediately
    const tempUserMessage: ChatMessage = {
      id: `temp-${Date.now()}`,
      content: userMessage,
      role: 'user',
      timestamp: messageTimestamp
    }

    // Add user message to local state immediately
    queryClient.setQueryData(['chat-session', currentSessionId], (oldData: ChatSession | undefined) => {
      if (!oldData) return oldData
      return {
        ...oldData,
        messages: [...oldData.messages, tempUserMessage]
      }
    })

    console.log('Setting isTyping to true')
    setIsTyping(true)
    setMessage('') // Clear input immediately

    const request: MultilingualChatRequest = {
      message: userMessage,
      language: currentLanguage,
      responseLanguage: currentLanguage,
      enableTranslation: true,
      useDocumentSearch: useDocumentSearch,
      metadata: {
        uiLanguage: currentLanguage,
        timestamp: messageTimestamp.toISOString()
      }
    }

    console.log('Sending multilingual message:', request)
    sendMultilingualMessageMutation.mutate({ sessionId: currentSessionId, request })
  }

  const handleCreateSession = async () => {
    createSessionMutation.mutate({ title: undefined, language: currentLanguage })
  }

  const handleNewSession = async () => {
    createSessionMutation.mutate({ title: undefined, language: currentLanguage })
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
    console.log('Session selection effect:', { currentSessionId, sessionsLength: sessions.length, sessions })
    if (!currentSessionId && sessions.length > 0) {
      console.log('Setting first session as current:', sessions[0].id)
      setCurrentSessionId(sessions[0].id)
    }
  }, [sessions, currentSessionId])

  // Clear current session if it no longer exists in the sessions list
  useEffect(() => {
    if (currentSessionId && sessions.length > 0) {
      const sessionExists = sessions.some(session => session.id === currentSessionId)
      if (!sessionExists) {
        console.log('Current session no longer exists, clearing:', currentSessionId)
        setCurrentSessionId(null)
        // Clear the session data from cache
        queryClient.setQueryData(['chat-session', currentSessionId], null)
      }
    }
  }, [sessions, currentSessionId, queryClient])

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
    isNewSession,
    setIsNewSession,

    // Multilingual specific data
    lastMessageLanguage: lastResponse?.detectedLanguage,
    translationStatus: lastResponse?.wasTranslated ? 'translated' : 'original',
    documentsAvailable,
    useDocumentSearch,

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
    setUseDocumentSearch,

    // Loading states
    isCreatingSession: createSessionMutation.isPending,
    isDeletingSession: deleteSessionMutation.isPending,
    isSendingMessage: sendMultilingualMessageMutation.isPending,
  }
}
