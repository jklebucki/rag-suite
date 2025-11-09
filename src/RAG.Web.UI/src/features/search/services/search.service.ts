import type { ApiResponse } from '@/shared/types/api'
import type {
  SearchQuery,
  SearchResponse,
  MultilingualSearchQuery,
  DocumentDetailResponse,
} from '@/features/search/types/search'
import { apiHttpClient } from '@/shared/services/api/httpClients'

type RequestOptions = {
  signal?: AbortSignal
}

export async function search(query: SearchQuery, options: RequestOptions = {}): Promise<SearchResponse> {
  const response = await apiHttpClient.post<ApiResponse<SearchResponse>>('/search', query, {
    signal: options.signal,
  })
  return response.data.data
}

export async function searchMultilingual(
  query: MultilingualSearchQuery,
  options: RequestOptions = {},
): Promise<SearchResponse> {
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

  const response = await apiHttpClient.post<ApiResponse<SearchResponse>>('/search', searchQuery, {
    headers,
    signal: options.signal,
  })
  return response.data.data
}

export async function getDocumentDetails(
  documentId: string,
  options: RequestOptions = {},
): Promise<DocumentDetailResponse> {
  const response = await apiHttpClient.get<ApiResponse<DocumentDetailResponse>>(`/search/documents/${documentId}`, {
    signal: options.signal,
  })
  return response.data.data
}

const searchService = {
  search,
  searchMultilingual,
  getDocumentDetails,
}

export default searchService
