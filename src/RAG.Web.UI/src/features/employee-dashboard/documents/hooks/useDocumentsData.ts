import { useCallback, useEffect, useMemo, useState } from 'react'
import { useAuth } from '@/shared/contexts/AuthContext'
import {
  downloadDocumentFile,
  getDocumentsPageData,
  saveDocumentAuditLog,
} from '../services/documentsMockData'
import type { DocumentsPageData, EmployeeDocument } from '../types/documentsTypes'

interface UseDocumentsDataResult {
  data: DocumentsPageData | null
  selectedDocument: EmployeeDocument | null
  selectedDocumentId: string | null
  isLoading: boolean
  isDownloading: boolean
  error: string | null
  downloadMessage: string | null
  selectDocument: (document: EmployeeDocument) => Promise<void>
  downloadDocument: (documentId: string) => Promise<void>
  clearDownloadMessage: () => void
}

export function useDocumentsData(): UseDocumentsDataResult {
  const { user } = useAuth()
  const [data, setData] = useState<DocumentsPageData | null>(null)
  const [selectedDocumentId, setSelectedDocumentId] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isDownloading, setIsDownloading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [downloadMessage, setDownloadMessage] = useState<string | null>(null)

  useEffect(() => {
    if (!user) return

    let cancelled = false
    setIsLoading(true)
    setError(null)

    getDocumentsPageData(user.id)
      .then((result) => {
        if (cancelled) return

        setData(result)
        setSelectedDocumentId(result.documents[0]?.id ?? null)
        setIsLoading(false)
      })
      .catch(() => {
        if (!cancelled) {
          setError('employeeDashboard.documents.error.loadFailed')
          setIsLoading(false)
        }
      })

    return () => {
      cancelled = true
    }
  }, [user])

  const selectedDocument = useMemo(() => {
    if (!data) return null
    return data.documents.find((document) => document.id === selectedDocumentId) ?? data.documents[0] ?? null
  }, [data, selectedDocumentId])

  const appendLog = useCallback((log: DocumentsPageData['downloadLogs'][number]) => {
    setData((current) => {
      if (!current) return current
      return {
        ...current,
        downloadLogs: [log, ...current.downloadLogs],
      }
    })
  }, [])

  const auditUserName = user?.fullName || user?.userName || user?.email || 'Current user'

  const selectDocument = useCallback(
    async (document: EmployeeDocument) => {
      setSelectedDocumentId(document.id)

      if (!user || !data) return

      const category = data.categories.find((item) => item.id === document.categoryId)
      if (!category) return

      const log = await saveDocumentAuditLog(
        user.id,
        document,
        category,
        'preview',
        auditUserName
      )
      appendLog(log)
    },
    [appendLog, auditUserName, data, user]
  )

  const downloadDocument = useCallback(
    async (documentId: string) => {
      if (!user || !data) return

      const document = data.documents.find((item) => item.id === documentId)
      if (!document) return

      const category = data.categories.find((item) => item.id === document.categoryId)
      if (!category) return

      setIsDownloading(true)
      setDownloadMessage(null)

      try {
        await downloadDocumentFile(user.id, documentId)
        const log = await saveDocumentAuditLog(
          user.id,
          document,
          category,
          'download',
          auditUserName
        )
        appendLog(log)
        setDownloadMessage('employeeDashboard.documents.download.backendPlaceholder')
      } finally {
        setIsDownloading(false)
      }
    },
    [appendLog, auditUserName, data, user]
  )

  return {
    data,
    selectedDocument,
    selectedDocumentId,
    isLoading,
    isDownloading,
    error,
    downloadMessage,
    selectDocument,
    downloadDocument,
    clearDownloadMessage: () => setDownloadMessage(null),
  }
}
