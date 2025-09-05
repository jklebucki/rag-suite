import React from 'react'
import { Calendar, FileText, Database, Tag, Hash, File, HardDrive, Clock } from 'lucide-react'
import { useI18n } from '@/contexts/I18nContext'
import { formatDateTime } from '@/utils/date'
import type { DocumentDetailResponse } from '@/types'

interface DocumentDetailProps {
  document: DocumentDetailResponse
}

export function DocumentDetail({ document }: DocumentDetailProps) {
  const formatScore = (score: number) => Math.round(score * 100)
  const { language } = useI18n()

  // Extract metadata for the table
  const fileExtension = document.metadata?.file_extension || document.source || ''
  const fileSize = document.metadata?.file_size || 'Unknown'
  const lastModified = document.metadata?.last_modified || ''
  const indexedAt = document.metadata?.indexed_at || ''
  const filePath = document.filePath || document.metadata?.file_path || document.metadata?.source_file || ''

  const formatBytes = (bytes: string | number) => {
    if (!bytes || bytes === 'Unknown') return 'Unknown'
    const num = typeof bytes === 'string' ? parseInt(bytes) : bytes
    if (isNaN(num)) return 'Unknown'
    
    const sizes = ['Bytes', 'KB', 'MB', 'GB']
    if (num === 0) return '0 Bytes'
    const i = Math.floor(Math.log(num) / Math.log(1024))
    return Math.round(num / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i]
  }

  const formatMetadataDate = (dateStr: string) => {
    if (!dateStr) return 'Unknown'
    try {
      const date = new Date(dateStr)
      return isNaN(date.getTime()) ? dateStr : formatDateTime(date, language)
    } catch {
      return dateStr
    }
  }

  return (
    <div className="flex flex-col h-full max-h-[80vh]">
      {/* Header with file path */}
      <div className="flex-shrink-0 border-b border-gray-200 pb-4 mb-4">
        <div className="flex items-center space-x-2 text-sm text-gray-600 mb-2">
          <File className="h-4 w-4" />
          <span className="font-mono text-xs bg-gray-100 px-2 py-1 rounded">
            {filePath || 'Unknown path'}
          </span>
        </div>
        <h1 className="text-xl font-bold text-gray-900">{document.title || document.fileName}</h1>
      </div>

      {/* Scrollable content area */}
      <div className="flex-grow overflow-y-auto mb-4">
        <div className="bg-gray-50 rounded-lg p-4 h-full">
          <div className="text-sm text-gray-700 leading-relaxed whitespace-pre-wrap font-mono">
            {document.fullContent || document.content}
          </div>
        </div>
      </div>

      {/* Metadata table at the bottom */}
      <div className="flex-shrink-0 bg-gray-50 rounded-lg p-4">
        <h3 className="text-sm font-semibold text-gray-900 mb-3 flex items-center">
          <Tag className="h-4 w-4 mr-2" />
          File Information
        </h3>
        <div className="grid grid-cols-2 gap-3 text-sm">
          <div className="flex justify-between">
            <span className="font-medium text-gray-700">File Extension:</span>
            <span className="text-gray-600">{fileExtension}</span>
          </div>
          <div className="flex justify-between">
            <span className="font-medium text-gray-700">File Size:</span>
            <span className="text-gray-600">{formatBytes(fileSize)}</span>
          </div>
          <div className="flex justify-between">
            <span className="font-medium text-gray-700">Last Modified:</span>
            <span className="text-gray-600">{formatMetadataDate(lastModified)}</span>
          </div>
          <div className="flex justify-between">
            <span className="font-medium text-gray-700">Indexed At:</span>
            <span className="text-gray-600">{formatMetadataDate(indexedAt)}</span>
          </div>
          <div className="flex justify-between">
            <span className="font-medium text-gray-700">Created:</span>
            <span className="text-gray-600">{formatDateTime(document.createdAt, language)}</span>
          </div>
          <div className="flex justify-between">
            <span className="font-medium text-gray-700">Last Updated:</span>
            <span className="text-gray-600">{formatDateTime(document.updatedAt, language)}</span>
          </div>
        </div>
      </div>
    </div>
  )
}
