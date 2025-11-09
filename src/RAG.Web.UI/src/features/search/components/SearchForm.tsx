import React from 'react'
import { Search, Filter } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'

interface SearchFormProps {
  query: string
  isAdvancedMode: boolean
  filters: {
    documentType: string
    source: string
    dateRange: string
  }
  onQueryChange: (query: string) => void
  onToggleAdvanced: () => void
  onSearch: (e: React.FormEvent) => void
  onFilterChange: (filterType: string, value: string) => void
  onClear?: () => void
}

export function SearchForm({
  query,
  isAdvancedMode,
  filters,
  onQueryChange,
  onToggleAdvanced,
  onSearch,
  onFilterChange,
  onClear,
}: SearchFormProps) {
  const { t } = useI18n()
  
  return (
    <div className="surface p-4 sm:p-6">
      <form onSubmit={onSearch} className="space-y-4">
        {/* Search input - full width on mobile */}
        <div className="relative">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-5 w-5 text-gray-400 dark:text-gray-500" />
          <input
            type="text"
            value={query}
            onChange={(e) => onQueryChange(e.target.value)}
            placeholder={t('search.input.placeholder')}
            className="form-input pl-10 pr-4"
          />
        </div>
        
        {/* Buttons - stacked on mobile, row on desktop */}
        <div className="flex flex-col sm:flex-row gap-2">
          <button
            type="button"
            onClick={onToggleAdvanced}
            className="w-full sm:w-auto px-4 py-3 rounded-xl border border-primary-100 dark:border-slate-700 bg-white dark:bg-slate-900 hover:bg-primary-50 dark:hover:bg-slate-800 flex items-center justify-center gap-2 transition-colors"
          >
            <Filter className="h-5 w-5" />
            <span className="sm:inline">{t('search.filters.title')}</span>
          </button>
          <button
            type="submit"
            disabled={!query.trim()}
            className="w-full sm:w-auto btn-primary disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {t('search.button')}
          </button>
          {onClear && (
            <button
              type="button"
              onClick={onClear}
              className="w-full sm:w-auto px-4 py-3 rounded-xl border border-transparent bg-gray-100 hover:bg-gray-200 dark:bg-slate-800 dark:hover:bg-slate-700 text-gray-700 dark:text-gray-200 transition-colors"
            >
              Clear
            </button>
          )}
        </div>

        {isAdvancedMode && (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 p-4 surface-muted">
            <div>
              <label htmlFor="document-type-select" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                Document Type
              </label>
              <select
                id="document-type-select"
                value={filters.documentType}
                onChange={(e) => onFilterChange('documentType', e.target.value)}
                className="form-select"
              >
                <option value="">All Types</option>
                <option value="sop">SOP Documents</option>
                <option value="schema">Database Schema</option>
                <option value="process">Business Process</option>
              </select>
            </div>
            <div>
              <label htmlFor="source-select" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                Source
              </label>
              <select
                id="source-select"
                value={filters.source}
                onChange={(e) => onFilterChange('source', e.target.value)}
                className="form-select"
              >
                <option value="">All Sources</option>
                <option value="oracle">Oracle DB</option>
                <option value="ifs">IFS</option>
                <option value="files">File System</option>
              </select>
            </div>
            <div>
              <label htmlFor="date-range-select" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                Date Range
              </label>
              <select
                id="date-range-select"
                value={filters.dateRange}
                onChange={(e) => onFilterChange('dateRange', e.target.value)}
                className="form-select"
              >
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
  )
}
