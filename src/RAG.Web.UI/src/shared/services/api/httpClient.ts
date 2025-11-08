/**
 * Centralized HTTP client factory
 * Provides consistent axios configuration with interceptors
 */

import axios, { AxiosInstance, AxiosRequestConfig, AxiosError } from 'axios'
import { API_TIMEOUTS, STORAGE_KEYS } from '@/app/config/appConfig'
import { logger } from '@/utils/logger'

export interface HttpClientConfig extends AxiosRequestConfig {
  baseURL: string
  timeout?: number
  requireAuth?: boolean
}

/**
 * Creates a configured HTTP client instance
 */
export function createHttpClient(config: HttpClientConfig): AxiosInstance {
  const {
    baseURL,
    timeout = API_TIMEOUTS.DEFAULT,
    requireAuth = true,
    ...axiosConfig
  } = config

  const client = axios.create({
    baseURL,
    timeout,
    headers: {
      'Content-Type': 'application/json',
    },
    ...axiosConfig,
  })

  // Request interceptor for authentication
  if (requireAuth) {
    client.interceptors.request.use(
      (config) => {
        const token = localStorage.getItem(STORAGE_KEYS.AUTH_TOKEN)
        if (token) {
          config.headers.Authorization = `Bearer ${token}`
        }
        return config
      },
      (error) => {
        logger.error('Request interceptor error:', error)
        return Promise.reject(error)
      }
    )
  }

  // Response interceptor for error handling
  client.interceptors.response.use(
    (response) => response,
    (error: AxiosError) => {
      // Handle 401 Unauthorized
      if (error.response?.status === 401) {
        localStorage.removeItem(STORAGE_KEYS.AUTH_TOKEN)
        localStorage.removeItem(STORAGE_KEYS.REFRESH_TOKEN)
        localStorage.removeItem(STORAGE_KEYS.USER)
        
        // Only redirect if not already on login page
        if (window.location.pathname !== '/login') {
          logger.warn('Unauthorized access detected, redirecting to login')
          // Note: Actual redirect should be handled by AuthContext or router
        }
      }

      // Log error details in development
      if (import.meta.env.DEV) {
        logger.error('HTTP request failed:', {
          url: error.config?.url,
          method: error.config?.method,
          status: error.response?.status,
          data: error.response?.data,
        })
      }

      return Promise.reject(error)
    }
  )

  return client
}

/**
 * Creates a client without authentication requirements
 */
export function createPublicHttpClient(baseURL: string, timeout?: number): AxiosInstance {
  return createHttpClient({
    baseURL,
    timeout: timeout || API_TIMEOUTS.DEFAULT,
    requireAuth: false,
  })
}

