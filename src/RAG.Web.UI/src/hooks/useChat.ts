import { useState, useRef, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/services/api'
import type { ChatRequest } from '@/types'

export function useChat() {
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
      console.log('Session created:', newSession)
      queryClient.invalidateQueries({ queryKey: ['chat-sessions'] })
      setCurrentSessionId(newSession.id)
    },
    onError: (error) => {
      console.error('Failed to create session:', error)
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
    console.log('Creating new session...')
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

  return {
    // State
    currentSessionId,
    message,
    isTyping,
    messagesEndRef,
    
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
    setMessage,
    setCurrentSessionId,
  }
}
