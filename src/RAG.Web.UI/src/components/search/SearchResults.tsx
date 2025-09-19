import React, { useState } from 'react'
import { Download, Search } from 'lucide-react'
import { useI18n } from '@/contexts/I18nContext'
import { Modal } from '@/components/ui/Modal'
import { DocumentDetail } from './DocumentDetail'
import { useDocumentDetail } from './hooks/useDocumentDetail'
import { formatDate } from '@/utils/date'
import { apiClient } from '@/services/api'
import type { SearchResult } from '@/types'

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
  const { data: documentDetail, isLoading: isLoadingDetail, error: detailError } = useDocumentDetail(selectedDocumentId)

  console.log('🔍 SearchResults render:', { searchResults, isLoading, error, hasSearched })

  const renderError = (err: unknown) => {
    if (!err) return null
    return (
      <div className="p-6 bg-red-50 border border-red-200 rounded-lg m-6">
        <p className="text-red-700">{t('search.error')}</p>
      </div>
    )
  }

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

  if (!hasSearched) {
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

  if (!searchResults) {
    return null
  }

  return (
    <div className="bg-white rounded-lg shadow-sm border">
      <div className="p-6 border-b border-gray-200 flex justify-between items-center">
        <div>
          <h2 className="text-lg font-semibold text-gray-900">{t('search.results')}</h2>
          <p className="text-sm text-gray-600">
            {t('search.results')} {searchResults.total} {t('search.results')} in {searchResults.took}ms
          </p>
        </div>
        <button onClick={onExport} className="btn-secondary flex items-center gap-2">
          <Download className="h-4 w-4" />
          Export
        </button>
      </div>

      <div className="divide-y divide-gray-200">
        {searchResults.results.map((result) => (
          <SearchResultItem
            key={result.id}
            result={result}
            onViewDetails={() => setSelectedDocumentId(result.id)}
            language={language}
          />
        ))}
      </div>

      {searchResults.results.length === 0 && (
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
    </div>
  )
}

interface SearchResultItemProps {
  result: SearchResult
  onViewDetails: () => void
  language: import('@/types/i18n').LanguageCode
}

function SearchResultItem({ result, onViewDetails, language }: SearchResultItemProps) {
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
            <button
              onClick={handleDownload}
              className="text-primary-600 hover:text-primary-700 p-1 rounded"
              title="Download file"
            >
              <Download className="h-4 w-4" />
            </button>
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
