import React from 'react'
import { Download, Search } from 'lucide-react'
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
  console.log('🔍 SearchResults render:', { searchResults, isLoading, error, hasSearched })
  
  if (isLoading && hasSearched) {
    return (
      <div className="text-center py-8">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-500 mx-auto"></div>
        <p className="mt-4 text-gray-600">Searching...</p>
      </div>
    )
  }

  if (error && hasSearched) {
    console.error('🔍 Search error in component:', error)
    return (
      <div className="bg-red-50 border border-red-200 rounded-lg p-4">
        <p className="text-red-700">Error occurred while searching. Please try again.</p>
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
          <h3 className="text-lg font-medium text-gray-900 mb-2">Ready to Search</h3>
          <p>Enter your search query and click the search button to find relevant documents.</p>
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
          <h2 className="text-lg font-semibold text-gray-900">Search Results</h2>
          <p className="text-sm text-gray-600">
            Found {searchResults.total} results in {searchResults.took}ms
          </p>
        </div>
        <button onClick={onExport} className="btn-secondary flex items-center gap-2">
          <Download className="h-4 w-4" />
          Export
        </button>
      </div>

      <div className="divide-y divide-gray-200">
        {searchResults.results.map((result) => (
          <SearchResultItem key={result.id} result={result} />
        ))}
      </div>

      {searchResults.results.length === 0 && (
        <div className="p-8 text-center text-gray-500">
          <Search className="h-12 w-12 mx-auto mb-4 text-gray-300" />
          <p>No results found for your search query.</p>
        </div>
      )}
    </div>
  )
}

interface SearchResultItemProps {
  result: SearchResult
}

function SearchResultItem({ result }: SearchResultItemProps) {
  const formatScore = (score: number) => Math.round(score * 100)

  return (
    <div className="p-6 hover:bg-gray-50">
      <div className="flex justify-between items-start mb-2">
        <h3 className="text-lg font-medium text-gray-900 line-clamp-2">
          {result.title}
        </h3>
        <span className="ml-4 px-2 py-1 bg-primary-100 text-primary-700 text-xs font-medium rounded-full">
          {formatScore(result.score)}% match
        </span>
      </div>

      <p className="text-gray-600 mb-3 line-clamp-3">
        {result.content}
      </p>

      <div className="flex items-center justify-between text-sm text-gray-500">
        <div className="flex items-center space-x-4">
          <span className="px-2 py-1 bg-gray-100 rounded text-xs">
            {result.documentType}
          </span>
          <span>{result.source}</span>
          <span>{new Date(result.updatedAt).toLocaleDateString()}</span>
        </div>
        <button className="text-primary-600 hover:text-primary-700 font-medium">
          View Details
        </button>
      </div>
    </div>
  )
}
