import React, { createContext, useContext, useEffect, useReducer, ReactNode, useCallback } from 'react'
import { authService } from '@/services/auth'
import { useTokenRefresh } from '@/hooks/useTokenRefresh'
import { useAuthStorage } from '@/hooks/useAuthStorage'
import type { AuthState, User, LoginRequest, RegisterRequest, ResetPasswordRequest } from '@/types/auth'

interface AuthContextType extends AuthState {
  login: (credentials: LoginRequest) => Promise<boolean>
  register: (userData: RegisterRequest) => Promise<boolean>
  resetPassword: (data: ResetPasswordRequest) => Promise<void>
  logout: () => Promise<void>
  refreshAuth: () => Promise<void>
  clearError: () => void
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

type AuthAction =
  | { type: 'SET_LOADING'; payload: boolean }
  | { type: 'SET_USER'; payload: User | null }
  | { type: 'SET_TOKEN'; payload: string | null }
  | { type: 'SET_REFRESH_TOKEN'; payload: string | null }
  | { type: 'SET_ERROR'; payload: string | null }
  | { type: 'LOGOUT' }

const initialState: AuthState = {
  isAuthenticated: false,
  user: null,
  token: null,
  refreshToken: null,
  loading: true,
  error: null,
}

function authReducer(state: AuthState, action: AuthAction): AuthState {
  console.debug('🔄 Auth reducer action:', action.type, 'payload' in action ? action.payload : 'no payload')
  
  switch (action.type) {
    case 'SET_LOADING':
      return { ...state, loading: action.payload }
    case 'SET_USER':
      const newState = { 
        ...state, 
        user: action.payload,
        isAuthenticated: !!action.payload,
        loading: false 
      }
      console.debug('🔄 SET_USER new state:', { isAuthenticated: newState.isAuthenticated, loading: newState.loading })
      return newState
    case 'SET_TOKEN':
      return { ...state, token: action.payload }
    case 'SET_REFRESH_TOKEN':
      return { ...state, refreshToken: action.payload }
    case 'SET_ERROR':
      return { ...state, error: action.payload, loading: false }
    case 'LOGOUT':
      console.debug('🔄 LOGOUT action')
      return { 
        ...initialState, 
        loading: false 
      }
    default:
      return state
  }
}

interface AuthProviderProps {
  children: ReactNode
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [state, dispatch] = useReducer(authReducer, initialState)

  // Callbacks for auth storage hook
  const handleStorageLogin = useCallback((token: string, refreshToken: string | null, user: User) => {
    dispatch({ type: 'SET_TOKEN', payload: token })
    dispatch({ type: 'SET_REFRESH_TOKEN', payload: refreshToken })
    dispatch({ type: 'SET_USER', payload: user })
  }, [])

  const handleStorageLogout = useCallback(() => {
    dispatch({ type: 'LOGOUT' })
  }, [])

  // Callbacks for token refresh hook
  const handleTokenRefresh = useCallback((token: string, refreshToken: string) => {
    dispatch({ type: 'SET_TOKEN', payload: token })
    dispatch({ type: 'SET_REFRESH_TOKEN', payload: refreshToken })
  }, [])

  const handleLogout = useCallback(() => {
    dispatch({ type: 'LOGOUT' })
  }, [])

  // Use custom hooks
  const { storeAuthData, clearAuthData } = useAuthStorage(handleStorageLogin, handleStorageLogout)
  const { performTokenRefresh } = useTokenRefresh(state.isAuthenticated, handleTokenRefresh, handleLogout)

