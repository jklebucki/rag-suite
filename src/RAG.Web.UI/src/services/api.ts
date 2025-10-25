import axios, { AxiosInstance, AxiosResponse } from 'axios'
import type {
  ApiResponse,
  SearchQuery,
  SearchResponse,
  MultilingualSearchQuery,
  DocumentDetailResponse,
  ChatRequest,
  ChatMessage,
  MultilingualChatRequest,
  MultilingualChatResponse,
  ChatSession,
  PluginInfo,
  UsageStats,
  PerformanceMetrics,
  SystemHealthResponse,
  ElasticsearchStats,
  IndexStats,
  NodeStats,
  SearchStatistics,
  SystemHealth,
  DashboardData,
  LlmSettings,
  LlmSettingsRequest,
  LlmSettingsResponse,
  AvailableModelsResponse,
  // CyberPanel types
  ListQuizzesResponse,
  GetQuizResponse,
  CreateQuizRequest,
  CreateQuizResponse,
  ExportQuizResponse,
  ImportQuizRequest,
  ImportQuizResponse,
  SubmitAttemptRequest,
  SubmitAttemptResponse,
  ListAttemptsResponse
} from '@/types'

class ApiClient {
  private client: AxiosInstance
  private healthClient: AxiosInstance

  constructor() {
    this.client = axios.create({
      baseURL: '/api',
      timeout: 900000, // 15 minutes for chat operations (matches backend Chat:RequestTimeoutMinutes)
      headers: {
        'Content-Type': 'application/json',
      },
    })

    // Separate client for health endpoints with SHORT timeout
    this.healthClient = axios.create({
      timeout: 5000, // 5 seconds for health checks
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

  // Multilingual Search (extends basic search with language context)
  async searchMultilingual(query: MultilingualSearchQuery): Promise<SearchResponse> {
    // For now, use basic search endpoint but include language in metadata
    const searchQuery: SearchQuery = {
      query: query.query,
      limit: query.maxResults || 10,
      filters: query.filters as any
    }

    // Add language headers for future API enhancement
    const headers: Record<string, string> = {}
    if (query.language) {
      headers['Accept-Language'] = query.language
    }
    if (query.resultLanguage) {
      headers['X-Response-Language'] = query.resultLanguage
    }

    const response: AxiosResponse<ApiResponse<SearchResponse>> = await this.client.post('/search', searchQuery, { headers })
    return response.data.data
  }

  async getDocumentDetails(documentId: string): Promise<DocumentDetailResponse> {
    const response: AxiosResponse<ApiResponse<DocumentDetailResponse>> = await this.client.get(`/search/documents/${documentId}`)
    return response.data.data
  }

  // Chat - Using user-specific endpoints for proper security
  async sendMessage(sessionId: string, request: ChatRequest): Promise<ChatMessage> {
    const response: AxiosResponse<ApiResponse<ChatMessage>> = await this.client.post(`/user-chat/sessions/${sessionId}/messages`, request)
    return response.data.data
  }

  // Multilingual Chat
  async sendMultilingualMessage(sessionId: string, request: MultilingualChatRequest): Promise<MultilingualChatResponse> {
    console.log(`Calling API: POST /api/user-chat/sessions/${sessionId}/messages/multilingual`, request)
    try {
      // Uses main client timeout (15 minutes configured in constructor)
      const response: AxiosResponse<ApiResponse<MultilingualChatResponse>> = await this.client.post(
        `/user-chat/sessions/${sessionId}/messages/multilingual`,
        request
      )
      console.log('API response received:', response.data)
      return response.data.data
    } catch (error) {
      console.error('API call failed:', error)
      throw error
    }
  }

  async getChatSessions(): Promise<ChatSession[]> {
    const response: AxiosResponse<ApiResponse<ChatSession[]>> = await this.client.get('/user-chat/sessions')
    return response.data.data
  }

  async getChatSession(sessionId: string): Promise<ChatSession> {
    const response: AxiosResponse<ApiResponse<ChatSession>> = await this.client.get(`/user-chat/sessions/${sessionId}`)
    return response.data.data
  }

  async createChatSession(title?: string, language?: string): Promise<ChatSession> {
    const response: AxiosResponse<ApiResponse<ChatSession>> = await this.client.post('/user-chat/sessions', { title, language })
    return response.data.data
  }

  async deleteChatSession(sessionId: string): Promise<void> {
    await this.client.delete(`/user-chat/sessions/${sessionId}`)
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

  // Enhanced Analytics - Elasticsearch Integration
  async getElasticsearchClusterStats(): Promise<ElasticsearchStats> {
    const response: AxiosResponse<ApiResponse<ElasticsearchStats>> = await this.client.get('/analytics/elasticsearch/cluster')
    return response.data.data
  }

  async getElasticsearchIndices(indexName?: string): Promise<IndexStats[]> {
    const url = indexName ? `/analytics/elasticsearch/indices/${indexName}` : '/analytics/elasticsearch/indices'
    const response: AxiosResponse<ApiResponse<IndexStats[]>> = await this.client.get(url)
    return response.data.data
  }

  async getElasticsearchNodes(): Promise<NodeStats[]> {
    const response: AxiosResponse<ApiResponse<NodeStats[]>> = await this.client.get('/analytics/elasticsearch/nodes')
    return response.data.data
  }

  async getSearchStatistics(): Promise<SearchStatistics> {
    const response: AxiosResponse<ApiResponse<SearchStatistics>> = await this.client.get('/analytics/search')
    return response.data.data
  }

  async getAnalyticsHealth(): Promise<SystemHealth> {
    const response: AxiosResponse<ApiResponse<SystemHealth>> = await this.client.get('/analytics/health')
    return response.data.data
  }

  async getDashboardData(includeDetailedStats = false): Promise<DashboardData> {
    const params = new URLSearchParams()
    if (includeDetailedStats) params.append('includeDetailedStats', 'true')

    const response: AxiosResponse<ApiResponse<DashboardData>> = await this.client.get(`/analytics/dashboard?${params}`)
    return response.data.data
  }

  async getAnalyticsStatus(): Promise<{ status: string; services: Record<string, boolean> }> {
    const response: AxiosResponse<ApiResponse<{ status: string; services: Record<string, boolean> }>> = await this.client.get('/analytics/status')
    return response.data.data
  }

  // Health check
  async healthCheck(): Promise<{ status: string; timestamp: Date }> {
    const response: AxiosResponse<ApiResponse<{ status: string; timestamp: Date }>> = await this.healthClient.get('/health')
    return response.data.data
  }

  async getSystemHealth(): Promise<SystemHealthResponse> {
    const response: AxiosResponse<ApiResponse<SystemHealthResponse>> = await this.healthClient.get('/healthz/system')
    return response.data.data
  }

  // File Download
  async downloadFile(filePath: string): Promise<void> {
    const response = await this.client.get(`/filedownload/${encodeURIComponent(filePath)}`, {
      responseType: 'blob'
    })

    // Create download link
    const url = window.URL.createObjectURL(new Blob([response.data]))
    const link = document.createElement('a')
    link.href = url
    link.setAttribute('download', filePath.split('/').pop() || 'download')
    document.body.appendChild(link)
    link.click()
    link.remove()
    window.URL.revokeObjectURL(url)
  }

  async downloadFileWithConversion(filePath: string, forceConvert: boolean = false): Promise<AxiosResponse<Blob>> {
    const params = new URLSearchParams()
    params.append('forceConvert', forceConvert.toString())

    return await this.client.get(`/filedownload/convert/${encodeURIComponent(filePath)}?${params.toString()}`, {
      responseType: 'blob'
    })
  }

  // Settings API methods
  async getLlmSettings(): Promise<LlmSettingsResponse> {
    const response = await this.client.get('/settings/llm')
    return response.data
  }

  async updateLlmSettings(settings: LlmSettingsRequest): Promise<{ message: string }> {
    const response = await this.client.put('/settings/llm', settings)
    return response.data
  }

  async getAvailableLlmModels(): Promise<AvailableModelsResponse> {
    const response = await this.client.get('/settings/llm/models')
    return response.data
  }

  async getAvailableLlmModelsFromUrl(url: string, isOllama: boolean): Promise<AvailableModelsResponse> {
    const params = new URLSearchParams({
      url: url,
      isOllama: isOllama.toString()
    })
    const response = await this.client.get(`/settings/llm/models?${params}`)
    return response.data
  }

  // CyberPanel Quiz API methods
  
  /**
   * List all quizzes
   */
  async listQuizzes(): Promise<ListQuizzesResponse> {
    const response = await this.client.get('/cyberpanel/quizzes')
    return response.data
  }

  /**
   * Get a specific quiz by ID (for quiz takers - without correct answers)
   */
  async getQuiz(quizId: string): Promise<GetQuizResponse> {
    const response = await this.client.get(`/cyberpanel/quizzes/${quizId}`)
    return response.data
  }

  /**
   * Create a new quiz
   */
  async createQuiz(request: CreateQuizRequest): Promise<CreateQuizResponse> {
    const response = await this.client.post('/cyberpanel/quizzes', request)
    return response.data
  }

  /**
   * Export quiz to JSON format (includes correct answers and metadata)
   */
  async exportQuiz(quizId: string): Promise<ExportQuizResponse> {
    const response = await this.client.get(`/cyberpanel/quizzes/${quizId}/export`)
    return response.data
  }

  /**
   * Import quiz from JSON (create new or overwrite existing)
   */
  async importQuiz(request: ImportQuizRequest): Promise<ImportQuizResponse> {
    const response = await this.client.post('/cyberpanel/quizzes/import', request)
    return response.data
  }

  /**
   * Submit quiz attempt and get results
   */
  async submitQuizAttempt(request: SubmitAttemptRequest): Promise<SubmitAttemptResponse> {
    const response = await this.client.post(`/cyberpanel/quizzes/${request.quizId}/attempts`, request)
    return response.data
  }

  /**
   * Delete a quiz by ID
   */
  async deleteQuiz(quizId: string): Promise<void> {
    await this.client.delete(`/cyberpanel/quizzes/${quizId}`)
  }

  /**
   * Update/Edit an existing quiz
   */
  async updateQuiz(quizId: string, request: CreateQuizRequest): Promise<CreateQuizResponse> {
    const response = await this.client.put(`/cyberpanel/quizzes/${quizId}`, request)
    return response.data
  }

  /**
   * List all quiz attempts/results for current user
   */
  async listQuizAttempts(): Promise<ListAttemptsResponse> {
    const response = await this.client.get('/cyberpanel/quizzes/attempts')
    return response.data
  }
}

export const apiClient = new ApiClient()
export default apiClient
