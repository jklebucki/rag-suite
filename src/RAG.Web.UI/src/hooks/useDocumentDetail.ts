import { useQuery } from '@tanstack/react-query'
import { apiClient } from '@/services/api'
import { CACHE_CONFIG } from '@/constants/config'
import type { DocumentDetailResponse } from '@/types'

export function useDocumentDetail(documentId: string | null) {
  return useQuery<DocumentDetailResponse>({
    queryKey: ['document', documentId],
    queryFn: () => {
      if (!documentId) throw new Error('Document ID is required')
      return apiClient.getDocumentDetails(documentId)
    },
    enabled: !!documentId,
    staleTime: CACHE_CONFIG.DOCUMENT_DETAIL.STALE_TIME,
    cacheTime: CACHE_CONFIG.DOCUMENT_DETAIL.CACHE_TIME,
  })
}
