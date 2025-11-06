import React from 'react'
import { useMultilingualSearch } from '@/hooks/useMultilingualSearch'
import { useI18n } from '@/contexts/I18nContext'
import { SearchForm } from './SearchForm'
import { SearchResults } from './SearchResults'
import { logger } from '@/utils/logger'

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
    error,
    isSearching,
    setQuery,
    toggleAdvancedMode,
    handleSearch,
    handleFilterChange,
    handleClearSearch,
  } = useMultilingualSearch()

  return (
    <div className="max-w-6xl mx-auto space-y-6 px-4 sm:px-6">
      <div className="mb-6 sm:mb-8">
        <h1 className="text-2xl sm:text-3xl font-bold text-gray-900">{t('search.title')}</h1>
        <p className="text-gray-600 mt-1 text-sm sm:text-base">
          {t('search.subtitle')}
          {lastSearchLanguage && lastSearchLanguage !== currentLanguage && (
            <span className="block sm:inline sm:ml-2 text-blue-600 text-sm mt-1 sm:mt-0">
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
        onSearch={(e) => {
          e.preventDefault()
          handleSearch()
        }}
        onFilterChange={handleFilterChange}
        onClear={handleClearSearch}
      />

      <SearchResults
        searchResults={searchResults}
        isLoading={isSearching}
        error={error}
        hasSearched={hasSearched}
        onExport={() => {
          // TODO: Implement export functionality
          logger.debug('Export results:', searchResults)
        }}
      />
    </div>
  )
}
