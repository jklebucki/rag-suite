import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import searchService from '@/features/search/services/search.service'
import { useToastContext } from '@/shared/contexts/ToastContext'
import { useI18n } from '@/shared/contexts/I18nContext'
import { logger } from '@/utils/logger'
import { formatDate } from '@/utils/date'

export function useSearch() {
  const [query, setQuery] = useState('')
  const [filters, setFilters] = useState({
    documentType: '',
    source: '',
    dateRange: '',
  })
  const [isAdvancedMode, setIsAdvancedMode] = useState(false)
  const [hasSearched, setHasSearched] = useState(false) // Track if user has initiated search
  const { showError, showSuccess } = useToastContext()
  const { language } = useI18n()

  // Search query - only enabled after user clicks search
  const {
    data: searchResults,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: ['search', query, filters],
    queryFn: async ({ signal }) => {
      logger.debug('Searching for:', query)
      try {
        const result = await searchService.search({
          query,
          limit: 20,
        }, { signal })
        logger.debug('Search results:', result)
        // Ensure we always return a valid SearchResponse object
        return result || {
          results: [],
          total: 0,
          took: 0,
          query: query
        }
      } catch (error) {
        logger.error('Search error:', error)
        throw error
      }
    },
    enabled: false, // Never auto-execute, only manual refetch
  })

  const handleQueryChange = (newQuery: string) => {
    setQuery(newQuery)
    // Reset search state when query is cleared
    if (!newQuery.trim()) {
      setHasSearched(false)
    }
  }

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault()
    logger.debug('Handle search triggered, query:', query)
    if (query.trim()) {
      setHasSearched(true)
      refetch()
    }
  }

  const handleFilterChange = (filterType: string, value: string) => {
    setFilters(prev => ({
      ...prev,
      [filterType]: value,
    }))
  }

  const clearFilters = () => {
    setFilters({
      documentType: '',
      source: '',
      dateRange: '',
    })
  }

  const exportResults = () => {
    if (searchResults?.results) {
      // Simple CSV export logic
      const csvContent = [
        ['Title', 'Content', 'Type', 'Source', 'Score', 'Updated'],
        ...searchResults.results.map(result => [
          result.title,
          result.content.substring(0, 100) + '...',
          result.documentType,
          result.source,
          Math.round(result.score) + '%',
          formatDate(result.updatedAt, language)
        ])
      ].map(row => row.map(cell => `"${cell}"`).join(',')).join('\n')

      const blob = new Blob([csvContent], { type: 'text/csv' })
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `search-results-${new Date().toISOString().split('T')[0]}.csv`
      a.click()
      window.URL.revokeObjectURL(url)
      
      showSuccess('Search results exported', `Downloaded ${searchResults.results.length} results as CSV`)
    } else {
      showError('Export failed', 'No search results to export')
    }
  }

  return {
    // State
    query,
    filters,
    isAdvancedMode,
    hasSearched,
    
    // Data
    searchResults,
    isLoading,
    error,
    
    // Actions
    setQuery: handleQueryChange,
    setIsAdvancedMode,
    handleSearch,
    handleFilterChange,
    clearFilters,
    exportResults,
    refetch,
  }
}
