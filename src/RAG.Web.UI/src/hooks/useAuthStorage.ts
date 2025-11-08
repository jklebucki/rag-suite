import { useEffect, useCallback } from 'react'
import { authService } from '@/services/auth'
import { logger } from '@/utils/logger'
import { STORAGE_KEYS } from '@/constants/config'
import type { User } from '@/types/auth'

/**
 * Custom hook for handling authentication storage synchronization
 * and cross-tab communication
 */
export const useAuthStorage = (
  onLogin: (token: string, refreshToken: string | null, user: User) => void,
  onLogout: () => void
) => {
  // Handle storage events for cross-tab synchronization
  const handleStorageChange = useCallback((e: StorageEvent) => {
    if (e.key === STORAGE_KEYS.AUTH_TOKEN) {
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
        logger.warn('Failed to verify auth on tab focus:', error)
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
  const storeAuthData = useCallback((token: string, refreshToken: string | null, user: User) => {
    logger.debug('storeAuthData called with:', { hasToken: !!token, hasRefreshToken: !!refreshToken, hasUser: !!user })
    
    try {
      localStorage.setItem(STORAGE_KEYS.AUTH_TOKEN, token)
      if (refreshToken) {
        localStorage.setItem(STORAGE_KEYS.REFRESH_TOKEN, refreshToken)
      } else {
        localStorage.removeItem(STORAGE_KEYS.REFRESH_TOKEN)
      }
      localStorage.setItem(STORAGE_KEYS.USER, JSON.stringify(user))
      
      logger.debug('Data stored in localStorage successfully')
      
      // Dispatch custom event for same-tab updates
      window.dispatchEvent(new CustomEvent('authStateChanged', {
        detail: { type: 'login', token, refreshToken, user }
      }))
      
      // Verify storage worked
      logger.debug('Verification - localStorage now contains:', {
        token: localStorage.getItem(STORAGE_KEYS.AUTH_TOKEN) ? 'EXISTS' : 'MISSING',
        refreshToken: localStorage.getItem(STORAGE_KEYS.REFRESH_TOKEN) ? 'EXISTS' : 'MISSING',
        userData: localStorage.getItem(STORAGE_KEYS.USER) ? 'EXISTS' : 'MISSING'
      })
    } catch (error) {
      logger.error('Failed to store auth data:', error)
      throw new Error('Failed to persist authentication data')
    }
  }, [])

  // Function to safely clear auth data
  const clearAuthData = useCallback(() => {
    try {
      localStorage.removeItem(STORAGE_KEYS.AUTH_TOKEN)
      localStorage.removeItem(STORAGE_KEYS.REFRESH_TOKEN)
      localStorage.removeItem(STORAGE_KEYS.USER)
      
      // Dispatch custom event for same-tab updates
      window.dispatchEvent(new CustomEvent('authStateChanged', {
        detail: { type: 'logout' }
      }))
    } catch (error) {
      logger.error('Failed to clear auth data:', error)
    }
  }, [])

  return {
    storeAuthData,
    clearAuthData
  }
}
