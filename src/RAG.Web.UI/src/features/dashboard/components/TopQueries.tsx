import React from 'react'
import { BarChart3, TrendingUp } from 'lucide-react'
import type { SearchStatistics } from '@/types'

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
    <div className="bg-white rounded-lg shadow-sm border p-6">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold text-gray-900">Search Analytics</h2>
        <BarChart3 className="h-5 w-5 text-gray-400" />
      </div>

      {/* Search Performance Overview */}
      {searchStats && (
        <div className="mb-6 grid grid-cols-2 gap-4">
          <div className="bg-blue-50 rounded-lg p-3">
            <div className="flex items-center gap-2">
              <TrendingUp className="h-4 w-4 text-blue-500" />
              <span className="text-sm font-medium text-blue-900">Total Searches</span>
            </div>
            <p className="text-lg font-bold text-blue-900 mt-1">
              {searchStats.totalSearches.toLocaleString()}
            </p>
          </div>
          <div className="bg-green-50 rounded-lg p-3">
            <div className="flex items-center gap-2">
              <TrendingUp className="h-4 w-4 text-green-500" />
              <span className="text-sm font-medium text-green-900">Last 24h</span>
            </div>
            <p className="text-lg font-bold text-green-900 mt-1">
              {searchStats.searchesLast24h.toLocaleString()}
            </p>
          </div>
        </div>
      )}

      {/* Top Indices by Search Volume */}
      <div className="space-y-3">
        <h3 className="text-sm font-medium text-gray-700 mb-2">Most Searched Indices</h3>
        {topIndices.length > 0 ? (
          topIndices.map(([indexName, count]) => {
            const maxCount = topIndices[0][1]
            const percentage = (count / maxCount) * 100
            
            return (
              <div key={indexName} className="space-y-1">
                <div className="flex items-center justify-between">
                  <span className="text-sm text-gray-900 truncate flex-1">{indexName}</span>
                  <span className="text-xs text-gray-500 ml-2">{count.toLocaleString()} searches</span>
                </div>
                <div className="w-full bg-gray-200 rounded-full h-2">
                  <div 
                    className={`bg-blue-500 h-2 rounded-full transition-all duration-300 ${
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
            <span className="text-sm text-gray-900 truncate flex-1">{query}</span>
            <span className="text-xs text-gray-500 ml-2">#{index + 1}</span>
          </div>
        )) || (
          <div className="text-gray-500 text-sm py-4 text-center">
            No search data available
          </div>
        )}
      </div>

      {/* Most Active Index */}
      {searchStats?.mostActiveIndex && (
        <div className="mt-4 pt-4 border-t">
          <div className="text-xs text-gray-500 mb-1">Most Active Index</div>
          <div className="text-sm font-medium text-gray-900">{searchStats.mostActiveIndex}</div>
        </div>
      )}
    </div>
  )
}
