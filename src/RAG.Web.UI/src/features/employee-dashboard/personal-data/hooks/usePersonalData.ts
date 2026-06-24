import { useState, useEffect } from 'react'
import { useAuth } from '@/shared/contexts/AuthContext'
import { getPersonalDataPageData } from '../services/personalData.mock'
import type { PersonalDataPageData } from '../types/personalData'

interface UsePersonalDataResult {
  data: PersonalDataPageData | null
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function usePersonalData(): UsePersonalDataResult {
  const { user } = useAuth()
  const [data, setData] = useState<PersonalDataPageData | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [revision, setRevision] = useState(0)

  useEffect(() => {
    if (!user) return

    let cancelled = false
    setIsLoading(true)
    setError(null)

    getPersonalDataPageData(user.id)
      .then((result) => {
        if (!cancelled) {
          setData(result)
          setIsLoading(false)
        }
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          const message =
            err instanceof Error ? err.message : 'Failed to load personal data'
          setError(message)
          setIsLoading(false)
        }
      })

    return () => {
      cancelled = true
    }
  }, [user, revision])

  const refetch = () => setRevision((v) => v + 1)

  return { data, isLoading, error, refetch }
}
