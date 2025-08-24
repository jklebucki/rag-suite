import React from 'react'
import { BarChart3 } from 'lucide-react'

interface TopQueriesProps {
  queries?: string[]
}

export function TopQueries({ queries }: TopQueriesProps) {
  return (
    <div className="bg-white rounded-lg shadow-sm border p-6">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold text-gray-900">Top Queries</h2>
        <BarChart3 className="h-5 w-5 text-gray-400" />
      </div>

      <div className="space-y-3">
        {queries?.slice(0, 5).map((query, index) => (
          <div key={index} className="flex items-center justify-between py-2">
            <span className="text-sm text-gray-900 truncate flex-1">{query}</span>
            <span className="text-xs text-gray-500 ml-2">#{index + 1}</span>
          </div>
        )) || (
          <div className="text-gray-500 text-sm py-4 text-center">
            No query data available
          </div>
        )}
      </div>
    </div>
  )
}
