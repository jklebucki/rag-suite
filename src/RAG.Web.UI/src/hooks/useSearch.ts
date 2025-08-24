import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { apiClient } from '@/services/api'

export function useSearch() {
  const [query, setQuery] = useState('')
  const [filters, setFilters] = useState({
    documentType: '',
    source: '',
    dateRange: '',
  })
  const [isAdvancedMode, setIsAdvancedMode] = useState(false)

  const hasFilters = () => {
    return filters.documentType || filters.source || filters.dateRange
  }

  // Search query
  const {
    data: searchResults,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: ['search', query, filters],
    queryFn: () => apiClient.search({
      query,
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
          Math.round(result.score * 100) + '%',
          new Date(result.updatedAt).toLocaleDateString()
        ])
      ].map(row => row.map(cell => `"${cell}"`).join(',')).join('\n')

      const blob = new Blob([csvContent], { type: 'text/csv' })
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `search-results-${new Date().toISOString().split('T')[0]}.csv`
      a.click()
      window.URL.revokeObjectURL(url)
    }
  }

  return {
    // State
    query,
    filters,
    isAdvancedMode,
    
    // Data
    searchResults,
    isLoading,
    error,
    
    // Actions
    setQuery,
    setIsAdvancedMode,
    handleSearch,
    handleFilterChange,
    clearFilters,
    exportResults,
    refetch,
  }
}
