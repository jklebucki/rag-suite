import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { apiClient } from '@/services/api'
import { useToastContext } from '@/contexts/ToastContext'
import { useI18n } from '@/contexts/I18nContext'
import type { MultilingualSearchQuery, SearchResponse } from '@/types'

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
      console.log('🌐 Multilingual searching for:', query, 'in language:', currentLanguage)
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

        const result = await apiClient.searchMultilingual(searchQuery)
        console.log('🌐 Multilingual search results:', result)
        
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
        console.error('🌐 Multilingual search error:', error)
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

    console.log('🔍 Starting multilingual search for:', query)
    setHasSearched(true)
    
    try {
      await refetch()
    } catch (error) {
      console.error('Search failed:', error)
      showError('Search failed', 'Unable to search at this time. Please try again.')
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
