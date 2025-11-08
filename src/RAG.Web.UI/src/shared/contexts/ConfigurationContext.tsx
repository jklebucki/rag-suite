import React, { createContext, useContext, useReducer, useEffect, ReactNode } from 'react'
import { ConfigurationState, ConfigurationContextType, RegistrationConfiguration } from '@/features/settings/types/configuration'
import configurationService from '@/features/settings/services/configurationService'

// Actions
type ConfigurationAction =
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: RegistrationConfiguration }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'CLEAR_ERROR' }

// Initial state
const initialState: ConfigurationState = {
  configuration: null,
  loading: false,
  error: null,
  lastFetched: null,
}

// Reducer
function configurationReducer(state: ConfigurationState, action: ConfigurationAction): ConfigurationState {
  switch (action.type) {
    case 'FETCH_START':
      return {
        ...state,
        loading: true,
        error: null,
      }
    case 'FETCH_SUCCESS':
      return {
        ...state,
        configuration: action.payload,
        loading: false,
        error: null,
        lastFetched: new Date(),
      }
    case 'FETCH_ERROR':
      return {
        ...state,
        loading: false,
        error: action.payload,
      }
    case 'CLEAR_ERROR':
      return {
        ...state,
        error: null,
      }
    default:
      return state
  }
}

// Context
const ConfigurationContext = createContext<ConfigurationContextType | undefined>(undefined)

// Provider component
interface ConfigurationProviderProps {
  children: ReactNode
}

export function ConfigurationProvider({ children }: ConfigurationProviderProps) {
  const [state, dispatch] = useReducer(configurationReducer, initialState)

  const fetchConfiguration = async (): Promise<void> => {
    dispatch({ type: 'FETCH_START' })

    try {
      const configuration = await configurationService.getRegistrationConfiguration()
      dispatch({ type: 'FETCH_SUCCESS', payload: configuration })
    } catch (error) {
      console.error('Failed to fetch configuration:', error)

      // Use default configuration as fallback
      const defaultConfig = configurationService.getDefaultConfiguration()
      dispatch({ type: 'FETCH_SUCCESS', payload: defaultConfig })

      // Still dispatch error for logging purposes
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred'
      dispatch({ type: 'FETCH_ERROR', payload: errorMessage })
    }
  }

  const refreshConfiguration = async (): Promise<void> => {
    await fetchConfiguration()
  }

  const clearError = () => {
    dispatch({ type: 'CLEAR_ERROR' })
  }

  // Auto-fetch configuration on mount
  useEffect(() => {
    fetchConfiguration()
  }, [])

  const contextValue: ConfigurationContextType = {
    ...state,
    fetchConfiguration,
    refreshConfiguration,
    clearError,
  }

  return (
    <ConfigurationContext.Provider value={contextValue}>
      {children}
    </ConfigurationContext.Provider>
  )
}

// Custom hook
export function useConfiguration(): ConfigurationContextType {
  const context = useContext(ConfigurationContext)

  if (context === undefined) {
    throw new Error('useConfiguration must be used within a ConfigurationProvider')
  }

  return context
}

// Utility hook for password validation
export function usePasswordValidation() {
  const { configuration } = useConfiguration()

  const validatePassword = (password: string): { isValid: boolean; errors: string[] } => {
    const errors: string[] = []

    if (!configuration) {
      return { isValid: false, errors: ['Configuration not loaded'] }
    }

    const { passwordRequirements } = configuration

    // Length check
    if (password.length < passwordRequirements.requiredLength) {
      errors.push(`auth.validation.password_min_length#${passwordRequirements.requiredLength}`)
    }

    // Digit check
    if (passwordRequirements.requireDigit && !/\d/.test(password)) {
      errors.push('auth.validation.password_require_digit')
    }

    // Lowercase check
    if (passwordRequirements.requireLowercase && !/[a-z]/.test(password)) {
      errors.push('auth.validation.password_require_lowercase')
    }

    // Uppercase check
    if (passwordRequirements.requireUppercase && !/[A-Z]/.test(password)) {
      errors.push('auth.validation.password_require_uppercase')
    }

    // Special character check
    if (passwordRequirements.requireNonAlphanumeric && !/[^a-zA-Z0-9]/.test(password)) {
      errors.push('auth.validation.password_require_special')
    }

    return {
      isValid: errors.length === 0,
      errors
    }
  }

  return {
    validatePassword,
    passwordRequirements: configuration?.passwordRequirements || null
  }
}

export default ConfigurationContext
