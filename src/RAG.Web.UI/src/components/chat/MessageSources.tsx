import React from 'react'
import { FileText, ExternalLink, Clock, Star, Folder, Download, Eye } from 'lucide-react'
import { useI18n } from '@/contexts/I18nContext'
import { formatDateTime, formatRelativeTime } from '@/utils/date'
import { apiClient } from '@/services/api'
import { Modal } from '@/components/ui/Modal'
import { DocumentDetail } from '@/components/search/DocumentDetail'
import { useDocumentDetail } from '@/hooks/useDocumentDetail'
import type { SearchResult } from '@/types/api'

// Lazy load PDFViewerModal
const PDFViewerModal = React.lazy(() => import('@/components/ui/PDFViewerModal').then(module => ({ default: module.PDFViewerModal })))

interface MessageSourcesProps {
  sources: SearchResult[]
  messageRole: 'user' | 'assistant' | 'system'
}

export function MessageSources({ sources, messageRole }: MessageSourcesProps) {
  const { t, language: currentLanguage } = useI18n()
  const [pdfViewerFilePath, setPdfViewerFilePath] = React.useState<string | null>(null)
  const [selectedDocumentId, setSelectedDocumentId] = React.useState<string | null>(null)
  const { data: documentDetail, isLoading: isLoadingDetail, error: detailError } = useDocumentDetail(selectedDocumentId)

  const renderError = (err: unknown) => {
    if (!err) return null
    return (
      <div className="p-6 bg-red-50 border border-red-200 rounded-lg m-6">
        <p className="text-red-700">{t('search.error')}</p>
      </div>
    )
  }

  const handleDownload = async (filePath: string) => {
    try {
      await apiClient.downloadFile(filePath)
    } catch (error) {
      console.error('Download failed:', error)
      // TODO: Show error toast
    }
  }

  if (!sources || sources.length === 0) {
    return null
  }

  return (
    <div className={`mt-3 p-3 rounded-lg border ${
      messageRole === 'user'
        ? 'bg-blue-50 border-blue-200'
        : 'bg-gray-50 border-gray-200'
    }`}>
      <div className="flex items-center gap-2 mb-2">
        <FileText className={`h-4 w-4 ${
          messageRole === 'user' ? 'text-blue-600' : 'text-gray-600'
        }`} />
        <span className={`text-sm font-medium ${
          messageRole === 'user' ? 'text-blue-800' : 'text-gray-700'
        }`}>
          {t('chat.sources.title', [sources.length.toString()])}
        </span>
      </div>

      <div className="space-y-2">
        {sources.map((source, index) => (
          <div
            key={`${source.id}-${index}`}
            className={`flex items-start gap-3 p-2 rounded border ${
              messageRole === 'user'
                ? 'bg-white border-blue-100 hover:border-blue-300'
                : 'bg-white border-gray-100 hover:border-gray-300'
            } transition-colors duration-200 cursor-pointer group`}
          >
            {/* Source Index */}
            <div className={`flex-shrink-0 w-6 h-6 rounded-full flex items-center justify-center text-xs font-medium ${
              messageRole === 'user'
                ? 'bg-blue-100 text-blue-700'
                : 'bg-gray-100 text-gray-700'
            }`}>
              {index + 1}
            </div>

            {/* Source Content */}
            <div className="flex-1 min-w-0">
              {/* Title and Score */}
              <div className="flex items-center justify-between mb-1">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-1.5">
                    <FileText className="h-3.5 w-3.5 text-gray-400 flex-shrink-0" />
                    <h4 className="text-sm font-medium text-gray-900 truncate">
                      {source.fileName || source.title || `Document ${source.title}`}
                    </h4>
                  </div>
                  {source.filePath && source.filePath !== source.fileName && (
                    <div className="flex items-center gap-1.5 mt-0.5">
                      <Folder className="h-3 w-3 text-gray-400 flex-shrink-0" />
                      <p className="text-xs text-gray-500 truncate">
                        {source.filePath.replace(/\/[^/]*$/, '') || source.filePath}
                      </p>
                    </div>
                  )}
                </div>
                <div className="flex items-center gap-1 ml-2 flex-shrink-0">
                  <Star className="h-3 w-3 text-yellow-500" />
                  <span className="text-xs text-gray-500">
                    {Math.round(source.score)}%
                  </span>
                </div>
              </div>

              {/* Content Preview */}
              <p className="text-xs text-gray-600 line-clamp-2 mb-2">
                {source.content.length > 120
                  ? `${source.content.substring(0, 120)}...`
                  : source.content
                }
              </p>

              {/* Metadata */}
              <div className="flex items-center justify-between text-xs text-gray-500">
                <div className="flex items-center gap-2 flex-wrap">
                  {source.documentType && (
                    <span className={`px-2 py-1 rounded-full ${
                      messageRole === 'user'
                        ? 'bg-blue-100 text-blue-700'
                        : 'bg-gray-100 text-gray-700'
                    }`}>
                      {source.documentType}
                    </span>
                  )}
                  {source.fileName && source.fileName !== source.source && (
                    <span>• {source.fileName}</span>
                  )}
                  {source.source && (
                    <span>• {source.source}</span>
                  )}
                </div>

                <div className="flex items-center gap-1 flex-shrink-0">
                  <Clock className="h-3 w-3" />
                  <span title={formatDateTime(source.createdAt, currentLanguage)}>
                    {formatRelativeTime(source.createdAt, currentLanguage)}
                  </span>
                </div>
              </div>
            </div>

            {/* External Link Icon */}
            <div className="flex-shrink-0 flex items-center gap-1">
              {source.filePath && (
                <>
                  <button
                    onClick={() => setPdfViewerFilePath(source.filePath!)}
                    className={`h-4 w-4 opacity-0 group-hover:opacity-100 transition-opacity hover:text-primary-600 ${
                      messageRole === 'user' ? 'text-blue-600' : 'text-gray-600'
                    }`}
                    title="View PDF"
                  >
                    <Eye className="h-4 w-4" />
                  </button>
                  <button
                    onClick={() => handleDownload(source.filePath!)}
                    className={`h-4 w-4 opacity-0 group-hover:opacity-100 transition-opacity hover:text-primary-600 ${
                      messageRole === 'user' ? 'text-blue-600' : 'text-gray-600'
                    }`}
                    title="Download file"
                  >
                    <Download className="h-4 w-4" />
                  </button>
                </>
              )}
              <button
                onClick={() => setSelectedDocumentId(source.id)}
                className={`h-4 w-4 opacity-0 group-hover:opacity-100 transition-opacity cursor-pointer hover:text-primary-600 ${
                  messageRole === 'user' ? 'text-blue-600' : 'text-gray-600'
                }`}
                title="View Details"
              >
                <ExternalLink className="h-4 w-4" />
              </button>
            </div>
          </div>
        ))}
      </div>

      {/* Summary */}
      {sources.length > 3 && (
        <div className={`mt-2 text-xs text-center ${
          messageRole === 'user' ? 'text-blue-600' : 'text-gray-500'
        }`}>
          {t('chat.sources.summary', [sources.length.toString()])}
        </div>
      )}

      {/* Document Detail Modal */}
      <Modal
        isOpen={!!selectedDocumentId}
        onClose={() => setSelectedDocumentId(null)}
        title="Document Details"
        size="xl"
      >
        {isLoadingDetail && (
          <div className="p-8 text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-500 mx-auto"></div>
            <p className="mt-4 text-gray-600">Loading document details...</p>
          </div>
        )}

        {renderError(detailError)}

        {documentDetail && (
          <DocumentDetail document={documentDetail} />
        )}
      </Modal>

      {/* PDF Viewer Modal */}
      <React.Suspense fallback={
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div className="bg-white rounded-lg shadow-xl p-8">
            <div className="flex items-center justify-center">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-500"></div>
              <span className="ml-2 text-gray-600">Loading PDF viewer...</span>
            </div>
          </div>
        </div>
      }>
        <PDFViewerModal
          isOpen={!!pdfViewerFilePath}
          onClose={() => setPdfViewerFilePath(null)}
          filePath={pdfViewerFilePath || ''}
          title="PDF Viewer"
        />
      </React.Suspense>
    </div>
  )
}