  // Initialize auth state - simplified and more reliable approach
  useEffect(() => {
    const initializeAuth = () => {
      console.debug('🔐 Auth initialization started (synchronous)')
      
      try {
        // Synchronous check of localStorage - no async calls here
        const hasValidAuth = authService.isAuthenticated()
        
        if (hasValidAuth) {
          const token = authService.getToken()
          const refreshToken = authService.getRefreshToken()
          const userData = authService.getUser()
          
          console.debug('🔐 Valid auth found, setting state immediately')
          
          // Set state immediately - this should prevent login redirect
          dispatch({ type: 'SET_TOKEN', payload: token })
          dispatch({ type: 'SET_REFRESH_TOKEN', payload: refreshToken })
          dispatch({ type: 'SET_USER', payload: userData })
          // Loading is automatically set to false in SET_USER action
          
          // Schedule background verification (don't block initial render)
          setTimeout(() => {
            verifyAuthInBackground()
          }, 500)
        } else {
          console.debug('🔐 No valid auth found')
          dispatch({ type: 'LOGOUT' })
        }
      } catch (error) {
        console.error('🔐 Auth initialization failed:', error)
        dispatch({ type: 'LOGOUT' })
      }
    }
    
    const verifyAuthInBackground = async () => {
      try {
        console.debug('🔐 Background auth verification started')
        const user = await authService.getCurrentUser()
        if (user) {
          console.debug('🔐 Background verification successful')
          dispatch({ type: 'SET_USER', payload: user })
        } else {
          console.warn('🔐 Background verification failed - server rejected token')
          // Try refresh instead of immediate logout
          performTokenRefresh()
        }
      } catch (error) {
        console.warn('🔐 Background verification error:', error)
        // Try refresh instead of immediate logout
        performTokenRefresh()
      }
    }

    // Run initialization immediately (synchronously)
    initializeAuth()
  }, [performTokenRefresh])

  const login = async (credentials: LoginRequest): Promise<boolean> => {
    dispatch({ type: 'SET_LOADING', payload: true })
    dispatch({ type: 'SET_ERROR', payload: null })

    try {
      const loginData = await authService.login(credentials)
      
      // Store auth data using our secure storage method
      storeAuthData(loginData.token, loginData.refreshToken, loginData.user)
      
      // Update context state
      dispatch({ type: 'SET_TOKEN', payload: loginData.token })
      dispatch({ type: 'SET_REFRESH_TOKEN', payload: loginData.refreshToken })
      dispatch({ type: 'SET_USER', payload: loginData.user })
      return true
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || error.message || 'Login failed'
      dispatch({ type: 'SET_ERROR', payload: errorMessage })
      throw error
    } finally {
      dispatch({ type: 'SET_LOADING', payload: false })
    }
  }

  const register = async (userData: RegisterRequest): Promise<boolean> => {
    dispatch({ type: 'SET_LOADING', payload: true })
    dispatch({ type: 'SET_ERROR', payload: null })

    try {
      await authService.register(userData)
      return true
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || error.message || 'Registration failed'
      dispatch({ type: 'SET_ERROR', payload: errorMessage })
      throw error
    } finally {
      dispatch({ type: 'SET_LOADING', payload: false })
    }
  }

  const resetPassword = async (data: ResetPasswordRequest): Promise<void> => {
    dispatch({ type: 'SET_LOADING', payload: true })
    dispatch({ type: 'SET_ERROR', payload: null })

    try {
      await authService.requestPasswordReset(data)
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || error.message || 'Password reset failed'
      dispatch({ type: 'SET_ERROR', payload: errorMessage })
      throw error
    } finally {
      dispatch({ type: 'SET_LOADING', payload: false })
    }
  }

  const logout = async (): Promise<void> => {
    dispatch({ type: 'SET_LOADING', payload: true })
    
    try {
      await authService.logout()
    } catch (error) {
      console.error('Logout error:', error)
    } finally {
      // Clear storage and update state
      clearAuthData()
      dispatch({ type: 'LOGOUT' })
    }
  }

  const refreshAuth = async (): Promise<void> => {
    try {
      const user = await authService.getCurrentUser()
      if (user) {
        dispatch({ type: 'SET_USER', payload: user })
        dispatch({ type: 'SET_TOKEN', payload: authService.getToken() })
        dispatch({ type: 'SET_REFRESH_TOKEN', payload: authService.getRefreshToken() })
      }
    } catch (error) {
      console.error('Auth refresh failed:', error)
    }
  }

  const clearError = (): void => {
    dispatch({ type: 'SET_ERROR', payload: null })
  }

  const value: AuthContextType = {
    ...state,
    login,
    register,
    resetPassword,
    logout,
    refreshAuth,
    clearError,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext)
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}

export default AuthContext
