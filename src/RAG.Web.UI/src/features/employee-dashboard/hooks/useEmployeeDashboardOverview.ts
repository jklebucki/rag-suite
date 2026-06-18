import { useState, useEffect } from 'react'
import { useAuth } from '@/shared/contexts/AuthContext'
import { getEmployeeDashboardData } from '../services/employeeDashboard.mock'
import type { EmployeeDashboardData } from '../types/employeeDashboard'

interface UseEmployeeDashboardOverviewResult {
  data: EmployeeDashboardData | null
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function useEmployeeDashboardOverview(): UseEmployeeDashboardOverviewResult {
  const { user } = useAuth()
  const [data, setData] = useState<EmployeeDashboardData | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [revision, setRevision] = useState(0)

  useEffect(() => {
    if (!user) return

    let cancelled = false
    setIsLoading(true)
    setError(null)

    getEmployeeDashboardData(user.id)
      .then((result) => {
        if (!cancelled) {
          setData(result)
          setIsLoading(false)
        }
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          const message =
            err instanceof Error ? err.message : 'Failed to load dashboard data'
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
