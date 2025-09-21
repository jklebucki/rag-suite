import React, { useState, useEffect, Suspense } from 'react'
import { Download, Search, Eye } from 'lucide-react'
import { useI18n } from '@/contexts/I18nContext'
import { Modal } from '@/components/ui/Modal'
import { DocumentDetail } from './DocumentDetail'
import { useDocumentDetail } from './hooks/useDocumentDetail'
import { formatDate } from '@/utils/date'
import { apiClient } from '@/services/api'
import type { SearchResult } from '@/types'

// Lazy load PDFViewerModal
const PDFViewerModal = React.lazy(() => import('@/components/ui/PDFViewerModal').then(module => ({ default: module.PDFViewerModal })))

interface SearchResultsProps {
  searchResults?: {
    results: SearchResult[]
    total: number
    took: number
  }
  isLoading: boolean
  error: any
  hasSearched: boolean
  onExport: () => void
}

export function SearchResults({ searchResults, isLoading, error, hasSearched, onExport }: SearchResultsProps) {
  const { t, language } = useI18n()
  const [selectedDocumentId, setSelectedDocumentId] = useState<string | null>(null)
  const [pdfViewerFilePath, setPdfViewerFilePath] = useState<string | null>(null)
  const { data: documentDetail, isLoading: isLoadingDetail, error: detailError } = useDocumentDetail(selectedDocumentId)

  // Dodaj stan lokalny do przechowywania poprzednich wyników wyszukiwania
  const [previousResults, setPreviousResults] = useState<typeof searchResults | null>(null)

  // Aktualizuj previousResults tylko gdy nowe searchResults są dostępne
  useEffect(() => {
    if (searchResults) {
      setPreviousResults(searchResults)
    }
  }, [searchResults])

  console.log('🔍 SearchResults render:', { searchResults, isLoading, error, hasSearched })

  const renderError = (err: unknown) => {
    if (!err) return null
    return (
      <div className="p-6 bg-red-50 border border-red-200 rounded-lg m-6">
        <p className="text-red-700">{t('search.error')}</p>
      </div>
    )
  }

  // Spinner jak przy pierwszym wyszukiwaniu - przy każdym wyszukiwaniu gdy isLoading i hasSearched
  if (isLoading && hasSearched) {
    return (
      <div className="text-center py-8">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-500 mx-auto"></div>
        <p className="mt-4 text-gray-600">{t('search.loading')}</p>
      </div>
    )
  }

  if (error && hasSearched) {
    console.error('🔍 Search error in component:', error)
    return (
      <div className="bg-red-50 border border-red-200 rounded-lg p-4">
        <p className="text-red-700">{t('search.error')}</p>
        <details className="mt-2">
          <summary className="cursor-pointer text-sm">Error details</summary>
          <pre className="text-xs mt-1 text-red-600">{JSON.stringify(error, null, 2)}</pre>
        </details>
      </div>
    )
  }

  if (!hasSearched || (hasSearched && !searchResults && !isLoading)) {
    return (
      <div className="bg-white rounded-lg shadow-sm border p-8">
        <div className="text-center text-gray-500">
          <Search className="h-16 w-16 mx-auto mb-4 text-gray-300" />
          <h3 className="text-lg font-medium text-gray-900 mb-2">{t('search.title')}</h3>
          <p>{t('search.subtitle')}</p>
        </div>
      </div>
    )
  }

  // Użyj previousResults jeśli isLoading, w przeciwnym razie searchResults
  const resultsToShow = isLoading ? previousResults : searchResults

  if (!resultsToShow) {
    return null
  }

  return (
    <div className="bg-white rounded-lg shadow-sm border relative">
      {/* Dodaj overlay z spinnerem jeśli isLoading, hasSearched i są wyniki (np. przy zmianie inputu po wyszukaniu) */}
      {isLoading && hasSearched && searchResults && (
        <div className="absolute inset-0 bg-white bg-opacity-75 flex items-center justify-center z-10">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-500"></div>
        </div>
      )}
      <div className="p-6 border-b border-gray-200 flex justify-between items-center">
        <div>
          <h2 className="text-lg font-semibold text-gray-900">{t('search.results')}</h2>
          <p className="text-sm text-gray-600">
            {t('search.results')} {resultsToShow.total} {t('search.results')} in {resultsToShow.took}ms
          </p>
        </div>
        <button onClick={onExport} className="btn-secondary flex items-center gap-2">
          <Download className="h-4 w-4" />
          Export
        </button>
      </div>

      <div className="divide-y divide-gray-200">
        {resultsToShow.results.map((result) => (
          <SearchResultItem
            key={result.id}
            result={result}
            onViewDetails={() => setSelectedDocumentId(result.id)}
            onViewPDF={(filePath) => setPdfViewerFilePath(filePath)}
            language={language}
          />
        ))}
      </div>

      {resultsToShow.results.length === 0 && (
        <div className="p-8 text-center text-gray-500">
          <Search className="h-12 w-12 mx-auto mb-4 text-gray-300" />
          <p>{t('search.no_results')}</p>
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
      <Suspense fallback={
        <Modal
          isOpen={!!pdfViewerFilePath}
          onClose={() => setPdfViewerFilePath(null)}
          title="Loading PDF Viewer..."
        >
          <div className="flex items-center justify-center p-8">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-500"></div>
            <span className="ml-2 text-gray-600">Loading PDF viewer...</span>
          </div>
        </Modal>
      }>
        <PDFViewerModal
          isOpen={!!pdfViewerFilePath}
          onClose={() => setPdfViewerFilePath(null)}
          filePath={pdfViewerFilePath || ''}
          title="PDF Viewer"
        />
      </Suspense>
    </div>
  )
}

interface SearchResultItemProps {
  result: SearchResult
  onViewDetails: () => void
  onViewPDF?: (filePath: string) => void
  language: import('@/types/i18n').LanguageCode
}

function SearchResultItem({ result, onViewDetails, onViewPDF, language }: SearchResultItemProps) {
  const formatScore = (score: number) => Math.round(score)

  // Check if document was reconstructed from chunks
  const isReconstructed = result.metadata?.reconstructed
  const chunksInfo = result.metadata?.chunksFound && result.metadata?.totalChunks
    ? `${result.metadata.chunksFound}/${result.metadata.totalChunks} chunks`
    : null

  const handleDownload = async () => {
    if (result.filePath) {
      try {
        await apiClient.downloadFile(result.filePath)
      } catch (error) {
        console.error('Download failed:', error)
        // TODO: Show error toast
      }
    }
  }

  return (
    <div className="p-6 hover:bg-gray-50">
      <div className="flex justify-between items-start mb-2">
        <h3 className="text-lg font-medium text-gray-900 line-clamp-2">
          {result.title}
        </h3>
        <div className="ml-4 flex items-center gap-2">
          {isReconstructed && (
            <span className="px-2 py-1 bg-blue-100 text-blue-700 text-xs font-medium rounded-full">
              Reconstructed
            </span>
          )}
          <span className="px-2 py-1 bg-primary-100 text-primary-700 text-xs font-medium rounded-full">
            {formatScore(result.score)}% match
          </span>
        </div>
      </div>

      {/* Show highlights if available, otherwise show content */}
      <div className="text-gray-600 mb-3 line-clamp-3 search-highlights">
        {result.metadata?.highlights ? (
          <div dangerouslySetInnerHTML={{ __html: result.metadata.highlights }} />
        ) : (
          <p>{result.content}</p>
        )}
      </div>

      <div className="flex items-center justify-between text-sm text-gray-500">
        <div className="flex items-center space-x-4">
          <span className="px-2 py-1 bg-gray-100 rounded text-xs">
            {result.documentType}
          </span>
          <span>{result.source}</span>
          {chunksInfo && (
            <span className="px-2 py-1 bg-orange-100 text-orange-700 text-xs rounded">
              {chunksInfo}
            </span>
          )}
          {/* <span>{result.filePath}</span> */}
          <span>{formatDate(result.updatedAt, language)}</span>
        </div>
        <div className="flex items-center gap-2">
          {result.filePath && (
            <>
              <button
                onClick={() => onViewPDF?.(result.filePath!)}
                className="text-primary-600 hover:text-primary-700 p-1 rounded"
                title="View PDF"
              >
                <Eye className="h-4 w-4" />
              </button>
              <button
                onClick={handleDownload}
                className="text-primary-600 hover:text-primary-700 p-1 rounded"
                title="Download file"
              >
                <Download className="h-4 w-4" />
              </button>
            </>
          )}
          <button
            onClick={onViewDetails}
            className="text-primary-600 hover:text-primary-700 font-medium"
          >
            View Details
          </button>
        </div>
      </div>
    </div>
  )
}
