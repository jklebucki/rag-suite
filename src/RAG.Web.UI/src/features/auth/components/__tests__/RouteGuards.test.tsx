import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { MemoryRouter } from 'react-router-dom'
import { render, screen } from '@testing-library/react'
import { ProtectedRoute } from '../ProtectedRoute'
import { AuthRoute } from '../AuthRoute'
import { RoleProtectedRoute } from '../RoleProtectedRoute'
import { AdminProtectedRoute } from '../AdminProtectedRoute'

const mockedNavigate = vi.fn()
const mockUseAuth = vi.fn()

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockedNavigate,
    useLocation: () => ({ pathname: '/current' }),
  }
})

vi.mock('@/shared/contexts/AuthContext', () => ({
  useAuth: () => mockUseAuth(),
}))

describe('Route Guards', () => {
  beforeEach(() => {
    mockedNavigate.mockReset()
  })

  afterEach(() => {
    mockUseAuth.mockReset()
  })

  it('ProtectedRoute renders loading state while auth is loading', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, loading: true })

    render(
      <MemoryRouter>
        <ProtectedRoute>
          <div>protected</div>
        </ProtectedRoute>
      </MemoryRouter>,
    )

    expect(screen.getByText('Checking access...')).toBeInTheDocument()
  })

  it('ProtectedRoute redirects to login when unauthenticated', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, loading: false })

    render(
      <MemoryRouter>
        <ProtectedRoute>
          <div>protected</div>
        </ProtectedRoute>
      </MemoryRouter>,
    )

    expect(mockedNavigate).toHaveBeenCalledWith(
      '/login',
      expect.objectContaining({
        replace: true,
        state: expect.objectContaining({ from: expect.objectContaining({ pathname: '/current' }) }),
      }),
    )
  })

  it('ProtectedRoute renders children when authenticated', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, loading: false })

    render(
      <MemoryRouter>
        <ProtectedRoute>
          <div>protected</div>
        </ProtectedRoute>
      </MemoryRouter>,
    )

    expect(screen.getByText('protected')).toBeInTheDocument()
    expect(mockedNavigate).not.toHaveBeenCalled()
  })

  it('AuthRoute navigates away when user already authenticated', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, loading: false })

    render(
      <MemoryRouter>
        <AuthRoute>
          <div>login form</div>
        </AuthRoute>
      </MemoryRouter>,
    )

    expect(mockedNavigate).toHaveBeenCalledWith('/', expect.objectContaining({ replace: true }))
  })

  it('RoleProtectedRoute denies access without role', () => {
    mockUseAuth.mockReturnValue({
      isAuthenticated: true,
      loading: false,
      user: { roles: ['User'] },
    })

    render(
      <MemoryRouter>
        <RoleProtectedRoute allowedRoles={['Admin']}>
          <div>admin area</div>
        </RoleProtectedRoute>
      </MemoryRouter>,
    )

    expect(mockedNavigate).toHaveBeenCalledWith('/', expect.objectContaining({ replace: true }))
  })

  it('AdminProtectedRoute allows admins', () => {
    mockUseAuth.mockReturnValue({
      isAuthenticated: true,
      loading: false,
      user: { roles: ['Admin'] },
    })

    render(
      <MemoryRouter>
        <AdminProtectedRoute>
          <div>admin dashboard</div>
        </AdminProtectedRoute>
      </MemoryRouter>,
    )

    expect(screen.getByText('admin dashboard')).toBeInTheDocument()
    expect(mockedNavigate).not.toHaveBeenCalled()
  })
})

