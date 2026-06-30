import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { useAuth } from '@/shared/contexts/AuthContext'
import { getEmploymentContexts } from '../services/employmentContext.mock'
import type { EmploymentContextOption } from '../types/employmentContextTypes'

interface EmploymentContextValue {
  contexts: EmploymentContextOption[]
  activeContext: EmploymentContextOption | null
  activeContextId: string | null
  isLoading: boolean
  error: string | null
  setActiveContextId: (contextId: string) => void
}

const EmploymentContext = createContext<EmploymentContextValue | undefined>(undefined)

export function EmploymentContextProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth()
  const [contexts, setContexts] = useState<EmploymentContextOption[]>([])
  const [activeContextId, setActiveContextIdState] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!user) return

    let cancelled = false
    setIsLoading(true)
    setError(null)

    getEmploymentContexts(user.id)
      .then((result) => {
        if (cancelled) return

        setContexts(result)
        setActiveContextIdState((current) => {
          if (current && result.some((context) => context.id === current)) {
            return current
          }

          return result[0]?.id ?? null
        })
        setIsLoading(false)
      })
      .catch(() => {
        if (!cancelled) {
          setError('employeeDashboard.employmentContext.error.loadFailed')
          setIsLoading(false)
        }
      })

    return () => {
      cancelled = true
    }
  }, [user])

  const activeContext = useMemo(
    () => contexts.find((context) => context.id === activeContextId) ?? contexts[0] ?? null,
    [activeContextId, contexts]
  )

  const setActiveContextId = useCallback((contextId: string) => {
    setActiveContextIdState(contextId)
  }, [])

  const value = useMemo<EmploymentContextValue>(
    () => ({
      contexts,
      activeContext,
      activeContextId,
      isLoading,
      error,
      setActiveContextId,
    }),
    [activeContext, activeContextId, contexts, error, isLoading, setActiveContextId]
  )

  return (
    <EmploymentContext.Provider value={value}>
      {children}
    </EmploymentContext.Provider>
  )
}

export function useEmploymentContext(): EmploymentContextValue {
  const context = useContext(EmploymentContext)

  if (!context) {
    throw new Error('useEmploymentContext must be used within EmploymentContextProvider')
  }

  return context
}
