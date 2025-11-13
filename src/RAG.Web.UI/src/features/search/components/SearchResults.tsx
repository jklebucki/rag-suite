import React, { useState, useEffect, Suspense } from 'react'
import { Download, Search, Eye } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import { Modal } from '@/shared/components/ui/Modal'
import { DocumentDetail } from './DocumentDetail'
import { useDocumentDetail } from '@/features/search/hooks/useDocumentDetail'
import { formatDate } from '@/utils/date'
import fileService from '@/shared/services/file.service'
import type { SearchResult } from '@/features/search/types/search'
import { logger } from '@/utils/logger'
import { useAsyncComponent } from '@/shared/hooks/useAsyncComponent'

// Lazy load PDFViewerModal using React 19's use() hook
const PDFViewerModalPromise = import('@/shared/components/ui/PDFViewerModal').then(module => ({ default: module.PDFViewerModal }))

function PDFViewerModalLoader(props: React.ComponentProps<typeof import('@/shared/components/ui/PDFViewerModal').PDFViewerModal>) {
  const PDFViewerModal = useAsyncComponent(PDFViewerModalPromise)
  return <PDFViewerModal {...props} />
}

interface SearchResultsProps {
  searchResults?: {
    results: SearchResult[]
    total: number
    took: number
  }
  isLoading: boolean
  error: string | null
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

  logger.debug('SearchResults render:', { searchResults, isLoading, error, hasSearched })

  const renderError = (err: unknown) => {
    if (!err) return null
    return (
      <div className="p-6 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-700 rounded-2xl m-6">
        <p className="text-red-700 dark:text-red-300">{t('search.error')}</p>
      </div>
    )
  }

  // Spinner jak przy pierwszym wyszukiwaniu - przy każdym wyszukiwaniu gdy isLoading i hasSearched
  if (isLoading && hasSearched) {
    return (
      <div className="surface h-full flex items-center justify-center rounded-2xl">
        <div className="text-center py-8 px-4 text-gray-600 dark:text-gray-300">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-500 mx-auto"></div>
          <p className="mt-4 text-sm sm:text-base">{t('search.loading')}</p>
        </div>
      </div>
    )
  }

  if (error && hasSearched) {
    logger.error('Search error in component:', error)
    return (
      <div className="surface h-full flex items-center justify-center rounded-2xl">
        <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-700 rounded-2xl p-4 mx-4 sm:mx-0">
          <p className="text-red-700 dark:text-red-300 text-sm sm:text-base">{t('search.error')}</p>
          <details className="mt-2">
            <summary className="cursor-pointer text-sm dark:text-red-200">Error details</summary>
            <pre className="text-xs mt-1 text-red-600 dark:text-red-300 overflow-x-auto">{JSON.stringify(error, null, 2)}</pre>
          </details>
        </div>
      </div>
    )
  }

  if (!hasSearched || (hasSearched && !searchResults && !isLoading)) {
    return (
      <div className="surface h-full flex items-center justify-center rounded-2xl">
        <div className="text-center text-gray-500 dark:text-gray-400">
          <Search className="h-12 sm:h-16 w-12 sm:w-16 mx-auto mb-4 text-gray-300 dark:text-slate-600" />
          <h3 className="text-base sm:text-lg font-medium text-gray-900 dark:text-gray-100 mb-2">{t('search.title')}</h3>
          <p className="text-sm sm:text-base">{t('search.subtitle')}</p>
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
    <div className="surface relative h-full flex flex-col overflow-hidden rounded-2xl">
      {/* Dodaj overlay z spinnerem jeśli isLoading, hasSearched i są wyniki (np. przy zmianie inputu po wyszukaniu) */}
      {isLoading && hasSearched && searchResults && (
        <div className="absolute inset-0 bg-white dark:bg-slate-900 bg-opacity-75 dark:bg-opacity-80 flex items-center justify-center z-10 rounded-2xl">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-500"></div>
        </div>
      )}
      <div className="p-4 sm:p-6 border-b border-gray-200 dark:border-slate-800 flex flex-col sm:flex-row sm:justify-between sm:items-center gap-3 flex-shrink-0">
        <div>
          <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">{t('search.results')}</h2>
          <p className="text-sm text-gray-600 dark:text-gray-400">
            {t('search.results')} {resultsToShow.total} {t('search.results')} in {resultsToShow.took}ms
          </p>
        </div>
        <button onClick={onExport} className="btn-secondary flex items-center justify-center gap-2 w-full sm:w-auto">
          <Download className="h-4 w-4" />
          Export
        </button>
      </div>

      <div className="flex-1 min-h-0 overflow-y-auto">
        <div className="divide-y divide-gray-200 dark:divide-slate-800">
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
          <div className="p-6 sm:p-8 text-center text-gray-500 dark:text-gray-400">
            <Search className="h-10 sm:h-12 w-10 sm:w-12 mx-auto mb-4 text-gray-300 dark:text-slate-600" />
            <p className="text-sm sm:text-base">{t('search.no_results')}</p>
          </div>
        )}
      </div>

