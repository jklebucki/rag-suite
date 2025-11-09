// @ts-nocheck

import React from 'react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor, act } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { LoginForm } from './LoginForm'

// Mock main.tsx to prevent React root creation in tests
vi.mock('@/main', () => ({
  queryClient: {
    invalidateQueries: vi.fn(),
    setQueryData: vi.fn(),
    getQueryData: vi.fn(),
  },
}))

// Mock only the pieces we need from contexts to avoid side effects and act warnings
vi.mock('@/shared/contexts/AuthContext', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/shared/contexts/AuthContext')>()
  return {
    ...actual,
    AuthProvider: ({ children }: { children: React.ReactNode }) => <>{children}</>,
    useAuth: vi.fn(),
  }
})

vi.mock('@/shared/contexts/ToastContext', async () => {
  const ReactModule = await vi.importActual<typeof import('react')>('react')
  const toastMocks = {
    addToast: vi.fn(),
    showSuccess: vi.fn(),
    showError: vi.fn(),
    showWarning: vi.fn(),
    showInfo: vi.fn(),
    removeToast: vi.fn(),
    clearToasts: vi.fn(),
  }
  return {
    ToastProvider: ({ children }: { children: React.ReactNode }) =>
      ReactModule.createElement(ReactModule.Fragment, null, children),
    useToast: () => toastMocks,
  }
})

vi.mock('@/shared/contexts/ConfigurationContext', async () => {
  const ReactModule = await vi.importActual<typeof import('react')>('react')
  const { configurationService } = await vi.importActual<
    typeof import('@/features/settings/services/configuration.service')
  >('@/features/settings/services/configuration.service')

  const defaultConfig = configurationService.getDefaultConfiguration()

  return {
    ConfigurationProvider: ({ children }: { children: React.ReactNode }) =>
      ReactModule.createElement(ReactModule.Fragment, null, children),
    useConfiguration: () => ({
      configuration: defaultConfig,
      loading: false,
      error: null,
      lastFetched: new Date(),
      fetchConfiguration: vi.fn(),
      refreshConfiguration: vi.fn(),
      clearError: vi.fn(),
    }),
    usePasswordValidation: () => {
      const passwordRequirements = defaultConfig.passwordRequirements
      const validatePassword = (password: string) => {
        const errors: string[] = []

        if (password.length < passwordRequirements.requiredLength) {
          errors.push(`auth.validation.password_min_length#${passwordRequirements.requiredLength}`)
        }
        if (passwordRequirements.requireDigit && !/\d/.test(password)) {
          errors.push('auth.validation.password_require_digit')
        }
        if (passwordRequirements.requireLowercase && !/[a-z]/.test(password)) {
          errors.push('auth.validation.password_require_lowercase')
        }
        if (passwordRequirements.requireUppercase && !/[A-Z]/.test(password)) {
          errors.push('auth.validation.password_require_uppercase')
        }
        if (passwordRequirements.requireNonAlphanumeric && !/[^a-zA-Z0-9]/.test(password)) {
          errors.push('auth.validation.password_require_special')
        }

        return {
          isValid: errors.length === 0,
          errors,
        }
      }

      return {
        validatePassword,
        passwordRequirements,
      }
    },
  }
})

vi.mock('@/shared/contexts/I18nContext', async () => {
  const ReactModule = await vi.importActual<typeof import('react')>('react')
  const { SUPPORTED_LANGUAGES, DEFAULT_LANGUAGE } = await vi.importActual<
    typeof import('@/shared/types/i18n')
  >('@/shared/types/i18n')
  const { en } = await vi.importActual<typeof import('@/locales/en')>('@/locales/en')

  return {
    I18nProvider: ({ children }: { children: React.ReactNode }) =>
      ReactModule.createElement(ReactModule.Fragment, null, children),
    useI18n: () => ({
      language: DEFAULT_LANGUAGE,
      setLanguage: vi.fn(),
      t: (key: keyof typeof en) => en[key] ?? key,
      languages: SUPPORTED_LANGUAGES,
      isAutoDetected: false,
    }),
  }
})

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => vi.fn(),
    useLocation: () => ({ pathname: '/login', state: null }),
  }
})

import { MemoryRouter } from 'react-router-dom'
import { useAuth } from '@/shared/contexts/AuthContext'

const renderLoginForm = async () => {
  let utils: ReturnType<typeof render>
  await act(async () => {
    utils = render(
      <MemoryRouter>
        <LoginForm />
      </MemoryRouter>
    )
  })
  // TypeScript can't guarantee assignment inside act, but in practice it's set.
  // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
  return utils!
}

