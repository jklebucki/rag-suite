import { useEffect, useState } from 'react'
import { getHrAbsenceRequests } from '../services/hrAbsenceMockData'
import type { HrAbsenceRequest } from '../types/hrAbsenceTypes'

export function useHrAbsenceRequests() {
  const [requests, setRequests] = useState<HrAbsenceRequest[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<'error.loadFailed' | null>(null)

  useEffect(() => {
    let cancelled = false

    getHrAbsenceRequests()
      .then((result) => {
        if (!cancelled) setRequests(result)
      })
      .catch(() => {
        if (!cancelled) setError('error.loadFailed')
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [])

  return { requests, isLoading, error }
}
