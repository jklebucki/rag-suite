import { type ReactNode, Suspense, useEffect } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useEffectEvent } from 'react'
import { useAuth } from '@/shared/contexts/AuthContext'
import { LoadingScreen } from '@/shared/components/ui/LoadingScreen'

interface ProtectedRouteProps {
  children: ReactNode
  redirectTo?: string
}

export function ProtectedRoute({ children, redirectTo = '/login' }: ProtectedRouteProps) {
  const { isAuthenticated, loading } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()

  const handleNavigate = useEffectEvent((path: string) => {
    navigate(path, { replace: true, state: { from: location } })
  })

  useEffect(() => {
    if (!loading && !isAuthenticated) {
      handleNavigate(redirectTo)
    }
  }, [loading, isAuthenticated, redirectTo, handleNavigate])

  if (loading) {
    return <LoadingScreen label="Checking access..." />
  }

  if (!isAuthenticated) {
    return null
  }

  return <Suspense fallback={<LoadingScreen label="Loading..." />}>{children}</Suspense>
}
