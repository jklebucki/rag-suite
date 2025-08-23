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
  LoginRequest,
  LoginResponse
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
          window.location.href = '/login'
        }
        return Promise.reject(error)
      }
    )
  }

  // Authentication
  async login(credentials: LoginRequest): Promise<LoginResponse> {
    const response: AxiosResponse<ApiResponse<LoginResponse>> = await this.client.post('/auth/login', credentials)
    return response.data.data
  }

  async logout(): Promise<void> {
    await this.client.post('/auth/logout')
    localStorage.removeItem('auth_token')
  }

  // Search
  async search(query: SearchQuery): Promise<SearchResponse> {
    const response: AxiosResponse<ApiResponse<SearchResponse>> = await this.client.post('/search', query)
    return response.data.data
  }

  // Chat
  async sendMessage(request: ChatRequest): Promise<ChatMessage> {
    const response: AxiosResponse<ApiResponse<ChatMessage>> = await this.client.post('/chat/message', request)
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

  async togglePlugin(pluginId: string, enabled: boolean): Promise<void> {
    await this.client.patch(`/plugins/${pluginId}`, { enabled })
  }

  // Analytics
  async getUsageStats(): Promise<UsageStats> {
    const response: AxiosResponse<ApiResponse<UsageStats>> = await this.client.get('/analytics/usage')
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
