import React from 'react'
import { useMultilingualSearch } from '@/hooks/useMultilingualSearch'
import { useI18n } from '@/contexts/I18nContext'
import { SearchForm } from './SearchForm'
import { SearchResults } from './SearchResults'

export function SearchInterface() {
  const { t } = useI18n()
  const {
    query,
    filters,
    isAdvancedMode,
    hasSearched,
    lastSearchLanguage,
    currentLanguage,
    searchResults,
    isLoading,
    error,
    setQuery,
    toggleAdvancedMode,
    handleSearch,
    handleFilterChange,
    handleClearSearch,
  } = useMultilingualSearch()

  return (
    <div className="max-w-6xl mx-auto space-y-6">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">{t('search.title')}</h1>
        <p className="text-gray-600 mt-1">
          {t('search.subtitle')}
          {lastSearchLanguage && lastSearchLanguage !== currentLanguage && (
            <span className="ml-2 text-blue-600 text-sm">
              (Query detected in {lastSearchLanguage})
            </span>
          )}
        </p>
      </div>

      <SearchForm
        query={query}
        isAdvancedMode={isAdvancedMode}
        filters={filters}
        onQueryChange={setQuery}
        onToggleAdvanced={toggleAdvancedMode}
        onSearch={() => handleSearch()}
        onFilterChange={handleFilterChange}
        onClear={handleClearSearch}
      />

      <SearchResults
        searchResults={searchResults}
        isLoading={isLoading}
        error={error}
        hasSearched={hasSearched}
        onExport={() => {
          // TODO: Implement export functionality
          console.log('Export results:', searchResults)
        }}
      />
    </div>
  )
}
