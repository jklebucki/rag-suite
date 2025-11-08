import { AxiosInstance, AxiosResponse } from 'axios'
import { createHttpClient, createPublicHttpClient } from '@/utils/httpClient'
import { logger } from '@/utils/logger'
import { API_TIMEOUTS, API_ENDPOINTS } from '@/constants/config'
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
  ListAttemptsResponse,
  GetAttemptByIdResponse,
  DeleteQuizResponse
} from '@/types'
import type {
  ListContactsRequest,
  ListContactsResponse,
  CreateContactRequest,
  CreateContactResponse,
  UpdateContactRequest,
  UpdateContactResponse,
  SearchContactsResponse,
  Contact,
  ProposeChangeRequest,
  ProposeChangeResponse,
  ListProposalsRequest,
  ListProposalsResponse,
  ContactChangeProposal,
  ReviewProposalRequest,
  ReviewProposalResponse,
  ImportContactsRequest,
  ImportContactsResponse
} from '@/features/address-book/types/addressbook'

class ApiClient {
  private client: AxiosInstance
  private healthClient: AxiosInstance

  constructor() {
    // Main API client with chat timeout
    this.client = createHttpClient({
      baseURL: API_ENDPOINTS.BASE,
      timeout: API_TIMEOUTS.CHAT,
      requireAuth: true,
    })

    // Separate client for health endpoints with SHORT timeout (no auth required)
    this.healthClient = createPublicHttpClient('', API_TIMEOUTS.HEALTH)
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
      filters: query.filters as SearchQuery['filters']
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
    logger.debug(`Calling API: POST /api/user-chat/sessions/${sessionId}/messages/multilingual`, request)
    try {
      // Uses main client timeout (15 minutes configured in constructor)
      const response: AxiosResponse<ApiResponse<MultilingualChatResponse>> = await this.client.post(
        `/user-chat/sessions/${sessionId}/messages/multilingual`,
        request
      )
      logger.debug('API response received:', response.data)
      return response.data.data
    } catch (error) {
      logger.error('API call failed:', error)
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
   * @param language Optional language filter for published quizzes
   */
  async listQuizzes(language?: string): Promise<ListQuizzesResponse> {
    const params = language ? { language } : {}
    const response = await this.client.get('/cyberpanel/quizzes', { params })
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
   * Delete a quiz by ID (returns deletion summary)
   */
  async deleteQuiz(quizId: string): Promise<DeleteQuizResponse> {
    const response = await this.client.delete(`/cyberpanel/quizzes/${quizId}`)
    return response.data
  }

  /**
   * Delete a quiz attempt by ID (Admin/PowerUser only)
   */
  async deleteAttempt(attemptId: string): Promise<void> {
    await this.client.delete(`/cyberpanel/quizzes/attempts/${attemptId}`)
  }

  /**
   * Update/Edit an existing quiz
   */
  async updateQuiz(quizId: string, request: CreateQuizRequest): Promise<CreateQuizResponse> {
    const response = await this.client.put(`/cyberpanel/quizzes/${quizId}`, request)
    return response.data
  }

  /**
   * List all quiz attempts/results for current user (or all if Admin/PowerUser)
   */
  async listQuizAttempts(): Promise<ListAttemptsResponse> {
    const response = await this.client.get('/cyberpanel/quizzes/attempts')
    return response.data
  }

  /**
   * Get detailed results for a specific quiz attempt
   */
  async getAttemptById(attemptId: string): Promise<GetAttemptByIdResponse> {
    const response = await this.client.get(`/cyberpanel/quizzes/attempts/${attemptId}`)
    return response.data
  }

  // AddressBook Contact API methods

  /**
   * List all contacts with pagination and filtering
   */
  async listContacts(params?: ListContactsRequest): Promise<ListContactsResponse> {
    const queryParams = new URLSearchParams()
    // includeInactive is required by backend, default to false
    queryParams.append('includeInactive', params?.includeInactive ? 'true' : 'false')
    if (params?.department) queryParams.append('department', params.department)
    if (params?.location) queryParams.append('location', params.location)

    const response = await this.client.get(`/addressbook?${queryParams}`)
    return response.data
  }

  /**
   * Get a specific contact by ID
   */
  async getContact(contactId: string): Promise<Contact> {
    const response = await this.client.get(`/addressbook/${contactId}`)
    return response.data
  }

  /**
   * Search contacts by query
   */
  async searchContacts(query?: string, includeInactive?: boolean): Promise<SearchContactsResponse> {
    const queryParams = new URLSearchParams()
    if (query) queryParams.append('query', query)
    if (includeInactive) queryParams.append('includeInactive', 'true')

    const response = await this.client.get(`/addressbook/search?${queryParams}`)
    return response.data
  }

  /**
   * Create a new contact
   */
  async createContact(request: CreateContactRequest): Promise<CreateContactResponse> {
    const response = await this.client.post('/addressbook', request)
    return response.data
  }

  /**
   * Update an existing contact
   */
  async updateContact(contactId: string, request: UpdateContactRequest): Promise<UpdateContactResponse> {
    const response = await this.client.put(`/addressbook/${contactId}`, request)
    return response.data
  }

  /**
   * Delete a contact
   */
  async deleteContact(contactId: string): Promise<void> {
    await this.client.delete(`/addressbook/${contactId}`)
  }

  /**
   * Propose a change to a contact
   */
  async proposeChange(request: ProposeChangeRequest): Promise<ProposeChangeResponse> {
    const response = await this.client.post('/addressbook/proposals', request)
    return response.data
  }

  /**
   * List all proposals with filtering
   */
  async listProposals(params?: ListProposalsRequest): Promise<ListProposalsResponse> {
    const queryParams = new URLSearchParams()
    if (params?.status !== undefined && params.status !== null) queryParams.append('status', params.status.toString())
    if (params?.proposalType !== undefined && params.proposalType !== null) queryParams.append('proposalType', params.proposalType.toString())
    if (params?.proposedByUserId) queryParams.append('proposedByUserId', params.proposedByUserId)

    const url = queryParams.toString() ? `/addressbook/proposals?${queryParams}` : '/addressbook/proposals'
    const response = await this.client.get(url)
    return response.data
  }

  /**
   * Get a specific proposal by ID
   */
  async getProposal(proposalId: string): Promise<ContactChangeProposal> {
    const response = await this.client.get(`/addressbook/proposals/${proposalId}`)
    return response.data
  }

  /**
   * Review (approve/reject) a proposal
   */
  async reviewProposal(proposalId: string, request: ReviewProposalRequest): Promise<ReviewProposalResponse> {
    const response = await this.client.post(`/addressbook/proposals/${proposalId}/review`, request)
    return response.data
  }

  /**
   * Import contacts from data
   */
  async importContacts(request: ImportContactsRequest): Promise<ImportContactsResponse> {
    const response = await this.client.post('/addressbook/import', request)
    return response.data
  }
}

export const apiClient = new ApiClient()
export default apiClient
