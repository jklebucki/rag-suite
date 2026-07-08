import { useState, useRef, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import chatService from '@/features/chat/services/chat.service'
import { useToastContext } from '@/shared/contexts/ToastContext'
import { useI18n } from '@/shared/contexts/I18nContext'
import { logger } from '@/utils/logger'
import type {
  MultilingualChatRequest,
  MultilingualChatResponse,
  ChatSession,
  ChatMessage,
  ChatContextUsage,
} from '@/features/chat/types/chat'

export function useMultilingualChat() {
  const [currentSessionId, setCurrentSessionId] = useState<string | null>(null)
  const [message, setMessage] = useState('')
  const [isTyping, setIsTyping] = useState(false)
  const [sessionToDelete, setSessionToDelete] = useState<string | null>(null)
  const [lastResponse, setLastResponse] = useState<MultilingualChatResponse | null>(null)
  const [documentsAvailable, setDocumentsAvailable] = useState<boolean>(true)
  const [useDocumentSearch, setUseDocumentSearch] = useState<boolean>(false)
  const [isNewSession, setIsNewSession] = useState<boolean>(false)
  const [pendingMessages, setPendingMessages] = useState<ChatMessage[]>([])
  const messagesEndRef = useRef<HTMLDivElement>(null)
  const queryClient = useQueryClient()
  const { showError, showSuccess } = useToastContext()
  const { language: currentLanguage } = useI18n()

  // Get chat sessions (reuse existing endpoint)
  const { data: sessions = [] } = useQuery({
    queryKey: ['chat-sessions'],
    queryFn: ({ signal }) => chatService.getChatSessions({ signal }),
  })

  // Get current session messages
  const { data: currentSession } = useQuery({
    queryKey: ['chat-session', currentSessionId],
    queryFn: ({ signal }) => (currentSessionId ? chatService.getChatSession(currentSessionId, { signal }) : null),
    enabled: !!currentSessionId,
  })

  const { data: contextUsage = null } = useQuery<ChatContextUsage | null>({
    queryKey: ['chat-context', currentSessionId],
    queryFn: ({ signal }) => (currentSessionId ? chatService.getChatContext(currentSessionId, { signal }) : null),
    enabled: !!currentSessionId,
  })

  // Merge server messages with pending messages for display
  const displaySession = currentSession
    ? {
        ...currentSession,
        messages: [
          ...(currentSession.messages || []),
          ...pendingMessages.filter(
            pending => !currentSession.messages.some(msg => msg.id === pending.id)
          )
        ]
      }
    : currentSession

  // Send multilingual message mutation
  const sendMultilingualMessageMutation = useMutation({
    mutationFn: ({ sessionId, request }: { sessionId: string; request: MultilingualChatRequest }) => {
      logger.debug('Calling sendMultilingualMessage API with:', { sessionId, request })
      return chatService.sendMultilingualMessage(sessionId, request)
    },
    retry: false,
    onSuccess: (response) => {
      logger.debug('sendMultilingualMessage success:', response)
      setLastResponse(response)

      // Check if documents are available from metadata
      if (response.metadata && response.metadata.documentsAvailable !== undefined) {
        setDocumentsAvailable(response.metadata.documentsAvailable as boolean)
      }

      // Clear pending messages - server now has the real ones
      setPendingMessages([])

      // Refresh queries to get updated messages from server
      queryClient.invalidateQueries({ queryKey: ['chat-session', currentSessionId] })
      queryClient.invalidateQueries({ queryKey: ['chat-sessions'] })
      queryClient.invalidateQueries({ queryKey: ['chat-context', currentSessionId] })

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
      logger.error('Failed to send multilingual message:', error)
      showError('Failed to send message', extractApiErrorMessage(error, 'Please check your connection and try again'))
      setIsTyping(false)
      // Clear pending messages on error
      setPendingMessages([])
      queryClient.invalidateQueries({ queryKey: ['chat-session', currentSessionId] })
      queryClient.invalidateQueries({ queryKey: ['chat-context', currentSessionId] })
    },
  })

  const uploadAttachmentsMutation = useMutation({
    mutationFn: ({ sessionId, files }: { sessionId: string; files: File[] }) =>
      chatService.uploadChatAttachments(sessionId, files),
    onSuccess: (response, variables) => {
      queryClient.setQueryData(['chat-context', variables.sessionId], response.contextUsage)
    },
    onError: (error) => {
      logger.error('Failed to upload chat attachments:', error)
      showError('Attachment not added', extractApiErrorMessage(error, 'The selected file could not be attached.'))
      queryClient.invalidateQueries({ queryKey: ['chat-context', currentSessionId] })
    },
  })

  const deleteAttachmentMutation = useMutation({
    mutationFn: ({ sessionId, attachmentId }: { sessionId: string; attachmentId: string }) =>
      chatService.deleteChatAttachment(sessionId, attachmentId),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['chat-context', variables.sessionId] })
    },
    onError: (error) => {
      logger.error('Failed to delete chat attachment:', error)
      showError('Attachment not removed', extractApiErrorMessage(error, 'Please try again.'))
    },
  })

  // Create new session mutation (reuse existing endpoint)
  const createSessionMutation = useMutation({
    mutationFn: ({ title, language }: { title?: string; language?: string }) =>
      chatService.createChatSession(title, language),
    onSuccess: (newSession) => {
      logger.debug('Session created:', newSession)
      // Immediately add the new session to the cache for instant UI update
      queryClient.setQueryData(['chat-sessions'], (old: ChatSession[] = []) => [...old, newSession])
      queryClient.invalidateQueries({ queryKey: ['chat-sessions'] })
      setCurrentSessionId(newSession.id)
      setIsNewSession(true)
    },
    onError: (error) => {
      logger.error('Failed to create session:', error)
      showError('Failed to create new session', 'Please try again')
    },
  })

  // Delete session mutation (reuse existing endpoint)
  const deleteSessionMutation = useMutation({
    mutationFn: (sessionId: string) => chatService.deleteChatSession(sessionId),
    onSuccess: (_, deletedSessionId) => {
      queryClient.invalidateQueries({ queryKey: ['chat-sessions'] })
      setSessionToDelete(null)
      // Clear current session if it was the one that got deleted
      if (currentSessionId === deletedSessionId) {
        setCurrentSessionId(null)
        // Also clear the current session data from cache
        queryClient.setQueryData(['chat-session', deletedSessionId], null)
        queryClient.setQueryData(['chat-context', deletedSessionId], null)
      }
      showSuccess('Session deleted', 'Chat session has been deleted successfully')
    },
    onError: (error) => {
      logger.error('Failed to delete session:', error)
      showError('Failed to delete session', 'Please try again')
      setSessionToDelete(null)
    },
  })

  const handleSendMessage = async (e?: React.FormEvent) => {
    if (e) {
      e.preventDefault()
    }

    logger.debug('handleSendMessage called', { message: message.trim(), currentSessionId, isTyping })

    if (!message.trim()) {
      logger.debug('Message is empty, aborting')
      return
    }

    if (!currentSessionId) {
      logger.debug('No current session, aborting')
      return
    }

    if (contextUsage?.isLimitExceeded) {
      showError('Context limit reached', 'Start a new chat to continue the conversation.')
      return
    }

    // 🆕 Prevent double sending when already sending
    if (isTyping || sendMultilingualMessageMutation.isPending) {
      logger.debug('Already sending message, aborting')
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

    // Add to pending messages immediately for display
    setPendingMessages(prev => [...prev, tempUserMessage])

    logger.debug('Setting isTyping to true')
    setIsTyping(true)
    setMessage('') // Clear input immediately

    const request: MultilingualChatRequest = {
      message: userMessage,
      language: currentLanguage,
      responseLanguage: currentLanguage,
      enableTranslation: true,
      useDocumentSearch: useDocumentSearch,
      attachmentIds: (contextUsage?.attachments ?? []).map(attachment => attachment.id),
      metadata: {
        uiLanguage: currentLanguage,
        timestamp: messageTimestamp.toISOString()
      }
    }

    logger.debug('Sending multilingual message:', request)
    sendMultilingualMessageMutation.mutate({
      sessionId: currentSessionId,
      request
    })
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

  const handleAttachFiles = (files: File[]) => {
    if (!currentSessionId || files.length === 0 || contextUsage?.isLimitExceeded) {
      return
    }

    uploadAttachmentsMutation.mutate({
      sessionId: currentSessionId,
      files,
    })
  }

  const handleRemoveAttachment = (attachmentId: string) => {
    if (!currentSessionId) {
      return
    }

    deleteAttachmentMutation.mutate({
      sessionId: currentSessionId,
      attachmentId,
    })
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
  }, [displaySession?.messages])

  // Set first session as current if none selected
  useEffect(() => {
    logger.debug('Session selection effect:', { currentSessionId, sessionsLength: sessions.length, sessions })
    if (!currentSessionId && sessions.length > 0) {
      logger.debug('Setting first session as current:', sessions[0].id)
      setCurrentSessionId(sessions[0].id)
    }
  }, [sessions, currentSessionId])

  // Clear current session if it no longer exists in the sessions list
  useEffect(() => {
    if (currentSessionId && sessions.length > 0) {
      const sessionExists = sessions.some(session => session.id === currentSessionId)
      if (!sessionExists) {
        logger.debug('Current session no longer exists, clearing:', currentSessionId)
        setCurrentSessionId(null)
        // Clear the session data from cache
        queryClient.setQueryData(['chat-session', currentSessionId], null)
      }
    }
  }, [sessions, currentSessionId, queryClient])

  // Cleanup on unmount - reset state
  useEffect(() => {
    return () => {
      logger.debug('useMultilingualChat cleanup: resetting state')
      setCurrentSessionId(null)
      setMessage('')
      setIsTyping(false)
      setSessionToDelete(null)
      setLastResponse(null)
      setUseDocumentSearch(false)
      setIsNewSession(false)
    }
  }, [])

  return {
    // State
    currentSessionId,
    setCurrentSessionId,
    message,
    setMessage,
    isTyping,
    sessions,
    currentSession: displaySession,
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
    contextUsage,
    attachments: contextUsage?.attachments ?? [],

    // Mutations for compatibility with ChatInterface
    sendMessageMutation: sendMultilingualMessageMutation,
    createSessionMutation,
    deleteSessionMutation,
    uploadAttachmentsMutation,
    deleteAttachmentMutation,

    // Actions
    handleSendMessage,
    handleCreateSession,
    handleNewSession,
    handleDeleteSession,
    handleAttachFiles,
    handleRemoveAttachment,
    confirmDeleteSession,
    cancelDeleteSession,
    handleKeyPress,
    setUseDocumentSearch,

    // Loading states
    isCreatingSession: createSessionMutation.isPending,
    isDeletingSession: deleteSessionMutation.isPending,
    isSendingMessage: sendMultilingualMessageMutation.isPending,
    isUploadingAttachments: uploadAttachmentsMutation.isPending,
    isRemovingAttachment: deleteAttachmentMutation.isPending,
  }
}

function extractApiErrorMessage(error: unknown, fallback: string): string {
  const maybeResponse = error as { response?: { data?: { message?: unknown; errors?: unknown } } }
  const message = maybeResponse.response?.data?.message
  if (typeof message === 'string' && message.trim()) {
    return message
  }

  const errors = maybeResponse.response?.data?.errors
  if (Array.isArray(errors) && errors.length > 0 && typeof errors[0] === 'string') {
    return errors[0]
  }

  return fallback
}