      {/* Document Detail Modal */}
      <Modal
        isOpen={!!selectedDocumentId}
        onClose={() => setSelectedDocumentId(null)}
        title="Document Details"
        size="xl"
      >
        {isLoadingDetail && (
          <div className="p-6 sm:p-8 text-center text-gray-600 dark:text-gray-300">
            <div className="animate-spin rounded-full h-10 sm:h-12 w-10 sm:w-12 border-b-2 border-primary-500 mx-auto"></div>
            <p className="mt-4 text-sm sm:text-base">Loading document details...</p>
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
          <div className="flex items-center justify-center p-8 text-gray-600 dark:text-gray-300">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-500"></div>
            <span className="ml-2">Loading PDF viewer...</span>
          </div>
        </Modal>
      }>
        <PDFViewerModalLoader
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
  language: import('@/shared/types/i18n').LanguageCode
}

const SearchResultItem = React.memo<SearchResultItemProps>(({ result, onViewDetails, onViewPDF, language }) => {
  const formatScore = (score: number) => Math.round(score)

  // Check if document was reconstructed from chunks
  const isReconstructed = result.metadata?.reconstructed
  const chunksInfo = result.metadata?.chunksFound && result.metadata?.totalChunks
    ? `${result.metadata.chunksFound}/${result.metadata.totalChunks} chunks`
    : null

  const handleDownload = async () => {
    if (result.filePath) {
      try {
        await fileService.downloadFile(result.filePath)
      } catch (error) {
        logger.error('Download failed:', error)
        // TODO: Show error toast
      }
    }
  }

  return (
    <div className="p-4 sm:p-6 hover:bg-gray-50 dark:hover:bg-slate-900 transition-colors">
      <div className="flex flex-col sm:flex-row sm:justify-between sm:items-start mb-2 gap-2">
        <h3 className="text-base sm:text-lg font-medium text-gray-900 dark:text-gray-100 line-clamp-2 flex-1">
          {result.title}
        </h3>
        <div className="flex items-center gap-2 flex-shrink-0">
          {isReconstructed && (
            <span className="px-2 py-1 bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-200 text-xs font-medium rounded-full whitespace-nowrap">
              Reconstructed
            </span>
          )}
          <span className="px-2 py-1 bg-primary-100 text-primary-700 dark:bg-primary-900/30 dark:text-primary-300 text-xs font-medium rounded-full whitespace-nowrap">
            {formatScore(result.score)}% match
          </span>
        </div>
      </div>

      {/* Show highlights if available, otherwise show content */}
      <div className="text-sm sm:text-base text-gray-600 dark:text-gray-300 mb-3 line-clamp-3 search-highlights">
        {result.metadata?.highlights ? (
          <div dangerouslySetInnerHTML={{ __html: result.metadata.highlights }} />
        ) : (
          <p>{result.content}</p>
        )}
      </div>

      {/* Metadata and actions - stack on mobile */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 text-sm text-gray-500 dark:text-gray-400">
        <div className="flex flex-wrap items-center gap-2">
          <span className="px-2 py-1 bg-gray-100 dark:bg-slate-800 rounded text-xs whitespace-nowrap">
            {result.documentType}
          </span>
          <span className="text-xs sm:text-sm truncate">{result.source}</span>
          {chunksInfo && (
            <span className="px-2 py-1 bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-300 text-xs rounded whitespace-nowrap">
              {chunksInfo}
            </span>
          )}
          <span className="text-xs sm:text-sm">{formatDate(result.updatedAt, language)}</span>
        </div>
        <div className="flex items-center gap-2 justify-end">
          {result.filePath && (
            <>
              <button
                onClick={() => onViewPDF?.(result.filePath!)}
                className="text-primary-600 hover:text-primary-700 dark:text-primary-300 dark:hover:text-primary-200 p-1 rounded"
                title="View PDF"
              >
                <Eye className="h-4 w-4" />
              </button>
              <button
                onClick={handleDownload}
                className="text-primary-600 hover:text-primary-700 dark:text-primary-300 dark:hover:text-primary-200 p-1 rounded"
                title="Download file"
              >
                <Download className="h-4 w-4" />
              </button>
            </>
          )}
          <button
            onClick={onViewDetails}
            className="text-primary-600 hover:text-primary-700 dark:text-primary-300 dark:hover:text-primary-200 font-medium text-sm whitespace-nowrap"
          >
            View Details
          </button>
        </div>
      </div>
    </div>
  )
}, (prevProps, nextProps) => {
  // Custom comparison for better performance
  return (
    prevProps.result.id === nextProps.result.id &&
    prevProps.result.title === nextProps.result.title &&
    prevProps.result.content === nextProps.result.content &&
    prevProps.result.score === nextProps.result.score &&
    prevProps.language === nextProps.language
  )
})

SearchResultItem.displayName = 'SearchResultItem'

// Export for testing
export { SearchResultItem }
