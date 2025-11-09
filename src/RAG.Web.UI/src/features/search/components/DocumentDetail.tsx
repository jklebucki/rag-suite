import React from 'react'
import { Database, Tag, File, Download } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import { formatDateTime } from '@/utils/date'
import fileService from '@/shared/services/file.service'
import type { DocumentDetailResponse } from '@/features/search/types/search'
import { logger } from '@/utils/logger'

interface DocumentDetailProps {
  document: DocumentDetailResponse
}

export function DocumentDetail({ document }: DocumentDetailProps) {
  const { language } = useI18n()

  const handleDownload = async () => {
    const filePath = document.filePath || document.metadata?.file_path || document.metadata?.source_file
    if (filePath) {
      try {
        await fileService.downloadFile(filePath)
      } catch (error) {
        logger.error('Download failed:', error)
        // TODO: Show error toast
      }
    }
  }

  // Extract metadata for the table
  const fileExtension = document.metadata?.category || document.source || ''
  const fileSize = document.metadata?.file_size || 'Unknown'
  const lastModified = document.metadata?.last_modified || ''
  const indexedAt = document.metadata?.indexed_at || ''
  const filePath = document.filePath || document.metadata?.file_path || document.metadata?.source_file || ''

  // Chunk information
  const chunksFound = document.metadata?.chunksFound || document.metadata?.chunk_count
  const totalChunks = document.metadata?.totalChunks || document.metadata?.total_chunks
  const isReconstructed = document.metadata?.reconstructed || (chunksFound && totalChunks && chunksFound === totalChunks)
  const elasticsearchIndex = document.metadata?.index

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
        <div className="flex flex-col sm:flex-row sm:items-center gap-2 text-sm text-gray-600 mb-2">
          <div className="flex items-center gap-2 flex-wrap">
            <File className="h-4 w-4 flex-shrink-0" />
            <span className="font-mono text-xs bg-gray-100 px-2 py-1 rounded break-all">
              {filePath || 'Unknown path'}
            </span>
          </div>
          <div className="flex items-center gap-2 flex-wrap">
            {filePath && (
              <button
                onClick={handleDownload}
                className="text-primary-600 hover:text-primary-700 p-1 rounded"
                title="Download file"
              >
                <Download className="h-4 w-4" />
              </button>
            )}
            {isReconstructed && (
              <span className="px-2 py-1 bg-blue-100 text-blue-700 text-xs font-medium rounded whitespace-nowrap">
                Reconstructed Document
              </span>
            )}
            {chunksFound && totalChunks && (
              <span className="px-2 py-1 bg-orange-100 text-orange-700 text-xs font-medium rounded whitespace-nowrap">
                {chunksFound}/{totalChunks} chunks
              </span>
            )}
          </div>
        </div>
        <h1 className="text-lg sm:text-xl font-bold text-gray-900 break-words">{document.title || document.fileName}</h1>
        <div className="mt-2 flex flex-wrap items-center gap-2 sm:gap-4 text-xs sm:text-sm text-gray-600">
          {elasticsearchIndex && (
            <span className="flex items-center">
              <Database className="h-4 w-4 mr-1 flex-shrink-0" />
              <span className="truncate">Index: {elasticsearchIndex}</span>
            </span>
          )}
        </div>
      </div>

      {/* Scrollable content area */}
      <div className="flex-grow overflow-y-auto mb-4">
        <div className="bg-gray-50 rounded-lg p-3 sm:p-4 h-full">
          <div className="text-xs sm:text-sm text-gray-700 leading-relaxed whitespace-pre-wrap font-mono break-words">
            {document.fullContent || document.content}
          </div>
        </div>
      </div>

      {/* Metadata table at the bottom */}
      <div className="flex-shrink-0 bg-gray-50 rounded-lg p-3 sm:p-4">
        <h3 className="text-sm font-semibold text-gray-900 mb-3 flex items-center">
          <Tag className="h-4 w-4 mr-2" />
          File Information
        </h3>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 sm:gap-3 text-xs sm:text-sm">
          <div className="flex flex-col sm:flex-row sm:justify-between gap-1">
            <span className="font-medium text-gray-700">File Extension:</span>
            <span className="text-gray-600 break-all">{fileExtension}</span>
          </div>
          <div className="flex flex-col sm:flex-row sm:justify-between gap-1">
            <span className="font-medium text-gray-700">File Size:</span>
            <span className="text-gray-600">{formatBytes(fileSize)}</span>
          </div>
          {chunksFound && totalChunks && (
            <>
              <div className="flex flex-col sm:flex-row sm:justify-between gap-1">
                <span className="font-medium text-gray-700">Chunks Found:</span>
                <span className="text-gray-600">{chunksFound} of {totalChunks}</span>
              </div>
              <div className="flex flex-col sm:flex-row sm:justify-between gap-1">
                <span className="font-medium text-gray-700">Document Status:</span>
                <span className={`text-gray-600 ${isReconstructed ? 'text-blue-600' : ''}`}>
                  {isReconstructed ? 'Reconstructed' : 'Complete'}
                </span>
              </div>
            </>
          )}
          <div className="flex flex-col sm:flex-row sm:justify-between gap-1">
            <span className="font-medium text-gray-700">Last Modified:</span>
            <span className="text-gray-600 break-all">{formatMetadataDate(lastModified)}</span>
          </div>
          <div className="flex flex-col sm:flex-row sm:justify-between gap-1">
            <span className="font-medium text-gray-700">Indexed At:</span>
            <span className="text-gray-600 break-all">{formatMetadataDate(indexedAt)}</span>
          </div>
          <div className="flex flex-col sm:flex-row sm:justify-between gap-1">
            <span className="font-medium text-gray-700">Created:</span>
            <span className="text-gray-600 break-all">{formatDateTime(document.createdAt, language)}</span>
          </div>
          <div className="flex flex-col sm:flex-row sm:justify-between gap-1">
            <span className="font-medium text-gray-700">Last Updated:</span>
            <span className="text-gray-600 break-all">{formatDateTime(document.updatedAt, language)}</span>
          </div>
        </div>
      </div>
    </div>
  )
}
