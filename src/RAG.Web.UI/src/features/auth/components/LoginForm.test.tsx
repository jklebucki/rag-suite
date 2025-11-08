import React from 'react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { LoginForm } from './LoginForm'
import { render as customRender } from '@/test-utils/test-utils'

// Mock main.tsx to prevent React root creation in tests
vi.mock('@/main', () => ({
  queryClient: {
    invalidateQueries: vi.fn(),
    setQueryData: vi.fn(),
    getQueryData: vi.fn(),
  },
}))

// Mock only useAuth hook, providers are provided by customRender
vi.mock('@/shared/contexts/AuthContext', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/shared/contexts/AuthContext')>()
  return {
    ...actual,
    useAuth: vi.fn(),
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

import { useAuth } from '@/shared/contexts/AuthContext'

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

  it('should render login form', () => {
    customRender(<LoginForm />)
    
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /login/i })).toBeInTheDocument()
  })

  it('should validate email on submit', async () => {
    const user = userEvent.setup()
    customRender(<LoginForm />)
    
    const emailInput = screen.getByLabelText(/email/i)
    const passwordInput = screen.getByLabelText(/password/i)
    const submitButton = screen.getByRole('button', { name: /login/i })
    
    await user.type(emailInput, 'invalid-email')
    await user.type(passwordInput, 'password123')
    await user.click(submitButton)
    
    await waitFor(() => {
      expect(mockLogin).not.toHaveBeenCalled()
    })
  })

  it('should validate password on submit', async () => {
    const user = userEvent.setup()
    customRender(<LoginForm />)
    
    const emailInput = screen.getByLabelText(/email/i)
    const passwordInput = screen.getByLabelText(/password/i)
    const submitButton = screen.getByRole('button', { name: /login/i })
    
    await user.type(emailInput, 'test@example.com')
    await user.type(passwordInput, '123') // Too short
    await user.click(submitButton)
    
    await waitFor(() => {
      expect(mockLogin).not.toHaveBeenCalled()
    })
  })

  it('should call login with form data on valid submit', async () => {
    const user = userEvent.setup()
    mockLogin.mockResolvedValue(true)
    
    customRender(<LoginForm />)
    
    const emailInput = screen.getByLabelText(/email/i)
    const passwordInput = screen.getByLabelText(/password/i)
    const submitButton = screen.getByRole('button', { name: /login/i })
    
    await user.type(emailInput, 'test@example.com')
    await user.type(passwordInput, 'password123')
    await user.click(submitButton)
    
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
    customRender(<LoginForm />)
    
    const passwordInput = screen.getByLabelText(/password/i) as HTMLInputElement
    const toggleButton = screen.getByRole('button', { name: /show password/i })
    
    expect(passwordInput.type).toBe('password')
    
    await user.click(toggleButton)
    expect(passwordInput.type).toBe('text')
    
    await user.click(toggleButton)
    expect(passwordInput.type).toBe('password')
  })

  it('should handle remember me checkbox', async () => {
    const user = userEvent.setup()
    mockLogin.mockResolvedValue(true)
    
    customRender(<LoginForm />)
    
    const rememberCheckbox = screen.getByLabelText(/remember me/i)
    const emailInput = screen.getByLabelText(/email/i)
    const passwordInput = screen.getByLabelText(/password/i)
    const submitButton = screen.getByRole('button', { name: /login/i })
    
    await user.type(emailInput, 'test@example.com')
    await user.type(passwordInput, 'password123')
    await user.click(rememberCheckbox)
    await user.click(submitButton)
    
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
    
    customRender(<LoginForm />)
    
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
    
    customRender(<LoginForm />)
    
    const emailInput = screen.getByLabelText(/email/i)
    await user.type(emailInput, 't')
    
    expect(mockClearError).toHaveBeenCalled()
  })

  it('should show loading state during login', () => {
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
    
    customRender(<LoginForm />)
    
    const submitButton = screen.getByRole('button', { name: /login/i })
    expect(submitButton).toBeDisabled()
  })
})

