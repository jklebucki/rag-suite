import React from 'react'
import { useSearch } from '@/hooks/useSearch'
import { SearchForm } from './SearchForm'
import { SearchResults } from './SearchResults'

export function SearchInterface() {
  const {
    query,
    filters,
    isAdvancedMode,
    hasSearched,
    searchResults,
    isLoading,
    error,
    setQuery,
    setIsAdvancedMode,
    handleSearch,
    handleFilterChange,
    exportResults,
  } = useSearch()

  return (
    <div className="max-w-6xl mx-auto space-y-6">
      <SearchForm
        query={query}
        isAdvancedMode={isAdvancedMode}
        filters={filters}
        onQueryChange={setQuery}
        onToggleAdvanced={() => setIsAdvancedMode(!isAdvancedMode)}
        onSearch={handleSearch}
        onFilterChange={handleFilterChange}
      />

      <SearchResults
        searchResults={searchResults}
        isLoading={isLoading}
        error={error}
        hasSearched={hasSearched}
        onExport={exportResults}
      />
    </div>
  )
}
