import React from 'react'
import { Calendar, FileText, Database, Tag, Hash } from 'lucide-react'
import type { SearchResult } from '@/types'

interface DocumentDetailProps {
  document: SearchResult
}

export function DocumentDetail({ document }: DocumentDetailProps) {
  const formatScore = (score: number) => Math.round(score * 100)

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="border-b border-gray-200 pb-4">
        <div className="flex items-start justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900 mb-2">{document.title}</h1>
            <div className="flex items-center space-x-4 text-sm text-gray-600">
              <span className="flex items-center">
                <Database className="h-4 w-4 mr-1" />
                {document.source}
              </span>
              <span className="flex items-center">
                <FileText className="h-4 w-4 mr-1" />
                {document.documentType}
              </span>
              <span className="flex items-center">
                <Hash className="h-4 w-4 mr-1" />
                ID: {document.id}
              </span>
            </div>
          </div>
          <div className="text-right">
            <div className="px-3 py-1 bg-primary-100 text-primary-700 text-sm font-medium rounded-full mb-2">
              {formatScore(document.score)}% match
            </div>
          </div>
        </div>
      </div>

      {/* Content */}
      <div>
        <h3 className="text-lg font-semibold text-gray-900 mb-3">Content</h3>
        <div className="bg-gray-50 rounded-lg p-4">
          <p className="text-gray-700 leading-relaxed whitespace-pre-wrap">
            {document.content}
          </p>
        </div>
      </div>

      {/* Metadata */}
      {Object.keys(document.metadata).length > 0 && (
        <div>
          <h3 className="text-lg font-semibold text-gray-900 mb-3 flex items-center">
            <Tag className="h-5 w-5 mr-2" />
            Metadata
          </h3>
          <div className="bg-gray-50 rounded-lg p-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              {Object.entries(document.metadata).map(([key, value]) => (
                <div key={key} className="flex justify-between items-center py-2 border-b border-gray-200 last:border-b-0">
                  <span className="font-medium text-gray-700 capitalize">
                    {key.replace(/([A-Z])/g, ' $1').replace(/^./, str => str.toUpperCase())}:
                  </span>
                  <span className="text-gray-600">
                    {typeof value === 'object' ? JSON.stringify(value) : String(value)}
                  </span>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      {/* Timestamps */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="bg-gray-50 rounded-lg p-4">
          <h4 className="font-semibold text-gray-900 mb-2 flex items-center">
            <Calendar className="h-4 w-4 mr-2" />
            Created
          </h4>
          <p className="text-gray-600">
            {new Date(document.createdAt).toLocaleString()}
          </p>
        </div>
        <div className="bg-gray-50 rounded-lg p-4">
          <h4 className="font-semibold text-gray-900 mb-2 flex items-center">
            <Calendar className="h-4 w-4 mr-2" />
            Last Updated
          </h4>
          <p className="text-gray-600">
            {new Date(document.updatedAt).toLocaleString()}
          </p>
        </div>
      </div>
    </div>
  )
}
