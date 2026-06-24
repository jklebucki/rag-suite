import { useCallback, useEffect, useState } from 'react'
import { useAuth } from '@/shared/contexts/AuthContext'
import {
  approveApprovalRequest,
  getManagerPanelData,
  rejectApprovalRequest,
  saveApprovalDelegation,
} from '../services/managerMockData'
import type { DelegationPayload, ManagerPanelData } from '../types/managerTypes'

interface UseManagerPanelResult {
  data: ManagerPanelData | null
  isLoading: boolean
  isMutating: boolean
  error: string | null
  approveRequest: (requestId: string) => Promise<void>
  rejectRequest: (requestId: string, reason: string) => Promise<void>
  saveDelegation: (payload: DelegationPayload) => Promise<void>
}

export function useManagerPanel(): UseManagerPanelResult {
  const { user } = useAuth()
  const [data, setData] = useState<ManagerPanelData | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isMutating, setIsMutating] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [revision, setRevision] = useState(0)

  useEffect(() => {
    let cancelled = false
    setIsLoading(true)
    setError(null)

    getManagerPanelData(user?.id ?? 'mock-manager')
      .then((result) => {
        if (!cancelled) {
          setData(result)
          setIsLoading(false)
        }
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Nie udało się załadować panelu')
          setIsLoading(false)
        }
      })

    return () => {
      cancelled = true
    }
  }, [user?.id, revision])

  const approveRequest = useCallback(
    async (requestId: string) => {
      setIsMutating(true)
      try {
        await approveApprovalRequest(user?.id ?? 'mock-manager', requestId)
        setRevision((value) => value + 1)
      } finally {
        setIsMutating(false)
      }
    },
    [user?.id]
  )

  const rejectRequest = useCallback(
    async (requestId: string, reason: string) => {
      setIsMutating(true)
      try {
        await rejectApprovalRequest(user?.id ?? 'mock-manager', requestId, reason)
        setRevision((value) => value + 1)
      } finally {
        setIsMutating(false)
      }
    },
    [user?.id]
  )

  const saveDelegation = useCallback(
    async (payload: DelegationPayload) => {
      setIsMutating(true)
      try {
        await saveApprovalDelegation(user?.id ?? 'mock-manager', payload)
        setRevision((value) => value + 1)
      } finally {
        setIsMutating(false)
      }
    },
    [user?.id]
  )

  return {
    data,
    isLoading,
    isMutating,
    error,
    approveRequest,
    rejectRequest,
    saveDelegation,
  }
}
