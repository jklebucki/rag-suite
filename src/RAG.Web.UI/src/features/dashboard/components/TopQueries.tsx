import React from 'react'
import { BarChart3, TrendingUp } from 'lucide-react'
import type { SearchStatistics } from '@/features/dashboard/types/analytics'

interface TopQueriesProps {
  queries?: string[]
  searchStats?: SearchStatistics
}

export function TopQueries({ queries, searchStats }: TopQueriesProps) {
  // Get top indices by search count
  const topIndices = searchStats?.searchesByIndex 
    ? Object.entries(searchStats.searchesByIndex)
        .sort(([,a], [,b]) => b - a)
        .slice(0, 5)
    : []

  return (
    <div className="surface p-6 h-full">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Search Analytics</h2>
        <BarChart3 className="h-5 w-5 text-gray-400 dark:text-gray-500" />
      </div>

      {/* Search Performance Overview */}
      {searchStats && (
        <div className="mb-6 grid grid-cols-2 gap-4">
          <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-3 border border-blue-200 dark:border-blue-500/50">
            <div className="flex items-center gap-2">
              <TrendingUp className="h-4 w-4 text-blue-500" />
              <span className="text-sm font-medium text-blue-900 dark:text-blue-200">Total Searches</span>
            </div>
            <p className="text-lg font-bold text-blue-900 dark:text-blue-200 mt-1">
              {searchStats.totalSearches.toLocaleString()}
            </p>
          </div>
          <div className="bg-green-50 dark:bg-green-900/20 rounded-lg p-3 border border-green-200 dark:border-green-500/50">
            <div className="flex items-center gap-2">
              <TrendingUp className="h-4 w-4 text-green-500" />
              <span className="text-sm font-medium text-green-900 dark:text-green-200">Last 24h</span>
            </div>
            <p className="text-lg font-bold text-green-900 dark:text-green-200 mt-1">
              {searchStats.searchesLast24h.toLocaleString()}
            </p>
          </div>
        </div>
      )}

      {/* Top Indices by Search Volume */}
      <div className="space-y-3">
        <h3 className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Most Searched Indices</h3>
        {topIndices.length > 0 ? (
          topIndices.map(([indexName, count]) => {
            const maxCount = topIndices[0][1]
            const percentage = (count / maxCount) * 100
            
            return (
              <div key={indexName} className="space-y-1">
                <div className="flex items-center justify-between">
                  <span className="text-sm text-gray-900 dark:text-gray-100 truncate flex-1">{indexName}</span>
                  <span className="text-xs text-gray-500 dark:text-gray-400 ml-2">{count.toLocaleString()} searches</span>
                </div>
                <div className="w-full bg-gray-200 dark:bg-slate-800 rounded-full h-2">
                  <div 
                    className={`bg-blue-500 dark:bg-blue-400 h-2 rounded-full transition-all duration-300 ${
                      percentage > 90 ? 'w-11/12' : 
                      percentage > 75 ? 'w-3/4' : 
                      percentage > 50 ? 'w-1/2' : 
                      percentage > 25 ? 'w-1/4' : 'w-1/12'
                    }`}
                  ></div>
                </div>
              </div>
            )
          })
        ) : queries?.slice(0, 5).map((query, index) => (
          <div key={index} className="flex items-center justify-between py-2">
            <span className="text-sm text-gray-900 dark:text-gray-100 truncate flex-1">{query}</span>
            <span className="text-xs text-gray-500 dark:text-gray-400 ml-2">#{index + 1}</span>
          </div>
        )) || (
          <div className="text-gray-500 dark:text-gray-400 text-sm py-4 text-center">
            No search data available
          </div>
        )}
      </div>

      {/* Most Active Index */}
      {searchStats?.mostActiveIndex && (
        <div className="mt-4 pt-4 border-t border-gray-200 dark:border-slate-800">
          <div className="text-xs text-gray-500 dark:text-gray-400 mb-1">Most Active Index</div>
          <div className="text-sm font-medium text-gray-900 dark:text-gray-100">{searchStats.mostActiveIndex}</div>
        </div>
      )}
    </div>
  )
}
