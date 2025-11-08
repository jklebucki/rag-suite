import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import searchService from '@/features/search/services/search.service'
import { useToastContext } from '@/shared/contexts/ToastContext'
import { useI18n } from '@/shared/contexts/I18nContext'
import { logger } from '@/utils/logger'
import type { MultilingualSearchQuery } from '@/features/search/types/search'

export function useMultilingualSearch() {
  const [query, setQuery] = useState('')
  const [filters, setFilters] = useState({
    documentType: '',
    source: '',
    dateRange: '',
  })
  const [isAdvancedMode, setIsAdvancedMode] = useState(false)
  const [hasSearched, setHasSearched] = useState(false)
  const [lastSearchLanguage, setLastSearchLanguage] = useState<string | null>(null)
  const [isSearching, setIsSearching] = useState(false)
  const { showError, showSuccess } = useToastContext()
  const { language: currentLanguage } = useI18n()

  const hasFilters = () => {
    return filters.documentType || filters.source || filters.dateRange
  }

  // Multilingual search query - only enabled after user clicks search
  const {
    data: searchResults,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: ['multilingual-search', query, filters, currentLanguage],
    queryFn: async () => {
      logger.debug('Multilingual searching for:', query, 'in language:', currentLanguage)
      try {
        const searchQuery: MultilingualSearchQuery = {
          query,
          language: currentLanguage,
          resultLanguage: currentLanguage,
          maxResults: 20,
          enableCrossLanguageSearch: true,
          filters: hasFilters() ? {
            documentType: filters.documentType || undefined,
            source: filters.source || undefined,
            dateRange: filters.dateRange || undefined,
          } : undefined
        }

        const result = await searchService.searchMultilingual(searchQuery)
        logger.debug('Multilingual search results:', result)

        // Show language detection info if available
        if (result.detectedLanguage && result.detectedLanguage !== currentLanguage) {
          setLastSearchLanguage(result.detectedLanguage)
          showSuccess(
            'Language detected',
            `Query detected as ${result.detectedLanguage}, searching in ${result.resultLanguage || currentLanguage}`
          )
        }

        // Ensure we always return a valid SearchResponse object
        return result || {
          results: [],
          total: 0,
          took: 0,
          query: query
        }
      } catch (error) {
        logger.error('Multilingual search error:', error)
        throw error
      }
    },
    enabled: false, // Never auto-execute, only manual refetch
  })

  const handleSearch = async () => {
    if (!query.trim()) {
      showError('Search query required', 'Please enter a search query')
      return
    }

    logger.debug('Starting multilingual search for:', query)
    setHasSearched(true)
    setIsSearching(true)

    try {
      await refetch()
    } catch (error) {
      logger.error('Search failed:', error)
      showError('Search failed', 'Unable to search at this time. Please try again.')
    } finally {
      setIsSearching(false)
    }
  }

  const handleClearSearch = () => {
    setQuery('')
    setFilters({
      documentType: '',
      source: '',
      dateRange: '',
    })
    setHasSearched(false)
    setLastSearchLanguage(null)
  }

  const handleFilterChange = (filterType: string, value: string) => {
    setFilters(prev => ({
      ...prev,
      [filterType]: value
    }))
  }

  const toggleAdvancedMode = () => {
    setIsAdvancedMode(!isAdvancedMode)
  }

  return {
    // State
    query,
    setQuery,
    filters,
    isAdvancedMode,
    hasSearched,
    lastSearchLanguage,
    currentLanguage,
    isSearching,

    // Data
    searchResults,
    isLoading,
    error,

    // Actions
    handleSearch,
    handleClearSearch,
    handleFilterChange,
    toggleAdvancedMode,

    // Utilities
    hasFilters,
  }
}
