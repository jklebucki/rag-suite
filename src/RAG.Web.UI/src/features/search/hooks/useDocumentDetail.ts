import { useQuery } from '@tanstack/react-query'
import searchService from '@/features/search/services/search.service'
import { CACHE_CONFIG } from '@/app/config/appConfig'
import type { DocumentDetailResponse } from '@/features/search/types/search'

export function useDocumentDetail(documentId: string | null) {
  return useQuery<DocumentDetailResponse>({
    queryKey: ['document', documentId],
    queryFn: ({ signal }) => {
      if (!documentId) throw new Error('Document ID is required')
      return searchService.getDocumentDetails(documentId, { signal })
    },
    enabled: !!documentId,
    staleTime: CACHE_CONFIG.DOCUMENT_DETAIL.STALE_TIME,
    gcTime: CACHE_CONFIG.DOCUMENT_DETAIL.GC_TIME,
  })
}
