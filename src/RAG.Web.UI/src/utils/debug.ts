// Utility function to test localStorage and auth data
export const debugAuth = () => {
  console.group('🔍 Auth Debug Information')
  
  try {
    console.log('LocalStorage available:', typeof Storage !== 'undefined')
    console.log('auth_token:', localStorage.getItem('auth_token'))
    console.log('refresh_token:', localStorage.getItem('refresh_token'))
    console.log('user_data:', localStorage.getItem('user_data'))
    
    const token = localStorage.getItem('auth_token')
    if (token) {
      try {
        const payload = JSON.parse(atob(token.split('.')[1]))
        console.log('Token payload:', payload)
        console.log('Token expires:', new Date(payload.exp * 1000))
        console.log('Token valid:', payload.exp * 1000 > Date.now())
      } catch (e) {
        console.log('Token parse error:', e)
      }
    }
  } catch (error) {
    console.error('Debug error:', error)
  }
  
  console.groupEnd()
}

// Add to window object for easy access in browser console
if (typeof window !== 'undefined') {
  (window as typeof window & { debugAuth?: typeof debugAuth }).debugAuth = debugAuth
}
