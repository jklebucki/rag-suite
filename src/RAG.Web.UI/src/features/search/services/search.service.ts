import type { ApiResponse } from '@/shared/types/api'
import type {
  SearchQuery,
  SearchResponse,
  MultilingualSearchQuery,
  DocumentDetailResponse,
} from '@/features/search/types/search'
import { apiHttpClient } from '@/shared/services/api/httpClients'

export async function search(query: SearchQuery): Promise<SearchResponse> {
  const response = await apiHttpClient.post<ApiResponse<SearchResponse>>('/search', query)
  return response.data.data
}

export async function searchMultilingual(query: MultilingualSearchQuery): Promise<SearchResponse> {
  const searchQuery: SearchQuery = {
    query: query.query,
    limit: query.maxResults || 10,
    filters: query.filters as SearchQuery['filters'],
  }

  const headers: Record<string, string> = {}
  if (query.language) {
    headers['Accept-Language'] = query.language
  }
  if (query.resultLanguage) {
    headers['X-Response-Language'] = query.resultLanguage
  }

  const response = await apiHttpClient.post<ApiResponse<SearchResponse>>('/search', searchQuery, { headers })
  return response.data.data
}

export async function getDocumentDetails(documentId: string): Promise<DocumentDetailResponse> {
  const response = await apiHttpClient.get<ApiResponse<DocumentDetailResponse>>(`/search/documents/${documentId}`)
  return response.data.data
}

const searchService = {
  search,
  searchMultilingual,
  getDocumentDetails,
}

export default searchService
