import { type ReactNode, Suspense, useEffect } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '@/shared/contexts/AuthContext'
import { useEffectEvent } from 'react'
import { LoadingScreen } from '@/shared/components/ui/LoadingScreen'

interface AuthRouteProps {
  children: ReactNode
  redirectTo?: string
}

export function AuthRoute({ children, redirectTo = '/' }: AuthRouteProps) {
  const { isAuthenticated, loading } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()

  const handleRedirect = useEffectEvent((path: string) => {
    navigate(path, { replace: true })
  })

  useEffect(() => {
    if (!loading && isAuthenticated) {
      const redirectState = (location.state as { from?: { pathname?: string; search?: string; hash?: string } } | null)?.from
      if (redirectState?.pathname) {
        const { pathname, search = '', hash = '' } = redirectState
        handleRedirect(`${pathname}${search}${hash}`)
        return
      }

      handleRedirect(redirectTo)
    }
  }, [loading, isAuthenticated, redirectTo, location.state, handleRedirect])

  if (loading) {
    return <LoadingScreen label="Loading..." />
  }

  if (isAuthenticated) {
    return null
  }

  return <Suspense fallback={<LoadingScreen label="Loading..." />}>{children}</Suspense>
}
