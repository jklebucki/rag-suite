import { createScopedLogger } from '@/utils/logger'

const log = createScopedLogger('debugAuth')

// Utility function to test localStorage and auth data
export const debugAuth = () => {
  log.info('🔍 Auth Debug Information')
  
  try {
    log.info('LocalStorage available:', typeof Storage !== 'undefined')
    log.info('auth_token:', localStorage.getItem('auth_token'))
    log.info('refresh_token:', localStorage.getItem('refresh_token'))
    log.info('user_data:', localStorage.getItem('user_data'))
    
    const token = localStorage.getItem('auth_token')
    if (token) {
      try {
        const payload = JSON.parse(atob(token.split('.')[1]))
        log.info('Token payload:', payload)
        log.info('Token expires:', new Date(payload.exp * 1000))
        log.info('Token valid:', payload.exp * 1000 > Date.now())
      } catch (e) {
        log.warn('Token parse error:', e)
      }
    }
  } catch (error) {
    log.error('Debug error:', error)
  }
}

// Add to window object for easy access in browser console
if (typeof window !== 'undefined') {
  (window as typeof window & { debugAuth?: typeof debugAuth }).debugAuth = debugAuth
}
