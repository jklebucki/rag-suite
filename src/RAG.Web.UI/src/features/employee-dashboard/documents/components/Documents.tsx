import { useMemo, useState } from 'react'
import { Building2, FileArchive, FolderOpen, Landmark, XCircle } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { TranslationKeys } from '@/shared/types/i18n'
import type { DocumentCategoryId } from '../types/documentsTypes'
import { useDocumentsData } from '../hooks/useDocumentsData'
import { DocumentCategoryCard } from './DocumentCategoryCard'
import { DocumentDownloadCard } from './DocumentDownloadCard'
import { DocumentList } from './DocumentList'
import { DocumentPreview } from './DocumentPreview'
import { DownloadLogTable } from './DownloadLogTable'

const categoryIcons = {
  hr: FileArchive,
  tax: Landmark,
  company: Building2,
}

export function Documents() {
  const { t } = useI18n()
  const {
    data,
    selectedDocument,
    selectedDocumentId,
    isLoading,
    isDownloading,
    error,
    downloadMessage,
    selectDocument,
    downloadDocument,
    clearDownloadMessage,
  } = useDocumentsData()
  const [selectedCategoryId, setSelectedCategoryId] = useState<DocumentCategoryId | 'all'>('all')

  const filteredDocuments = useMemo(() => {
    if (!data) return []
    if (selectedCategoryId === 'all') return data.documents
    return data.documents.filter((document) => document.categoryId === selectedCategoryId)
  }, [data, selectedCategoryId])

  const handleCategorySelect = (categoryId: DocumentCategoryId) => {
    if (!data) return

    const nextCategoryId = selectedCategoryId === categoryId ? 'all' : categoryId
    setSelectedCategoryId(nextCategoryId)

    const nextDocument = nextCategoryId === 'all'
      ? data.documents[0]
      : data.documents.find((document) => document.categoryId === nextCategoryId)

    if (nextDocument) {
      void selectDocument(nextDocument)
    }
  }

  if (isLoading) {
    return (
      <div className="flex h-48 items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary-200 border-t-primary-600" />
      </div>
    )
  }

  if (error || !data || !selectedDocument) {
    return (
      <div className="surface p-6 text-center text-red-600 dark:text-red-400">
        <XCircle className="mx-auto mb-2 h-8 w-8" />
        <p className="text-sm">
          {error ? t(error as keyof TranslationKeys) : t('employeeDashboard.documents.error.noData')}
        </p>
      </div>
    )
  }

  if (!data.canViewDocuments) {
    return (
      <div className="surface p-6 text-center text-gray-600 dark:text-gray-300">
        <FolderOpen className="mx-auto mb-2 h-8 w-8 text-gray-400" />
        <p className="text-sm">{t('employeeDashboard.documents.error.noAccess')}</p>
      </div>
    )
  }

  return (
    <div className="space-y-5 text-gray-900 dark:text-gray-100">
      <div className="flex items-center gap-3">
        <FolderOpen className="h-8 w-8 text-primary-600 dark:text-primary-400" />
        <div>
          <h1 className="text-2xl font-bold">{t('employeeDashboard.documents')}</h1>
          <p className="text-sm text-gray-500 dark:text-gray-400">
            {t('employeeDashboard.documents.subtitle')}
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        {data.categories.map((category) => (
          <DocumentCategoryCard
            key={category.id}
            category={category}
            icon={categoryIcons[category.id]}
            count={data.documents.filter((document) => document.categoryId === category.id).length}
            selected={selectedCategoryId === category.id}
            onSelect={handleCategorySelect}
          />
        ))}
      </div>

      <div className="grid grid-cols-1 items-start gap-5 2xl:grid-cols-[minmax(360px,0.85fr)_minmax(0,1.45fr)_minmax(320px,0.7fr)]">
        <DocumentList
          documents={filteredDocuments}
          categories={data.categories}
          selectedDocumentId={selectedDocumentId}
          onSelect={selectDocument}
        />

        <DocumentPreview
          document={selectedDocument}
          categories={data.categories}
        />

        <DocumentDownloadCard
          document={selectedDocument}
          isDownloading={isDownloading}
          canDownload={data.canDownloadDocuments}
          message={downloadMessage}
          onDownload={downloadDocument}
          onClearMessage={clearDownloadMessage}
        />
      </div>

      <DownloadLogTable logs={data.downloadLogs} />
    </div>
  )
}
