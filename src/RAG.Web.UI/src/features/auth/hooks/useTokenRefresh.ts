import { useEffect, useCallback, useRef } from 'react'
import { authService } from '@/features/auth/services/auth.service'
import { useOnlineStatus } from '@/shared/hooks/useOnlineStatus'
import { logger } from '@/utils/logger'

let refreshInFlight: Promise<void> | null = null

/**
 * Custom hook for automatic token refresh management
 */
export const useTokenRefresh = (
  isAuthenticated: boolean,
  onTokenRefresh: (token: string, refreshToken: string) => void,
  onLogout: () => void,
  onRefreshError?: () => void
) => {
  const refreshTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)
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
      logger.warn('Failed to parse token for refresh calculation:', error)
      return 0
    }
  }, [])

  // Perform token refresh
  const performTokenRefresh = useCallback(async () => {
    if (refreshInFlight) {
      logger.debug('Token refresh already in progress, joining existing promise')
      await refreshInFlight
      return
    }

    const now = Date.now()

    if (!isOnline) {
      logger.debug('Token refresh skipped (offline)')
      return
    }

    if (now - lastRefreshAttemptRef.current < MIN_REFRESH_INTERVAL) {
      logger.debug('Token refresh skipped (too soon since last attempt)')
      return
    }

    lastRefreshAttemptRef.current = now
    logger.debug('Starting token refresh...')

    const refreshPromise = (async () => {
      isRefreshingRef.current = true

      try {
        const success = await authService.refreshToken()
        if (success) {
          const newToken = authService.getToken()
          const newRefreshToken = authService.getRefreshToken()

          if (newToken && newRefreshToken) {
            onTokenRefresh(newToken, newRefreshToken)
            logger.debug('Token refreshed successfully')
          } else {
            logger.warn('Token refresh succeeded but tokens are missing')
            onRefreshError?.()
            onLogout()
          }
        } else {
          logger.warn('Token refresh failed, logging out')
          onRefreshError?.()
          onLogout()
        }
      } catch (error) {
        logger.error('Token refresh error:', error)
        onRefreshError?.()
        onLogout()
      } finally {
        isRefreshingRef.current = false
        logger.debug('Token refresh process completed')
      }
    })()

    refreshInFlight = refreshPromise.finally(() => {
      refreshInFlight = null
    })

    await refreshInFlight
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
      logger.debug('Token expired/expiring, scheduling refresh in 1 minute to avoid loops')
      
      refreshTimeoutRef.current = setTimeout(() => {
        logger.debug('Delayed token refresh triggered for expired token')
        performTokenRefresh()
      }, delayTime)
      return
    } else {
      // Schedule refresh
      refreshTimeoutRef.current = setTimeout(() => {
        logger.debug('Scheduled token refresh triggered')
        performTokenRefresh()
      }, timeUntilRefresh)
      
      logger.debug(`Token refresh scheduled in ${Math.round(timeUntilRefresh / 1000)} seconds`)
    }
  }, [isAuthenticated, getTimeUntilRefresh, performTokenRefresh])

  // Setup automatic token refresh
  useEffect(() => {
    if (isAuthenticated) {
      logger.debug('Setting up automatic token refresh with safeguards')
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
          logger.debug('Page visible and token expiring soon, attempting refresh')
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
      logger.debug('Just came back online, checking token status')
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
