import { useEffect, useCallback, useRef } from 'react'
import { authService } from '@/services/auth'
import { useOnlineStatus } from './useOnlineStatus'

/**
 * Custom hook for automatic token refresh management
 */
export const useTokenRefresh = (
  isAuthenticated: boolean,
  onTokenRefresh: (token: string, refreshToken: string) => void,
  onLogout: () => void,
  onRefreshError?: () => void
) => {
  const refreshTimeoutRef = useRef<NodeJS.Timeout | null>(null)
  const isRefreshingRef = useRef(false)
  const lastRefreshAttemptRef = useRef<number>(0)
  const { isOnline, wasOffline, resetOfflineStatus } = useOnlineStatus()

  // Minimum time between refresh attempts (5 minutes)
  const MIN_REFRESH_INTERVAL = 5 * 60 * 1000

  // Calculate time until token refresh is needed
  const getTimeUntilRefresh = useCallback((): number => {
    const token = authService.getToken()
    if (!token) return 0

    try {
      const payload = JSON.parse(atob(token.split('.')[1]))
      const expiryTime = payload.exp * 1000
      const now = Date.now()
      
      // Refresh when 80% of token lifetime has passed or 5 minutes before expiry
      const refreshTime = Math.min(
        expiryTime - (5 * 60 * 1000), // 5 minutes before expiry
        now + ((expiryTime - now) * 0.8) // 80% of remaining time
      )
      
      return Math.max(0, refreshTime - now)
    } catch (error) {
      console.warn('Failed to parse token for refresh calculation:', error)
      return 0
    }
  }, [])

  // Perform token refresh
  const performTokenRefresh = useCallback(async () => {
    const now = Date.now()
    
    // Check if enough time has passed since last attempt
    if (now - lastRefreshAttemptRef.current < MIN_REFRESH_INTERVAL) {
      console.debug('🔄 Token refresh skipped (too soon since last attempt)')
      return
    }
    
    if (isRefreshingRef.current || !isOnline) {
      console.debug('🔄 Token refresh skipped (already refreshing or offline)')
      return
    }
    
    isRefreshingRef.current = true
    lastRefreshAttemptRef.current = now
    console.debug('🔄 Starting token refresh...')
    
    try {
      const success = await authService.refreshToken()
      if (success) {
        const newToken = authService.getToken()
        const newRefreshToken = authService.getRefreshToken()
        
        if (newToken && newRefreshToken) {
          onTokenRefresh(newToken, newRefreshToken)
          console.debug('🔄 Token refreshed successfully')
        } else {
          console.warn('🔄 Token refresh succeeded but tokens are missing')
          onRefreshError?.()
          onLogout()
        }
      } else {
        console.warn('🔄 Token refresh failed, logging out')
        onRefreshError?.()
        onLogout()
      }
    } catch (error) {
      console.error('🔄 Token refresh error:', error)
      onRefreshError?.()
      onLogout()
    } finally {
      isRefreshingRef.current = false
      console.debug('🔄 Token refresh process completed')
    }
  }, [onTokenRefresh, onLogout, onRefreshError, isOnline, MIN_REFRESH_INTERVAL])

  // Schedule next token refresh
  const scheduleTokenRefresh = useCallback(() => {
    if (refreshTimeoutRef.current) {
      clearTimeout(refreshTimeoutRef.current)
    }

    if (!isAuthenticated) return

    const timeUntilRefresh = getTimeUntilRefresh()
    
    if (timeUntilRefresh <= 0) {
      // Token expired or expiring soon - schedule in 1 minute to avoid immediate loop
      const delayTime = 60 * 1000 // 1 minute
      console.debug('🔄 Token expired/expiring, scheduling refresh in 1 minute to avoid loops')
      
      refreshTimeoutRef.current = setTimeout(() => {
        console.debug('🔄 Delayed token refresh triggered for expired token')
        performTokenRefresh()
      }, delayTime)
      return
    } else {
      // Schedule refresh
      refreshTimeoutRef.current = setTimeout(() => {
        console.debug('🔄 Scheduled token refresh triggered')
        performTokenRefresh()
      }, timeUntilRefresh)
      
      console.debug(`🔄 Token refresh scheduled in ${Math.round(timeUntilRefresh / 1000)} seconds`)
    }
  }, [isAuthenticated, getTimeUntilRefresh, performTokenRefresh])

  // Setup automatic token refresh
  useEffect(() => {
    if (isAuthenticated) {
      console.debug('🔄 Setting up automatic token refresh with safeguards')
      scheduleTokenRefresh()
    } else {
      if (refreshTimeoutRef.current) {
        clearTimeout(refreshTimeoutRef.current)
        refreshTimeoutRef.current = null
      }
    }

    return () => {
      if (refreshTimeoutRef.current) {
        clearTimeout(refreshTimeoutRef.current)
      }
    }
  }, [isAuthenticated, scheduleTokenRefresh])

  // Handle page visibility changes
  useEffect(() => {
    const handleVisibilityChange = () => {
      if (!document.hidden && isAuthenticated) {
        // Page became visible, check if we need to refresh token
        if (authService.isTokenExpiringSoon()) {
          console.debug('🔄 Page visible and token expiring soon, attempting refresh')
          performTokenRefresh()
        } else {
          // Reschedule refresh based on current token
          scheduleTokenRefresh()
        }
      }
    }

    document.addEventListener('visibilitychange', handleVisibilityChange)
    return () => document.removeEventListener('visibilitychange', handleVisibilityChange)
  }, [isAuthenticated, performTokenRefresh, scheduleTokenRefresh])

  // Handle coming back online
  useEffect(() => {
    if (wasOffline && isOnline && isAuthenticated) {
      console.debug('🔄 Just came back online, checking token status')
      // Just came back online, check if token needs refresh
      if (authService.isTokenExpiringSoon()) {
        performTokenRefresh()
      }
      resetOfflineStatus()
    }
  }, [wasOffline, isOnline, isAuthenticated, performTokenRefresh, resetOfflineStatus])

  return {
    performTokenRefresh,
    scheduleTokenRefresh
  }
}
