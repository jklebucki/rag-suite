import React, { createContext, useContext, useEffect, useReducer, ReactNode } from 'react'
import { authService } from '@/services/auth'
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
  switch (action.type) {
    case 'SET_LOADING':
      return { ...state, loading: action.payload }
    case 'SET_USER':
      return { 
        ...state, 
        user: action.payload,
        isAuthenticated: !!action.payload,
        loading: false 
      }
    case 'SET_TOKEN':
      return { ...state, token: action.payload }
    case 'SET_REFRESH_TOKEN':
      return { ...state, refreshToken: action.payload }
    case 'SET_ERROR':
      return { ...state, error: action.payload, loading: false }
    case 'LOGOUT':
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

  // Initialize auth state
  useEffect(() => {
    const initializeAuth = async () => {
      const token = authService.getToken()
      if (token) {
        try {
          const user = await authService.getCurrentUser()
          if (user) {
            dispatch({ type: 'SET_USER', payload: user })
            dispatch({ type: 'SET_TOKEN', payload: token })
            dispatch({ type: 'SET_REFRESH_TOKEN', payload: authService.getRefreshToken() })
          } else {
            authService.logout()
            dispatch({ type: 'LOGOUT' })
          }
        } catch (error) {
          console.error('Auth initialization failed:', error)
          authService.logout()
          dispatch({ type: 'LOGOUT' })
        }
      } else {
        dispatch({ type: 'SET_LOADING', payload: false })
      }
    }

    initializeAuth()
  }, [])

  const login = async (credentials: LoginRequest): Promise<boolean> => {
    dispatch({ type: 'SET_LOADING', payload: true })
    dispatch({ type: 'SET_ERROR', payload: null })

    try {
      const loginData = await authService.login(credentials)
      
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
