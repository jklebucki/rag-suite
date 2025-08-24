import axios, { AxiosInstance, AxiosResponse } from 'axios'
import type {
  ApiResponse,
  SearchQuery,
  SearchResponse,
  ChatRequest,
  ChatMessage,
  ChatSession,
  PluginInfo,
  UsageStats,
  PerformanceMetrics
} from '@/types'

class ApiClient {
  private client: AxiosInstance

  constructor() {
    this.client = axios.create({
      baseURL: '/api',
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
      },
    })

    // Request interceptor for auth token
    this.client.interceptors.request.use((config) => {
      const token = localStorage.getItem('auth_token')
      if (token) {
        config.headers.Authorization = `Bearer ${token}`
      }
      return config
    })

  // Response interceptor for error handling
    this.client.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          localStorage.removeItem('auth_token')
          // TODO: Implement proper authentication when auth endpoints are added to API
          // window.location.href = '/login'
        }
        return Promise.reject(error)
      }
    )
  }

  // TODO: Authentication endpoints - to be implemented when added to .NET API
  // async login(credentials: LoginRequest): Promise<LoginResponse>
  // async logout(): Promise<void>

  // Search
  async search(query: SearchQuery): Promise<SearchResponse> {
    const response: AxiosResponse<ApiResponse<SearchResponse>> = await this.client.post('/search', query)
    return response.data.data
  }

  // Chat
  async sendMessage(sessionId: string, request: ChatRequest): Promise<ChatMessage> {
    const response: AxiosResponse<ApiResponse<ChatMessage>> = await this.client.post(`/chat/sessions/${sessionId}/messages`, request)
    return response.data.data
  }

  async getChatSessions(): Promise<ChatSession[]> {
    const response: AxiosResponse<ApiResponse<ChatSession[]>> = await this.client.get('/chat/sessions')
    return response.data.data
  }

  async getChatSession(sessionId: string): Promise<ChatSession> {
    const response: AxiosResponse<ApiResponse<ChatSession>> = await this.client.get(`/chat/sessions/${sessionId}`)
    return response.data.data
  }

  async createChatSession(title?: string): Promise<ChatSession> {
    const response: AxiosResponse<ApiResponse<ChatSession>> = await this.client.post('/chat/sessions', { title })
    return response.data.data
  }

  async deleteChatSession(sessionId: string): Promise<void> {
    await this.client.delete(`/chat/sessions/${sessionId}`)
  }

  // Plugins
  async getPlugins(): Promise<PluginInfo[]> {
    const response: AxiosResponse<ApiResponse<PluginInfo[]>> = await this.client.get('/plugins')
    return response.data.data
  }

  async getPlugin(pluginId: string): Promise<PluginInfo> {
    const response: AxiosResponse<ApiResponse<PluginInfo>> = await this.client.get(`/plugins/${pluginId}`)
    return response.data.data
  }

  async togglePlugin(pluginId: string, enabled: boolean): Promise<void> {
    if (enabled) {
      await this.client.post(`/plugins/${pluginId}/enable`)
    } else {
      await this.client.post(`/plugins/${pluginId}/disable`)
    }
  }

  // Analytics
  async getUsageStats(filters?: {
    startDate?: Date
    endDate?: Date
    endpoint?: string
  }): Promise<UsageStats> {
    const params = new URLSearchParams()
    if (filters?.startDate) params.append('startDate', filters.startDate.toISOString())
    if (filters?.endDate) params.append('endDate', filters.endDate.toISOString())
    if (filters?.endpoint) params.append('endpoint', filters.endpoint)
    
    const response: AxiosResponse<ApiResponse<UsageStats>> = await this.client.get(`/analytics/usage?${params}`)
    return response.data.data
  }

  async getPerformanceMetrics(filters?: {
    startDate?: Date
    endDate?: Date
    endpoint?: string
  }): Promise<PerformanceMetrics[]> {
    const params = new URLSearchParams()
    if (filters?.startDate) params.append('startDate', filters.startDate.toISOString())
    if (filters?.endDate) params.append('endDate', filters.endDate.toISOString())
    if (filters?.endpoint) params.append('endpoint', filters.endpoint)
    
    const response: AxiosResponse<ApiResponse<PerformanceMetrics[]>> = await this.client.get(`/analytics/performance?${params}`)
    return response.data.data
  }

  // Health check
  async healthCheck(): Promise<{ status: string; timestamp: Date }> {
    const response: AxiosResponse<ApiResponse<{ status: string; timestamp: Date }>> = await this.client.get('/health')
    return response.data.data
  }
}

export const apiClient = new ApiClient()
export default apiClient
