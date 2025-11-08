import { createHttpClient, createPublicHttpClient } from './httpClient'
import { API_ENDPOINTS, API_TIMEOUTS } from '@/app/config/appConfig'

export const apiHttpClient = createHttpClient({
  baseURL: API_ENDPOINTS.BASE,
  timeout: API_TIMEOUTS.CHAT,
  requireAuth: true,
})

export const healthHttpClient = createPublicHttpClient('', API_TIMEOUTS.HEALTH)
