import React, { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '@/contexts/AuthContext'

interface RoleProtectedRouteProps {
  children: ReactNode
  allowedRoles: string[]
  redirectTo?: string
}

export function RoleProtectedRoute({ children, allowedRoles, redirectTo = '/' }: RoleProtectedRouteProps) {
  const { isAuthenticated, user, loading } = useAuth()
  const location = useLocation()

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    )
  }

  if (!isAuthenticated) return <Navigate to="/login" replace state={{ from: location }} />

  const hasRole = allowedRoles.some(r => user?.roles?.includes(r))

  if (!hasRole) return <Navigate to={redirectTo} replace />

  return <>{children}</>
}
