import React from 'react'
import { Search, Filter } from 'lucide-react'
import { useI18n } from '@/contexts/I18nContext'

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
    <div className="bg-white rounded-lg shadow-sm border p-6">
      <form onSubmit={onSearch} className="space-y-4">
        <div className="flex gap-2">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-5 w-5 text-gray-400" />
            <input
              type="text"
              value={query}
              onChange={(e) => onQueryChange(e.target.value)}
              placeholder={t('search.input.placeholder')}
              className="w-full pl-10 pr-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
            />
          </div>
          <button
            type="button"
            onClick={onToggleAdvanced}
            className="px-4 py-3 border border-gray-300 rounded-lg hover:bg-gray-50 flex items-center gap-2"
          >
            <Filter className="h-5 w-5" />
            {t('search.filters.title')}
          </button>
          <button
            type="submit"
            disabled={!query.trim()}
            className="btn-primary disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {t('search.button')}
          </button>
          {onClear && (
            <button
              type="button"
              onClick={onClear}
              className="px-4 py-3 border border-gray-300 rounded-lg hover:bg-gray-50 text-gray-700"
            >
              Clear
            </button>
          )}
        </div>

        {isAdvancedMode && (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 p-4 bg-gray-50 rounded-lg">
            <div>
              <label htmlFor="document-type-select" className="block text-sm font-medium text-gray-700 mb-2">
                Document Type
              </label>
              <select
                id="document-type-select"
                value={filters.documentType}
                onChange={(e) => onFilterChange('documentType', e.target.value)}
                className="w-full border border-gray-300 rounded-lg px-3 py-2"
              >
                <option value="">All Types</option>
                <option value="sop">SOP Documents</option>
                <option value="schema">Database Schema</option>
                <option value="process">Business Process</option>
              </select>
            </div>
            <div>
              <label htmlFor="source-select" className="block text-sm font-medium text-gray-700 mb-2">
                Source
              </label>
              <select
                id="source-select"
                value={filters.source}
                onChange={(e) => onFilterChange('source', e.target.value)}
                className="w-full border border-gray-300 rounded-lg px-3 py-2"
              >
                <option value="">All Sources</option>
                <option value="oracle">Oracle DB</option>
                <option value="ifs">IFS</option>
                <option value="files">File System</option>
              </select>
            </div>
            <div>
              <label htmlFor="date-range-select" className="block text-sm font-medium text-gray-700 mb-2">
                Date Range
              </label>
              <select
                id="date-range-select"
                value={filters.dateRange}
                onChange={(e) => onFilterChange('dateRange', e.target.value)}
                className="w-full border border-gray-300 rounded-lg px-3 py-2"
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
