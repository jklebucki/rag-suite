import { useState, useEffect, useCallback } from 'react'

/**
 * Custom hook for monitoring online/offline status
 * Useful for determining when to attempt token refresh or retry requests
 */
export const useOnlineStatus = () => {
  const [isOnline, setIsOnline] = useState(navigator.onLine)
  const [wasOffline, setWasOffline] = useState(false)

  const updateOnlineStatus = useCallback(() => {
    const online = navigator.onLine
    if (!isOnline && online) {
      // Just came back online
      setWasOffline(true)
    }
    setIsOnline(online)
  }, [isOnline])

  const resetOfflineStatus = useCallback(() => {
    setWasOffline(false)
  }, [])

  useEffect(() => {
    window.addEventListener('online', updateOnlineStatus)
    window.addEventListener('offline', updateOnlineStatus)

    return () => {
      window.removeEventListener('online', updateOnlineStatus)
      window.removeEventListener('offline', updateOnlineStatus)
    }
  }, [updateOnlineStatus])

  return {
    isOnline,
    wasOffline,
    resetOfflineStatus
  }
}
