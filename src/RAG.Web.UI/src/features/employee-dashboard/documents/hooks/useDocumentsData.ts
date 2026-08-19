import { useCallback, useEffect, useState } from 'react'
import { useAuth } from '@/shared/contexts/AuthContext'
import {
  downloadPit11File,
  getPit11Documents,
} from '../services/documentsMockData'
import type { Pit11Document } from '../types/documentsTypes'

interface UseDocumentsDataResult {
  documents: Pit11Document[]
  isLoading: boolean
  downloadingDocumentId: string | null
  error: string | null
  downloadDocument: (documentId: string) => Promise<void>
}

export function useDocumentsData(): UseDocumentsDataResult {
  const { user } = useAuth()
  const [documents, setDocuments] = useState<Pit11Document[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [downloadingDocumentId, setDownloadingDocumentId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!user) return

    let cancelled = false
    setIsLoading(true)
    setError(null)

    getPit11Documents(user.id)
      .then((result) => {
        if (cancelled) return

        setDocuments(result)
        setIsLoading(false)
      })
      .catch(() => {
        if (!cancelled) {
          setError('employeeDashboard.pit11.error.loadFailed')
          setIsLoading(false)
        }
      })

    return () => {
      cancelled = true
    }
  }, [user])

  const downloadDocument = useCallback(
    async (documentId: string) => {
      if (!user) return

      setDownloadingDocumentId(documentId)

      try {
        const file = await downloadPit11File(user.id, documentId)
        const url = URL.createObjectURL(file.blob)
        const link = window.document.createElement('a')
        link.href = url
        link.download = file.fileName
        link.click()
        URL.revokeObjectURL(url)
      } finally {
        setDownloadingDocumentId(null)
      }
    },
    [user]
  )

  return {
    documents,
    isLoading,
    downloadingDocumentId,
    error,
    downloadDocument,
  }
}