describe('LoginForm', () => {
  const mockLogin = vi.fn()
  const mockClearError = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(useAuth).mockReturnValue({
      login: mockLogin,
      loading: false,
      error: null,
      clearError: mockClearError,
      user: null,
      isAuthenticated: false,
      token: null,
      refreshToken: null,
      refreshError: false,
      register: vi.fn(),
      logout: vi.fn(),
      logoutAllDevices: vi.fn(),
      resetPassword: vi.fn(),
      confirmPasswordReset: vi.fn(),
      refreshAuth: vi.fn(),
      clearRefreshError: vi.fn(),
    })
  })

  it('should render login form', async () => {
    await renderLoginForm()
    
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/password/i, { selector: 'input' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument()
  })

  it('should validate email on submit', async () => {
    const user = userEvent.setup()
    await renderLoginForm()

    const emailInput = screen.getByLabelText(/email/i)
    const passwordInput = screen.getByLabelText(/password/i, { selector: 'input' })
    const submitButton = screen.getByRole('button', { name: /sign in/i })
    
    await act(async () => {
      await user.type(emailInput, 'invalid-email')
      await user.type(passwordInput, 'password123')
      await user.click(submitButton)
    })
    
    await waitFor(() => {
      expect(mockLogin).not.toHaveBeenCalled()
    })
  })

  it('should validate password on submit', async () => {
    const user = userEvent.setup()
    await renderLoginForm()
    
    const emailInput = screen.getByLabelText(/email/i)
    const passwordInput = screen.getByLabelText(/password/i, { selector: 'input' })
    const submitButton = screen.getByRole('button', { name: /sign in/i })
    
    await act(async () => {
      await user.type(emailInput, 'test@example.com')
      await user.type(passwordInput, '123') // Too short
      await user.click(submitButton)
    })
    
    await waitFor(() => {
      expect(mockLogin).not.toHaveBeenCalled()
    })
  })

  it('should call login with form data on valid submit', async () => {
    const user = userEvent.setup()
    mockLogin.mockResolvedValue(true)
    
    await renderLoginForm()
    
    const emailInput = screen.getByLabelText(/email/i)
    const passwordInput = screen.getByLabelText(/password/i, { selector: 'input' })
    const submitButton = screen.getByRole('button', { name: /sign in/i })
    
    await act(async () => {
      await user.type(emailInput, 'test@example.com')
      await user.type(passwordInput, 'password123')
      await user.click(submitButton)
    })
    
    await waitFor(() => {
      expect(mockLogin).toHaveBeenCalledWith({
        email: 'test@example.com',
        password: 'password123',
        rememberMe: false,
      })
    })
  })

  it('should toggle password visibility', async () => {
    const user = userEvent.setup()
    await renderLoginForm()
    
    const passwordInput = screen.getByLabelText(/password/i, { selector: 'input' }) as HTMLInputElement
    const toggleButton = screen.getByRole('button', { name: /show password/i })
    
    expect(passwordInput.type).toBe('password')
    
    await act(async () => {
      await user.click(toggleButton)
    })
    expect(passwordInput.type).toBe('text')
    
    await act(async () => {
      await user.click(toggleButton)
    })
    expect(passwordInput.type).toBe('password')
  })

  it('should handle remember me checkbox', async () => {
    const user = userEvent.setup()
    mockLogin.mockResolvedValue(true)
    
    await renderLoginForm()
    
    const rememberCheckbox = screen.getByLabelText(/remember me/i)
    const emailInput = screen.getByLabelText(/email/i)
    const passwordInput = screen.getByLabelText(/password/i, { selector: 'input' })
    const submitButton = screen.getByRole('button', { name: /sign in/i })
    
    await act(async () => {
      await user.type(emailInput, 'test@example.com')
      await user.type(passwordInput, 'password123')
      await user.click(rememberCheckbox)
      await user.click(submitButton)
    })
    
    await waitFor(() => {
      expect(mockLogin).toHaveBeenCalledWith({
        email: 'test@example.com',
        password: 'password123',
        rememberMe: true,
      })
    })
  })

  it('should display error message when login fails', async () => {
    mockLogin.mockResolvedValue(false)
    
    vi.mocked(useAuth).mockReturnValue({
      login: mockLogin,
      loading: false,
      error: 'Invalid credentials',
      clearError: mockClearError,
      user: null,
      isAuthenticated: false,
      token: null,
      refreshToken: null,
      refreshError: false,
      register: vi.fn(),
      logout: vi.fn(),
      logoutAllDevices: vi.fn(),
      resetPassword: vi.fn(),
      confirmPasswordReset: vi.fn(),
      refreshAuth: vi.fn(),
      clearRefreshError: vi.fn(),
    })
    
    await renderLoginForm()
    
    await waitFor(() => {
      expect(screen.getByText(/invalid credentials/i)).toBeInTheDocument()
    })
  })

  it('should clear error when user starts typing', async () => {
    const user = userEvent.setup()
    
    vi.mocked(useAuth).mockReturnValue({
      login: mockLogin,
      loading: false,
      error: 'Some error',
      clearError: mockClearError,
      user: null,
      isAuthenticated: false,
      token: null,
      refreshToken: null,
      refreshError: false,
      register: vi.fn(),
      logout: vi.fn(),
      logoutAllDevices: vi.fn(),
      resetPassword: vi.fn(),
      confirmPasswordReset: vi.fn(),
      refreshAuth: vi.fn(),
      clearRefreshError: vi.fn(),
    })
    
    await renderLoginForm()
    
    const emailInput = screen.getByLabelText(/email/i)
    await act(async () => {
      await user.type(emailInput, 't')
    })
    
    expect(mockClearError).toHaveBeenCalled()
  })

  it('should show loading state during login', async () => {
    vi.mocked(useAuth).mockReturnValue({
      login: mockLogin,
      loading: true,
      error: null,
      clearError: mockClearError,
      user: null,
      isAuthenticated: false,
      token: null,
      refreshToken: null,
      refreshError: false,
      register: vi.fn(),
      logout: vi.fn(),
      logoutAllDevices: vi.fn(),
      resetPassword: vi.fn(),
      confirmPasswordReset: vi.fn(),
      refreshAuth: vi.fn(),
      clearRefreshError: vi.fn(),
    })
    
    await renderLoginForm()
    
    const submitButton = screen.getByRole('button', { name: /signing in/i })
    expect(submitButton).toBeDisabled()
  })
})

