import { type ReactNode, Suspense, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/shared/contexts/AuthContext'
import { useEffectEvent } from 'react'
import { LoadingScreen } from '@/shared/components/ui/LoadingScreen'

interface AuthRouteProps {
  children: ReactNode
  redirectTo?: string
}

export function AuthRoute({ children, redirectTo = '/' }: AuthRouteProps) {
  const { isAuthenticated, loading } = useAuth()
  const navigate = useNavigate()

  const handleRedirect = useEffectEvent((path: string) => {
    navigate(path, { replace: true })
  })

  useEffect(() => {
    if (!loading && isAuthenticated) {
      handleRedirect(redirectTo)
    }
  }, [loading, isAuthenticated, redirectTo, handleRedirect])

  if (loading) {
    return <LoadingScreen label="Loading..." />
  }

  if (isAuthenticated) {
    return null
  }

  return <Suspense fallback={<LoadingScreen label="Loading..." />}>{children}</Suspense>
}
