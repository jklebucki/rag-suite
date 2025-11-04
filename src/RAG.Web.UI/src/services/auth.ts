import { AxiosInstance } from 'axios'
import { createHttpClient } from '@/utils/httpClient'
import { logger } from '@/utils/logger'
import { API_TIMEOUTS, API_ENDPOINTS, STORAGE_KEYS } from '@/constants/config'
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
    // Create client with auth endpoint, but with custom token refresh logic
    this.client = createHttpClient({
      baseURL: API_ENDPOINTS.AUTH,
      timeout: API_TIMEOUTS.AUTH,
      requireAuth: true,
    })

    // Override default response interceptor to add token refresh logic
    this.client.interceptors.response.clear()
    this.client.interceptors.response.use(
      (response) => response,
      async (error) => {
        const originalRequest = error.config

        logger.debug('HTTP error intercepted:', {
          status: error.response?.status,
          url: originalRequest?.url,
          hasRetry: !!originalRequest._retry
        })

        if (error.response?.status === 401 && !originalRequest._retry) {
          originalRequest._retry = true

          try {
            logger.debug('Attempting token refresh due to 401 error')
            const refreshed = await this.refreshToken()
            if (refreshed) {
              logger.debug('Token refresh successful, retrying request')
              originalRequest.headers.Authorization = `Bearer ${this.getToken()}`
              return this.client(originalRequest)
            } else {
              logger.warn('Token refresh failed, request will fail')
            }
          } catch (refreshError) {
            logger.warn('Token refresh failed in interceptor:', refreshError)
            this.clearStorage()

            // Dispatch custom event to notify AuthContext about refresh error
            window.dispatchEvent(new CustomEvent('authRefreshError', {
              detail: { hasError: true }
            }))

            // Don't force navigation here - let React Router handle it
            // The auth context will detect the missing token and redirect appropriately
          }
        }

        return Promise.reject(error)
      }
    )
  }

  async login(credentials: LoginRequest): Promise<LoginResponse> {
    logger.debug('Login attempt with credentials:', { email: credentials.email })

    const response = await this.client.post<LoginResponse>('/login', credentials)
    const loginData = response.data

    logger.debug('Login response received:', {
      hasToken: !!loginData.token,
      hasRefreshToken: !!loginData.refreshToken,
      hasUser: !!loginData.user,
      user: loginData.user
    })

    // NOTE: Do NOT store tokens here - let AuthContext handle storage
    // This prevents double storage and race conditions
    logger.debug('Login successful, returning data to AuthContext for storage')

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
      const refreshToken = this.getRefreshToken()
      logger.debug('Logout attempt with:', { hasRefreshToken: !!refreshToken })

      if (refreshToken) {
        await this.client.post('/logout', {
          RefreshToken: refreshToken
        })
        logger.debug('Logout request successful')
      } else {
        logger.warn('No refresh token for logout request')
      }
    } catch (error) {
      // Continue with logout even if server request fails
      logger.warn('Logout request failed:', error)
    } finally {
      this.clearStorage()
    }
  }

  async logoutAllDevices(): Promise<void> {
    try {
      logger.debug('Logout from all devices attempt')
      await this.client.post('/logout-all-devices')
      logger.debug('Logout from all devices request successful')
    } catch (error) {
      // Continue with logout even if server request fails
      logger.warn('Logout from all devices request failed:', error)
    } finally {
      this.clearStorage()
    }
  }

  async refreshToken(): Promise<boolean> {
    try {
      const refreshToken = this.getRefreshToken()
      logger.debug('Attempting token refresh with:', { hasRefreshToken: !!refreshToken })

      if (!refreshToken) {
        logger.warn('No refresh token available')
        return false
      }

      logger.debug('Calling POST /refresh endpoint...')
      const requestPayload = { RefreshToken: refreshToken }
      logger.debug('Request payload:', requestPayload)
      const response = await this.client.post<LoginResponse>('/refresh', requestPayload)

      logger.debug('Refresh successful, response received')
      const tokenData = response.data
      this.setTokens(tokenData.token, tokenData.refreshToken)
      return true
    } catch (error: any) {
      logger.error('Token refresh failed:', {
        status: error.response?.status,
        statusText: error.response?.statusText,
        data: error.response?.data,
        message: error.message
      })
      this.clearStorage()
      return false
    }
  }

  async getCurrentUser(): Promise<User | null> {
    try {
      logger.debug('Getting current user from server')
      const response = await this.client.get<User>('/me')
      logger.debug('Full response from /me:', {
        status: response.status,
        statusText: response.statusText,
        data: response.data
      })
      const user = response.data
      logger.debug('Current user received:', user)
      this.setUser(user)
      return user
    } catch (error: any) {
      logger.warn('Failed to get current user:', {
        status: error.response?.status,
        statusText: error.response?.statusText,
        data: error.response?.data,
        message: error.message
      })
      return null
    }
  }

  async requestPasswordReset(email: string, uiUrl: string): Promise<void> {
    await this.client.post<ApiResponse<void>>('/forgot-password', { email, uiUrl })
  }

  async confirmPasswordReset(token: string, newPassword: string, confirmPassword: string): Promise<void> {
    await this.client.post<ApiResponse<void>>('/reset-password', {
      Token: token,
      NewPassword: newPassword,
      ConfirmNewPassword: confirmPassword
    })
  }

  async changePassword(request: ChangePasswordRequest): Promise<void> {
    await this.client.post<ApiResponse<void>>('/change-password', request)
  }

  async getUsers(): Promise<User[]> {
    const response = await this.client.get<User[]>('/users')
    return response.data
  }

  async assignRole(userId: string, roleName: string): Promise<void> {
    await this.client.post<ApiResponse<void>>('/assign-role', { userId, roleName })
  }

  async removeRole(userId: string, roleName: string): Promise<void> {
    await this.client.post<ApiResponse<void>>('/remove-role', { userId, roleName })
  }

  async setPassword(userId: string, newPassword: string): Promise<void> {
    await this.client.post<ApiResponse<void>>('/set-password', { userId, newPassword })
  }

  async getRoles(): Promise<string[]> {
    const response = await this.client.get<string[]>('/roles')
    return response.data
  }

  // Token management with proper storage handling
  getToken(): string | null {
    try {
      const token = localStorage.getItem(STORAGE_KEYS.AUTH_TOKEN)
      logger.debug('Retrieved token from localStorage:', { hasToken: !!token })
      return token
    } catch (error) {
      logger.warn('Failed to access localStorage for token:', error)
      return null
    }
  }

  getRefreshToken(): string | null {
    try {
      const refreshToken = localStorage.getItem(STORAGE_KEYS.REFRESH_TOKEN)
      logger.debug('Retrieved refresh token from localStorage:', { hasRefreshToken: !!refreshToken })
      return refreshToken
    } catch (error) {
      logger.warn('Failed to access localStorage for refresh token:', error)
      return null
    }
  }

  getUser(): User | null {
    try {
      const userData = localStorage.getItem(STORAGE_KEYS.USER)
      const user = userData ? JSON.parse(userData) : null
      logger.debug('Retrieved user from localStorage:', { hasUser: !!user, user })
      return user
    } catch (error) {
      logger.warn('Failed to parse user data from localStorage:', error)
      return null
    }
  }

  isAuthenticated(): boolean {
    const token = this.getToken()
    logger.debug('Checking authentication:', { hasToken: !!token })

    if (!token) {
      logger.debug('No token found')
      return false
    }

    try {
      // Validate token expiry
      const payload = JSON.parse(atob(token.split('.')[1]))
      const isExpired = payload.exp * 1000 <= Date.now()

      logger.debug('Token validation:', {
        exp: payload.exp,
        expTime: new Date(payload.exp * 1000),
        now: new Date(),
        isExpired
      })

      if (isExpired) {
        // Token expired, clear storage
        logger.warn('Token expired, clearing storage')
        this.clearStorage()
        return false
      }

      logger.debug('Token is valid')
      return true
    } catch (error) {
      logger.warn('Invalid token format:', error)
      this.clearStorage()
      return false
    }
  }

  // Check if token is about to expire (within 5 minutes)
  isTokenExpiringSoon(): boolean {
    const token = this.getToken()
    if (!token) return false

    try {
      const payload = JSON.parse(atob(token.split('.')[1]))
      const expiryTime = payload.exp * 1000
      const fiveMinutesFromNow = Date.now() + (5 * 60 * 1000)

      return expiryTime <= fiveMinutesFromNow
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
    try {
      logger.debug('Storing tokens in localStorage')
      localStorage.setItem(STORAGE_KEYS.AUTH_TOKEN, accessToken)
      localStorage.setItem(STORAGE_KEYS.REFRESH_TOKEN, refreshToken)
      // Dispatch storage event for cross-tab synchronization
      window.dispatchEvent(new StorageEvent('storage', {
        key: STORAGE_KEYS.AUTH_TOKEN,
        newValue: accessToken,
        storageArea: localStorage
      }))
      logger.debug('Tokens stored successfully')
    } catch (error) {
      logger.error('Failed to store tokens:', error)
    }
  }

  private setUser(user: User): void {
    try {
      logger.debug('Storing user data in localStorage:', user)
      localStorage.setItem(STORAGE_KEYS.USER, JSON.stringify(user))
    } catch (error) {
      logger.error('Failed to store user data:', error)
    }
  }

  private clearStorage(): void {
    try {
      logger.debug('Clearing auth storage')
      localStorage.removeItem(STORAGE_KEYS.AUTH_TOKEN)
      localStorage.removeItem(STORAGE_KEYS.REFRESH_TOKEN)
      localStorage.removeItem(STORAGE_KEYS.USER)
      // Dispatch storage event for cross-tab synchronization
      window.dispatchEvent(new StorageEvent('storage', {
        key: STORAGE_KEYS.AUTH_TOKEN,
        newValue: null,
        storageArea: localStorage
      }))
      logger.debug('Auth storage cleared')
    } catch (error) {
      logger.error('Failed to clear storage:', error)
    }
  }
}

export const authService = new AuthService()
export default authService
