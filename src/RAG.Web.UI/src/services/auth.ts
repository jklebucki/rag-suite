import axios, { AxiosInstance } from 'axios'
import type {
  LoginRequest,
  RegisterRequest,
  ResetPasswordRequest,
  ChangePasswordRequest,
  LoginResponse,
  RefreshTokenResponse,
  User
} from '@/types/auth'
import type { ApiResponse } from '@/types/api'

class AuthService {
  private client: AxiosInstance

  constructor() {
    this.client = axios.create({
      baseURL: '/api/auth',
      timeout: 30000, // 30 seconds for auth operations
      headers: {
        'Content-Type': 'application/json',
      },
    })

    // Request interceptor for auth token
    this.client.interceptors.request.use((config) => {
      const token = this.getToken()
      if (token) {
        config.headers.Authorization = `Bearer ${token}`
      }
      return config
    })

    // Response interceptor for token refresh
    this.client.interceptors.response.use(
      (response) => response,
      async (error) => {
        const originalRequest = error.config

        if (error.response?.status === 401 && !originalRequest._retry) {
          originalRequest._retry = true

          try {
            const refreshed = await this.refreshToken()
            if (refreshed) {
              originalRequest.headers.Authorization = `Bearer ${this.getToken()}`
              return this.client(originalRequest)
            }
          } catch (refreshError) {
            this.logout()
            window.location.href = '/login'
          }
        }

        return Promise.reject(error)
      }
    )
  }

  async login(credentials: LoginRequest): Promise<LoginResponse> {
    const response = await this.client.post<LoginResponse>('/login', credentials)
    const loginData = response.data

    // Store tokens and user data
    this.setTokens(loginData.token, loginData.refreshToken)
    this.setUser(loginData.user)

    return loginData
  }

  async register(userData: RegisterRequest): Promise<User> {
    // Filter out acceptTerms as backend doesn't expect it
    const { acceptTerms, ...registrationData } = userData
    const response = await this.client.post<ApiResponse<User>>('/register', registrationData)
    return response.data.data
  }

  async logout(): Promise<void> {
    try {
      await this.client.post('/logout')
    } catch (error) {
      // Continue with logout even if server request fails
      console.warn('Logout request failed:', error)
    } finally {
      this.clearStorage()
    }
  }

  async refreshToken(): Promise<boolean> {
    try {
      const refreshToken = this.getRefreshToken()
      if (!refreshToken) {
        return false
      }

      const response = await this.client.post<LoginResponse>('/refresh', {
        refreshToken
      })

      const tokenData = response.data
      this.setTokens(tokenData.token, tokenData.refreshToken)
      return true
    } catch (error) {
      this.clearStorage()
      return false
    }
  }

  async getCurrentUser(): Promise<User | null> {
    try {
      const response = await this.client.get<ApiResponse<User>>('/me')
      const user = response.data.data
      this.setUser(user)
      return user
    } catch (error) {
      return null
    }
  }

  async requestPasswordReset(request: ResetPasswordRequest): Promise<void> {
    await this.client.post<ApiResponse<void>>('/reset-password', request)
  }

  async changePassword(request: ChangePasswordRequest): Promise<void> {
    await this.client.post<ApiResponse<void>>('/change-password', request)
  }

  // Token management
  getToken(): string | null {
    return localStorage.getItem('auth_token')
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refresh_token')
  }

  getUser(): User | null {
    const userData = localStorage.getItem('user_data')
    return userData ? JSON.parse(userData) : null
  }

  isAuthenticated(): boolean {
    const token = this.getToken()
    if (!token) return false

    try {
      // Basic check - in production you might want to validate token expiry
      const payload = JSON.parse(atob(token.split('.')[1]))
      return payload.exp * 1000 > Date.now()
    } catch {
      return false
    }
  }

  hasRole(role: string): boolean {
    const user = this.getUser()
    return user?.roles?.includes(role) ?? false
  }

  hasAnyRole(roles: string[]): boolean {
    const user = this.getUser()
    return roles.some(role => user?.roles?.includes(role)) ?? false
  }

  private setTokens(accessToken: string, refreshToken: string): void {
    localStorage.setItem('auth_token', accessToken)
    localStorage.setItem('refresh_token', refreshToken)
  }

  private setUser(user: User): void {
    localStorage.setItem('user_data', JSON.stringify(user))
  }

  private clearStorage(): void {
    localStorage.removeItem('auth_token')
    localStorage.removeItem('refresh_token')
    localStorage.removeItem('user_data')
  }
}

export const authService = new AuthService()
export default authService
