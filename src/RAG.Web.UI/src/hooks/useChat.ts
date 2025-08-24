import { useState, useRef, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/services/api'
import { useToastContext } from '@/contexts/ToastContext'
import type { ChatRequest } from '@/types'

export function useChat() {
  const [currentSessionId, setCurrentSessionId] = useState<string | null>(null)
  const [message, setMessage] = useState('')
  const [isTyping, setIsTyping] = useState(false)
  const [sessionToDelete, setSessionToDelete] = useState<string | null>(null)
  const messagesEndRef = useRef<HTMLDivElement>(null)
  const queryClient = useQueryClient()
  const { showError, showSuccess } = useToastContext()

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
    onError: (error) => {
      console.error('Failed to send message:', error)
      showError('Failed to send message', 'Please check your connection and try again')
      setIsTyping(false)
    },
  })

  // Create new session mutation
  const createSessionMutation = useMutation({
    mutationFn: () => apiClient.createChatSession(),
    onSuccess: (newSession) => {
      console.log('Session created:', newSession)
      queryClient.invalidateQueries({ queryKey: ['chat-sessions'] })
      setCurrentSessionId(newSession.id)
      showSuccess('Chat session created', 'New conversation started successfully')
    },
    onError: (error) => {
      console.error('Failed to create session:', error)
      showError('Failed to create chat session', 'Please try again later')
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
      showSuccess('Chat session deleted', 'Conversation has been removed successfully')
    },
    onError: (error) => {
      console.error('Failed to delete session:', error)
      showError('Failed to delete chat session', 'Please try again later')
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
    console.log('Creating new session...')
    createSessionMutation.mutate()
  }

  const handleDeleteSession = (sessionId: string) => {
    setSessionToDelete(sessionId)
  }

  const confirmDeleteSession = () => {
    if (sessionToDelete) {
      deleteSessionMutation.mutate(sessionToDelete)
      setSessionToDelete(null)
    }
  }

  const cancelDeleteSession = () => {
    setSessionToDelete(null)
  }

  // Auto-scroll to bottom
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [currentSession?.messages])

  return {
    // State
    currentSessionId,
    message,
    isTyping,
    messagesEndRef,
    sessionToDelete,
    
    // Data
    sessions,
    currentSession,
    
    // Mutations
    sendMessageMutation,
    createSessionMutation,
    deleteSessionMutation,
    
    // Handlers
    handleSendMessage,
    handleNewSession,
    handleDeleteSession,
    confirmDeleteSession,
    cancelDeleteSession,
    setMessage,
    setCurrentSessionId,
  }
}
