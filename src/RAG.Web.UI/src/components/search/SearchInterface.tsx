import React, { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Search, Filter, Download } from 'lucide-react'
import { apiClient } from '@/services/api'
import type { SearchResult } from '@/types'

export function SearchInterface() {
  const [query, setQuery] = useState('')
  const [filters] = useState({
    documentType: [],
    source: [],
  })
  const [isAdvancedMode, setIsAdvancedMode] = useState(false)

  const {
    data: searchResults,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: ['search', query, filters],
    queryFn: () => apiClient.search({
      query,
      filters: Object.keys(filters).length > 0 ? filters : undefined,
      limit: 20,
    }),
    enabled: query.length > 0,
  })

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault()
    if (query.trim()) {
      refetch()
    }
  }

  return (
    <div className="max-w-6xl mx-auto space-y-6">
      <div className="bg-white rounded-lg shadow-sm border p-6">
        <h1 className="text-2xl font-bold text-gray-900 mb-6">Search Knowledge Base</h1>

        <form onSubmit={handleSearch} className="space-y-4">
          <div className="flex gap-2">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-5 w-5 text-gray-400" />
              <input
                type="text"
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                placeholder="Search documents, processes, schemas..."
                className="w-full pl-10 pr-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
              />
            </div>
            <button
              type="button"
              onClick={() => setIsAdvancedMode(!isAdvancedMode)}
              className="px-4 py-3 border border-gray-300 rounded-lg hover:bg-gray-50 flex items-center gap-2"
            >
              <Filter className="h-5 w-5" />
              Filters
            </button>
            <button
              type="submit"
              disabled={!query.trim()}
              className="btn-primary disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Search
            </button>
          </div>

          {isAdvancedMode && (
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4 p-4 bg-gray-50 rounded-lg">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Document Type
                </label>
                <select className="w-full border border-gray-300 rounded-lg px-3 py-2">
                  <option value="">All Types</option>
                  <option value="sop">SOP Documents</option>
                  <option value="schema">Database Schema</option>
                  <option value="process">Business Process</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Source
                </label>
                <select className="w-full border border-gray-300 rounded-lg px-3 py-2">
                  <option value="">All Sources</option>
                  <option value="oracle">Oracle DB</option>
                  <option value="ifs">IFS</option>
                  <option value="files">File System</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Date Range
                </label>
                <select className="w-full border border-gray-300 rounded-lg px-3 py-2">
                  <option value="">Any Time</option>
                  <option value="week">Last Week</option>
                  <option value="month">Last Month</option>
                  <option value="year">Last Year</option>
                </select>
              </div>
            </div>
          )}
        </form>
      </div>

      {/* Search Results */}
      {isLoading ? (
        <div className="text-center py-8">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-500 mx-auto"></div>
          <p className="mt-4 text-gray-600">Searching...</p>
        </div>
      ) : null}

      {error ? (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <p className="text-red-700">Error occurred while searching. Please try again.</p>
        </div>
      ) : null}

      {searchResults && (
        <div className="bg-white rounded-lg shadow-sm border">
          <div className="p-6 border-b border-gray-200 flex justify-between items-center">
            <div>
              <h2 className="text-lg font-semibold text-gray-900">Search Results</h2>
              <p className="text-sm text-gray-600">
                Found {searchResults.total} results in {searchResults.took}ms
              </p>
            </div>
            <button className="btn-secondary flex items-center gap-2">
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
