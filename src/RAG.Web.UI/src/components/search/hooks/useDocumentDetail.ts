import { useQuery } from '@tanstack/react-query'
import { apiClient } from '@/services/api'
import type { DocumentDetailResponse } from '@/types'

export function useDocumentDetail(documentId: string | null) {
  return useQuery<DocumentDetailResponse>({
    queryKey: ['document', documentId],
    queryFn: () => {
      if (!documentId) throw new Error('Document ID is required')
      return apiClient.getDocumentDetails(documentId)
    },
    enabled: !!documentId,
    staleTime: 1000 * 60 * 5, // 5 minutes
    cacheTime: 1000 * 60 * 30 // 30 minutes
  })
}
