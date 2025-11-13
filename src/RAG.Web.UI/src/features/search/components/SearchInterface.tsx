import React from 'react'
import { useMultilingualSearch } from '@/features/search/hooks/useMultilingualSearch'
import { useI18n } from '@/shared/contexts/I18nContext'
import { SearchForm } from './SearchForm'
import { SearchResults } from './SearchResults'
import { SearchingIndicator } from '@/shared/components/common/SearchingIndicator'
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
    <div className="w-full h-full mx-auto px-4 sm:px-6 flex flex-col">
      <div className="mb-6 sm:mb-8 flex-shrink-0">
        <h1 className="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-gray-100">{t('search.title')}</h1>
        <p className="text-gray-600 dark:text-gray-400 mt-1 text-sm sm:text-base">
          {t('search.subtitle')}
          {lastSearchLanguage && lastSearchLanguage !== currentLanguage && (
            <span className="block sm:inline sm:ml-2 text-blue-600 dark:text-blue-300 text-sm mt-1 sm:mt-0">
              (Query detected in {lastSearchLanguage})
            </span>
          )}
        </p>
      </div>

      <div className="flex-shrink-0 mb-6">
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
      </div>

      <div className="flex-1 min-h-0 overflow-hidden">
        <SearchResults
          searchResults={searchResults}
          isLoading={isSearching}
          error={error instanceof Error ? error.message : error ? String(error) : null}
          hasSearched={hasSearched}
          onExport={() => {
            // TODO: Implement export functionality
            logger.debug('Export results:', searchResults)
          }}
        />
      </div>
    </div>
  )
}
