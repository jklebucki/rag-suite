/**
 * useDeferredSearch - Custom hook for deferred search with loading indicator
 * 
 * This hook uses useDeferredValue to keep UI responsive while searching,
 * and provides a loading indicator when query differs from deferred query.
 * 
 * Usage:
 * ```tsx
 * const { query, deferredQuery, isSearching, setQuery } = useDeferredSearch()
 * 
 * const { data } = useQuery({
 *   queryKey: ['search', deferredQuery],
 *   queryFn: () => searchService.search(deferredQuery),
 *   enabled: !!deferredQuery
 * })
 * ```
 */

import { useState, useDeferredValue, useMemo } from 'react'

interface UseDeferredSearchOptions {
  initialValue?: string
  debounceMs?: number
}

interface UseDeferredSearchReturn {
  query: string
  deferredQuery: string
  isSearching: boolean
  setQuery: (value: string) => void
  clearQuery: () => void
}

export function useDeferredSearch({
  initialValue = '',
  debounceMs = 300,
}: UseDeferredSearchOptions = {}): UseDeferredSearchReturn {
  const [query, setQuery] = useState(initialValue)
  const deferredQuery = useDeferredValue(query)

  // Show loading indicator when query is different from deferred query
  const isSearching = useMemo(() => {
    return query !== deferredQuery && query.trim() !== ''
  }, [query, deferredQuery])

  const clearQuery = () => {
    setQuery('')
  }

  return {
    query,
    deferredQuery,
    isSearching,
    setQuery,
    clearQuery,
  }
}

