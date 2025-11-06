import { describe, it, expect, vi } from 'vitest'
import { renderHook } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { useLayout } from '../useLayout'
import { createMockUser } from '@/test-utils/test-utils'

// Mock contexts
vi.mock('@/contexts/I18nContext', () => ({
  useI18n: () => ({
    t: (key: string) => key,
    language: 'en',
  }),
}))

vi.mock('@/contexts/AuthContext', () => ({
  useAuth: vi.fn(),
}))

import { useAuth } from '@/contexts/AuthContext'

describe('useLayout', () => {
  const wrapper = ({ children }: { children: React.ReactNode }) => (
    <MemoryRouter initialEntries={['/dashboard']}>{children}</MemoryRouter>
  )

  it('should initialize with sidebar closed', () => {
    vi.mocked(useAuth).mockReturnValue({
      user: null,
      isAuthenticated: false,
      token: null,
      refreshToken: null,
      loading: false,
      error: null,
      refreshError: false,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      logoutAllDevices: vi.fn(),
      resetPassword: vi.fn(),
      confirmPasswordReset: vi.fn(),
      refreshAuth: vi.fn(),
      clearError: vi.fn(),
      clearRefreshError: vi.fn(),
    })

    const { result } = renderHook(() => useLayout(), { wrapper })
    expect(result.current.isSidebarOpen).toBe(false)
  })

  it('should toggle sidebar', () => {
    vi.mocked(useAuth).mockReturnValue({
      user: null,
      isAuthenticated: false,
      token: null,
      refreshToken: null,
      loading: false,
      error: null,
      refreshError: false,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      logoutAllDevices: vi.fn(),
      resetPassword: vi.fn(),
      confirmPasswordReset: vi.fn(),
      refreshAuth: vi.fn(),
      clearError: vi.fn(),
      clearRefreshError: vi.fn(),
    })

    const { result } = renderHook(() => useLayout(), { wrapper })
    
    expect(result.current.isSidebarOpen).toBe(false)
    result.current.toggleSidebar()
    expect(result.current.isSidebarOpen).toBe(true)
    result.current.toggleSidebar()
    expect(result.current.isSidebarOpen).toBe(false)
  })

  it('should close sidebar', () => {
    vi.mocked(useAuth).mockReturnValue({
      user: null,
      isAuthenticated: false,
      token: null,
      refreshToken: null,
      loading: false,
      error: null,
      refreshError: false,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      logoutAllDevices: vi.fn(),
      resetPassword: vi.fn(),
      confirmPasswordReset: vi.fn(),
      refreshAuth: vi.fn(),
      clearError: vi.fn(),
      clearRefreshError: vi.fn(),
    })

    const { result } = renderHook(() => useLayout(), { wrapper })
    
    result.current.toggleSidebar()
    expect(result.current.isSidebarOpen).toBe(true)
    result.current.closeSidebar()
    expect(result.current.isSidebarOpen).toBe(false)
  })

  it('should include admin navigation for admin users', () => {
    const adminUser = createMockUser({ roles: ['Admin'] })
    
    vi.mocked(useAuth).mockReturnValue({
      user: adminUser,
      isAuthenticated: true,
      token: 'token',
      refreshToken: 'refresh',
      loading: false,
      error: null,
      refreshError: false,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      logoutAllDevices: vi.fn(),
      resetPassword: vi.fn(),
      confirmPasswordReset: vi.fn(),
      refreshAuth: vi.fn(),
      clearError: vi.fn(),
      clearRefreshError: vi.fn(),
    })

    const { result } = renderHook(() => useLayout(), { wrapper })
    
    const settingsNav = result.current.navigation.find(nav => nav.href === '/settings')
    expect(settingsNav).toBeDefined()
  })

  it('should not include admin navigation for non-admin users', () => {
    const regularUser = createMockUser({ roles: ['User'] })
    
    vi.mocked(useAuth).mockReturnValue({
      user: regularUser,
      isAuthenticated: true,
      token: 'token',
      refreshToken: 'refresh',
      loading: false,
      error: null,
      refreshError: false,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      logoutAllDevices: vi.fn(),
      resetPassword: vi.fn(),
      confirmPasswordReset: vi.fn(),
      refreshAuth: vi.fn(),
      clearError: vi.fn(),
      clearRefreshError: vi.fn(),
    })

    const { result } = renderHook(() => useLayout(), { wrapper })
    
    const settingsNav = result.current.navigation.find(nav => nav.href === '/settings')
    expect(settingsNav).toBeUndefined()
  })

  it('should detect active route', () => {
    vi.mocked(useAuth).mockReturnValue({
      user: null,
      isAuthenticated: false,
      token: null,
      refreshToken: null,
      loading: false,
      error: null,
      refreshError: false,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      logoutAllDevices: vi.fn(),
      resetPassword: vi.fn(),
      confirmPasswordReset: vi.fn(),
      refreshAuth: vi.fn(),
      clearError: vi.fn(),
      clearRefreshError: vi.fn(),
    })

    const { result } = renderHook(() => useLayout(), { wrapper })
    
    expect(result.current.isActiveRoute('/dashboard')).toBe(true)
    expect(result.current.isActiveRoute('/chat')).toBe(false)
  })

  it('should include cyber panel navigation for authenticated users', () => {
    const user = createMockUser()
    
    vi.mocked(useAuth).mockReturnValue({
      user,
      isAuthenticated: true,
      token: 'token',
      refreshToken: 'refresh',
      loading: false,
      error: null,
      refreshError: false,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      logoutAllDevices: vi.fn(),
      resetPassword: vi.fn(),
      confirmPasswordReset: vi.fn(),
      refreshAuth: vi.fn(),
      clearError: vi.fn(),
      clearRefreshError: vi.fn(),
    })

    const { result } = renderHook(() => useLayout(), { wrapper })
    
    const cyberNav = result.current.navigation.find(nav => nav.href === '/cyberpanel')
    expect(cyberNav).toBeDefined()
  })
})

