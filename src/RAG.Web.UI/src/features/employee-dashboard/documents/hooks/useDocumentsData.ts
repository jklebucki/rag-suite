import { useEffect, useState } from 'react'
import { useAuth } from '@/shared/contexts/AuthContext'
import { getPit11Documents } from '../services/documentsMockData'
import type { Pit11Document } from '../types/documentsTypes'

interface UseDocumentsDataResult {
  documents: Pit11Document[]
  isLoading: boolean
  error: string | null
}

export function useDocumentsData(): UseDocumentsDataResult {
  const { user } = useAuth()
  const [documents, setDocuments] = useState<Pit11Document[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!user) return

    let cancelled = false
    setIsLoading(true)
    setError(null)

    getPit11Documents(user.id)
      .then((result) => {
        if (cancelled) return

        setDocuments(result)
        setIsLoading(false)
      })
      .catch(() => {
        if (!cancelled) {
          setError('employeeDashboard.pit11.error.loadFailed')
          setIsLoading(false)
        }
      })

    return () => {
      cancelled = true
    }
  }, [user])

  return {
    documents,
    isLoading,
    error,
  }
}
