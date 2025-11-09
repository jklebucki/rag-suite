import { type ReactNode, Suspense, useEffect, useMemo } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useEffectEvent } from 'react'
import { useAuth } from '@/shared/contexts/AuthContext'
import { LoadingScreen } from '@/shared/components/ui/LoadingScreen'

interface AdminProtectedRouteProps {
  children: ReactNode
  redirectTo?: string
}

export function AdminProtectedRoute({ children, redirectTo = '/' }: AdminProtectedRouteProps) {
  const { isAuthenticated, user, loading } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const isAdmin = useMemo(() => user?.roles?.includes('Admin') ?? false, [user?.roles])

  const redirectToLogin = useEffectEvent(() => {
    navigate('/login', { replace: true, state: { from: location } })
  })

  const redirectAway = useEffectEvent((path: string) => {
    navigate(path, { replace: true })
  })

  useEffect(() => {
    if (!loading && !isAuthenticated) {
      redirectToLogin()
    }
  }, [loading, isAuthenticated, redirectToLogin])

  useEffect(() => {
    if (!loading && isAuthenticated && !isAdmin) {
      redirectAway(redirectTo)
    }
  }, [loading, isAuthenticated, isAdmin, redirectTo, redirectAway])

  if (loading) {
    return <LoadingScreen label="Checking admin access..." />
  }

  if (!isAuthenticated) {
    return null
  }

  if (!isAdmin) {
    return null
  }

  return <Suspense fallback={<LoadingScreen label="Loading..." />}>{children}</Suspense>
}
