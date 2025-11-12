/**
 * SearchingIndicator - Visual indicator for deferred search operations
 * 
 * Shows a subtle loading indicator when search query is being deferred.
 * Use with useDeferredSearch hook.
 */

import { Loader2 } from 'lucide-react'

interface SearchingIndicatorProps {
  isSearching: boolean
  className?: string
}

export function SearchingIndicator({ isSearching, className = '' }: SearchingIndicatorProps) {
  if (!isSearching) return null

  return (
    <div className={`flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400 ${className}`}>
      <Loader2 className="h-4 w-4 animate-spin" />
      <span>Searching...</span>
    </div>
  )
}

