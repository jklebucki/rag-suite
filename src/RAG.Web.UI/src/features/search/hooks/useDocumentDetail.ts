import { useQuery } from '@tanstack/react-query'
import searchService from '@/features/search/services/search.service'
import { CACHE_CONFIG } from '@/app/config/appConfig'
import type { DocumentDetailResponse } from '@/features/search/types/search'

export function useDocumentDetail(documentId: string | null) {
  return useQuery<DocumentDetailResponse>({
    queryKey: ['document', documentId],
    queryFn: () => {
      if (!documentId) throw new Error('Document ID is required')
      return searchService.getDocumentDetails(documentId)
    },
    enabled: !!documentId,
    staleTime: CACHE_CONFIG.DOCUMENT_DETAIL.STALE_TIME,
    cacheTime: CACHE_CONFIG.DOCUMENT_DETAIL.CACHE_TIME,
  })
}
