import { type ReactNode, Suspense, useEffect, useMemo } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useEffectEvent } from 'react'
import { useAuth } from '@/shared/contexts/AuthContext'
import { LoadingScreen } from '@/shared/components/ui/LoadingScreen'

interface RoleProtectedRouteProps {
  children: ReactNode
  allowedRoles: string[]
  redirectTo?: string
}

export function RoleProtectedRoute({ children, allowedRoles, redirectTo = '/' }: RoleProtectedRouteProps) {
  const { isAuthenticated, user, loading } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const hasRole = useMemo(() => allowedRoles.some(r => user?.roles?.includes(r)), [allowedRoles, user?.roles])

  const redirectToLogin = useEffectEvent(() => {
    navigate('/login', {
      replace: true,
      state: {
        from: {
          pathname: location.pathname,
          search: location.search,
          hash: location.hash,
        },
      },
    })
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
    if (!loading && isAuthenticated && !hasRole) {
      redirectAway(redirectTo)
    }
  }, [loading, isAuthenticated, hasRole, redirectTo, redirectAway])

  if (loading) {
    return <LoadingScreen label="Checking permissions..." />
  }

  if (!isAuthenticated) {
    return null
  }

  if (!hasRole) {
    return null
  }

  return <Suspense fallback={<LoadingScreen label="Loading..." />}>{children}</Suspense>
}
