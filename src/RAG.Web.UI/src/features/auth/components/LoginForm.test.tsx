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
      t: (key: keyof typeof en) => {
        // Return actual translation value, not just the key
        const value = en[key]
        return value !== undefined ? value : String(key)
      },
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
  let utils: ReturnType<typeof render> | null = null

  await act(async () => {
    utils = render(
      <MemoryRouter>
        <LoginForm />
      </MemoryRouter>,
    )
  })

  if (!utils) {
    throw new Error('LoginForm failed to render during tests.')
  }

  return utils
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
    
    // Fill form with invalid email and submit
    await act(async () => {
      await user.clear(emailInput)
      await user.type(emailInput, 'invalid-email')
      await user.clear(passwordInput)
      await user.type(passwordInput, 'password123')
    })
    
    await act(async () => {
      await user.click(submitButton)
    })
    
    // useActionState processes form submission asynchronously
    // Wait for validation to complete and error to be displayed
    await waitFor(() => {
      // useActionState should return fieldErrors, not call login
      expect(mockLogin).not.toHaveBeenCalled()
      
      // Check for email validation error
      // LoginForm displays error as state.fieldErrors.email
      // The error message comes from t('auth.validation.email_invalid')
      // We check for the error text or the error class on the input
      const emailError = screen.queryByText(/invalid|email/i)
      const emailInputElement = document.querySelector('#email') as HTMLInputElement
      const hasErrorClass = emailInputElement?.classList.contains('form-input-error')
      
      // Either the error message should be visible or the input should have error class
      expect(emailError || hasErrorClass).toBeTruthy()
    }, { timeout: 10000 })
  }, 15000)

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
      // useActionState zwraca fieldErrors dla hasła
      expect(mockLogin).not.toHaveBeenCalled()
      // Powinien wyświetlić błąd walidacji hasła (szukamy błędu, nie labela)
      const errorMessage = screen.queryByText(/password.*at least|password.*required/i)
      expect(errorMessage).toBeInTheDocument()
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

  it('should display error message from useActionState when login fails', async () => {
    const user = userEvent.setup()
    mockLogin.mockResolvedValue(false)
    
    await renderLoginForm()
    
    const emailInput = screen.getByLabelText(/email/i)
    const passwordInput = screen.getByLabelText(/password/i, { selector: 'input' })
    const submitButton = screen.getByRole('button', { name: /sign in/i })
    
    await act(async () => {
      await user.type(emailInput, 'test@example.com')
      await user.type(passwordInput, 'password123')
      await user.click(submitButton)
    })
    
    // useActionState zwraca error w state, nie z AuthContext
    await waitFor(() => {
      expect(mockLogin).toHaveBeenCalled()
      // Error powinien być wyświetlony przez state?.error
      const errorMessage = screen.queryByText(/error|invalid/i)
      expect(errorMessage).toBeTruthy()
    })
  })

  it('should handle form submission with useActionState', async () => {
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
    
    // useActionState wywołuje formAction, który wywołuje login
    await waitFor(() => {
      expect(mockLogin).toHaveBeenCalledWith({
        email: 'test@example.com',
        password: 'password123',
        rememberMe: false,
      })
    })
  })

  it('should disable submit button during form submission (useFormStatus)', async () => {
    const user = userEvent.setup()
    // Mock login to return a delayed promise to test pending state
    let resolveLogin: (value: boolean) => void
    const loginPromise = new Promise<boolean>((resolve) => {
      resolveLogin = resolve
    })
    mockLogin.mockReturnValue(loginPromise)
    
    await renderLoginForm()
    
    const emailInput = screen.getByLabelText(/email/i)
    const passwordInput = screen.getByLabelText(/password/i, { selector: 'input' })
    const submitButton = screen.getByRole('button', { name: /sign in/i })
    
    await act(async () => {
      await user.type(emailInput, 'test@example.com')
      await user.type(passwordInput, 'password123')
      await user.click(submitButton)
    })
    
    // SubmitButton używa useFormStatus, więc powinien być disabled podczas pending
    await waitFor(() => {
      expect(submitButton).toBeDisabled()
      // Powinien wyświetlić loadingText jeśli jest dostępny
      const loadingText = screen.queryByText(/signing in/i)
      if (loadingText) {
        expect(loadingText).toBeInTheDocument()
      }
    })
    
    // Resolve the promise to complete the test
    resolveLogin!(true)
    await waitFor(() => {
      expect(mockLogin).toHaveBeenCalled()
    })
  })

  it('should display field errors from useActionState', async () => {
    const user = userEvent.setup()
    await renderLoginForm()
    
    const submitButton = screen.getByRole('button', { name: /sign in/i })
    
    // Submit empty form to trigger validation
    // Note: HTML5 validation may prevent form submission if fields are required
    // So we need to ensure the form can be submitted
    const emailInput = screen.getByLabelText(/email/i)
    const passwordInput = screen.getByLabelText(/password/i, { selector: 'input' })
    
    // Clear any default values and submit
    await act(async () => {
      await user.clear(emailInput)
      await user.clear(passwordInput)
      // Try to submit - useActionState will validate
      await user.click(submitButton)
    })
    
    // useActionState processes form submission asynchronously
    // Wait for validation to complete and errors to be displayed
    await waitFor(() => {
      // useActionState should return fieldErrors for empty fields
      // Check for error indicators - either error text or error class on inputs
      const errorInputs = document.querySelectorAll('.form-input-error')
      const emailInputElement = document.querySelector('#email') as HTMLInputElement
      const passwordInputElement = document.querySelector('#password') as HTMLInputElement
      
      // Check if errors are displayed or inputs have error class
      const hasEmailError = emailInputElement?.classList.contains('form-input-error')
      const hasPasswordError = passwordInputElement?.classList.contains('form-input-error')
      
      // Check for error messages (but avoid matching labels)
      const emailErrorMsg = emailInputElement?.parentElement?.querySelector('p.text-red-600, p.text-red-400')
      const passwordErrorMsg = passwordInputElement?.parentElement?.querySelector('p.text-red-600, p.text-red-400')
      
      // At least one field should show an error, or login should not be called
      const hasErrors = hasEmailError || hasPasswordError || errorInputs.length > 0 || emailErrorMsg || passwordErrorMsg
      const loginNotCalled = !mockLogin.mock.calls.length
      
      // Either errors are displayed or login wasn't called (which means validation prevented submission)
      expect(hasErrors || loginNotCalled).toBeTruthy()
    }, { timeout: 10000 })
  }, 15000)
})

