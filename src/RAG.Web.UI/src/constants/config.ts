/**
 * Application configuration constants
 * Centralized place for magic numbers and configuration values
 */

/**
 * API timeout configurations (in milliseconds)
 */
export const API_TIMEOUTS = {
  DEFAULT: 30000, // 30 seconds
  CHAT: 900000, // 15 minutes (matches backend Chat:RequestTimeoutMinutes)
  HEALTH: 5000, // 5 seconds
  AUTH: 30000, // 30 seconds
} as const

/**
 * React Query cache configurations (in milliseconds)
 */
export const CACHE_TIMES = {
  STALE_TIME: 1000 * 60 * 5, // 5 minutes
  CACHE_TIME: 1000 * 60 * 10, // 10 minutes
} as const

/**
 * React Query retry configurations
 */
export const QUERY_RETRY = {
  QUERIES: 3, // Number of retries for queries
  MUTATIONS: 0, // No retry for mutations to prevent double sending
} as const

/**
 * Debounce delays (in milliseconds)
 */
export const DEBOUNCE_DELAYS = {
  SEARCH: 300,
  INPUT: 300,
} as const

/**
 * Pagination defaults
 */
export const PAGINATION = {
  DEFAULT_PAGE_SIZE: 10,
  MAX_PAGE_SIZE: 100,
} as const

/**
 * Toast notification defaults
 */
export const TOAST = {
  DEFAULT_DURATION: 5000, // 5 seconds
  AUTO_CLOSE: true,
} as const

/**
 * Local storage keys
 */
export const STORAGE_KEYS = {
  AUTH_TOKEN: 'auth_token',
  REFRESH_TOKEN: 'refresh_token',
  USER: 'user',
  LANGUAGE: 'language',
} as const

/**
 * API endpoints base paths
 */
export const API_ENDPOINTS = {
  BASE: '/api',
  AUTH: '/api/auth',
  SEARCH: '/api/search',
  CHAT: '/api/user-chat',
  DASHBOARD: '/api/dashboard',
  SETTINGS: '/api/settings',
  HEALTH: '/healthz',
} as const

