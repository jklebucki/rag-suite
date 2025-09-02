import { useEffect, useCallback } from 'react'
import { authService } from '@/services/auth'

/**
 * Custom hook for handling authentication storage synchronization
 * and cross-tab communication
 */
export const useAuthStorage = (
  onLogin: (token: string, refreshToken: string | null, user: any) => void,
  onLogout: () => void
) => {
  // Handle storage events for cross-tab synchronization
  const handleStorageChange = useCallback((e: StorageEvent) => {
    if (e.key === 'auth_token') {
      if (e.newValue === null) {
        // Token was removed in another tab
        onLogout()
      } else if (e.newValue) {
        // Token was added/updated in another tab
        const refreshToken = authService.getRefreshToken()
        const user = authService.getUser()
        if (user) {
          onLogin(e.newValue, refreshToken, user)
        }
      }
    }
  }, [onLogin, onLogout])

  // Handle visibility change to refresh auth state when tab becomes active
  const handleVisibilityChange = useCallback(async () => {
    if (!document.hidden && authService.isAuthenticated()) {
      // Tab became visible, check if token is still valid
      try {
        const user = await authService.getCurrentUser()
        if (!user) {
          // Token invalid, logout
          onLogout()
        }
      } catch (error) {
        console.warn('Failed to verify auth on tab focus:', error)
        // Optionally logout on error, depending on your security requirements
        // onLogout()
      }
    }
  }, [onLogout])

  useEffect(() => {
    // Listen for storage changes
    window.addEventListener('storage', handleStorageChange)
    
    // Listen for visibility changes
    document.addEventListener('visibilitychange', handleVisibilityChange)

    return () => {
      window.removeEventListener('storage', handleStorageChange)
      document.removeEventListener('visibilitychange', handleVisibilityChange)
    }
  }, [handleStorageChange, handleVisibilityChange])

  // Function to safely store auth data
  const storeAuthData = useCallback((token: string, refreshToken: string, user: any) => {
    try {
      localStorage.setItem('auth_token', token)
      localStorage.setItem('refresh_token', refreshToken)
      localStorage.setItem('user_data', JSON.stringify(user))
      
      // Dispatch custom event for same-tab updates
      window.dispatchEvent(new CustomEvent('authStateChanged', {
        detail: { type: 'login', token, refreshToken, user }
      }))
    } catch (error) {
      console.error('Failed to store auth data:', error)
      throw new Error('Failed to persist authentication data')
    }
  }, [])

  // Function to safely clear auth data
  const clearAuthData = useCallback(() => {
    try {
      localStorage.removeItem('auth_token')
      localStorage.removeItem('refresh_token')
      localStorage.removeItem('user_data')
      
      // Dispatch custom event for same-tab updates
      window.dispatchEvent(new CustomEvent('authStateChanged', {
        detail: { type: 'logout' }
      }))
    } catch (error) {
      console.error('Failed to clear auth data:', error)
    }
  }, [])

  return {
    storeAuthData,
    clearAuthData
  }
}
