import { useState, useEffect, useCallback } from 'react'
import { useAuth } from '@/shared/contexts/AuthContext'
import {
  getLeaveRequestPageData,
  createLeaveRequest,
  cancelLeaveRequest,
} from '../services/leaveRequest.mock'
import type {
  LeaveRequestPageData,
  CreateLeaveRequestPayload,
} from '../types/leaveRequest'

interface UseLeaveRequestResult {
  data: LeaveRequestPageData | null
  isLoading: boolean
  isSubmitting: boolean
  isCancelling: boolean
  error: string | null
  submitRequest: (payload: CreateLeaveRequestPayload) => Promise<void>
  cancelRequest: (requestId: string) => Promise<void>
  refetch: () => void
}

export function useLeaveRequest(): UseLeaveRequestResult {
  const { user } = useAuth()
  const [data, setData] = useState<LeaveRequestPageData | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isCancelling, setIsCancelling] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [revision, setRevision] = useState(0)

  useEffect(() => {
    if (!user) return

    let cancelled = false
    setIsLoading(true)
    setError(null)

    getLeaveRequestPageData(user.id)
      .then((result) => {
        if (!cancelled) {
          setData(result)
          setIsLoading(false)
        }
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          const message =
            err instanceof Error ? err.message : 'Failed to load leave request data'
          setError(message)
          setIsLoading(false)
        }
      })

    return () => {
      cancelled = true
    }
  }, [user, revision])

  const submitRequest = useCallback(
    async (payload: CreateLeaveRequestPayload) => {
      if (!user) return
      setIsSubmitting(true)
      try {
        await createLeaveRequest(user.id, payload)
        setRevision((v) => v + 1)
      } finally {
        setIsSubmitting(false)
      }
    },
    [user]
  )

  const cancelRequest = useCallback(
    async (requestId: string) => {
      if (!user) return
      setIsCancelling(true)
      try {
        await cancelLeaveRequest(user.id, requestId)
        setRevision((v) => v + 1)
      } finally {
        setIsCancelling(false)
      }
    },
    [user]
  )

  const refetch = () => setRevision((v) => v + 1)

  return {
    data,
    isLoading,
    isSubmitting,
    isCancelling,
    error,
    submitRequest,
    cancelRequest,
    refetch,
  }
}
