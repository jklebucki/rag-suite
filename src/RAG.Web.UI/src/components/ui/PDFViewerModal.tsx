import React, { useState } from 'react'
import { Document, Page, pdfjs } from 'react-pdf'
import { ChevronLeft, ChevronRight, ZoomIn, ZoomOut, RotateCw, Download, X } from 'lucide-react'
import { Modal } from './Modal'
import { apiClient } from '@/services/api'

// Configure PDF.js worker
pdfjs.GlobalWorkerOptions.workerSrc = `//unpkg.com/pdfjs-dist@${pdfjs.version}/build/pdf.worker.min.js`

interface PDFViewerModalProps {
  isOpen: boolean
  onClose: () => void
  filePath: string
  title?: string
}

export function PDFViewerModal({ isOpen, onClose, filePath, title }: PDFViewerModalProps) {
  const [numPages, setNumPages] = useState<number | null>(null)
  const [pageNumber, setPageNumber] = useState(1)
  const [scale, setScale] = useState(1.0)
  const [rotation, setRotation] = useState(0)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [pdfUrl, setPdfUrl] = useState<string | null>(null)

  React.useEffect(() => {
    if (isOpen && filePath) {
      loadPDF()
    } else {
      setPdfUrl(null)
      setError(null)
      setPageNumber(1)
      setScale(1.0)
      setRotation(0)
    }
  }, [isOpen, filePath])

  const loadPDF = async () => {
    setLoading(true)
    setError(null)
    try {
      // Use the convert endpoint to get PDF
      const response = await apiClient.downloadFileWithConversion(filePath, false)
      if (response.data) {
        // Create blob URL from the response
        const url = URL.createObjectURL(response.data)
        setPdfUrl(url)
      }
    } catch (err) {
      console.error('Failed to load PDF:', err)
      setError('Failed to load PDF document')
    } finally {
      setLoading(false)
    }
  }

  const onDocumentLoadSuccess = ({ numPages }: { numPages: number }) => {
    setNumPages(numPages)
    setPageNumber(1)
  }

  const onDocumentLoadError = (error: Error) => {
    console.error('PDF load error:', error)
    setError('Failed to load PDF document')
  }

  const goToPrevPage = () => {
    setPageNumber(prev => Math.max(prev - 1, 1))
  }

  const goToNextPage = () => {
    setPageNumber(prev => Math.min(prev + 1, numPages || 1))
  }

  const zoomIn = () => {
    setScale(prev => Math.min(prev + 0.25, 3.0))
  }

  const zoomOut = () => {
    setScale(prev => Math.max(prev - 0.25, 0.5))
  }

  const rotate = () => {
    setRotation(prev => (prev + 90) % 360)
  }

  const handleDownload = async () => {
    try {
      await apiClient.downloadFile(filePath)
    } catch (err) {
      console.error('Download failed:', err)
    }
  }

  const handleClose = () => {
    if (pdfUrl) {
      URL.revokeObjectURL(pdfUrl)
    }
    onClose()
  }

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title={title || 'PDF Viewer'}
      size="xl"
    >
      <div className="flex flex-col h-full">
        {/* Toolbar */}
        <div className="flex items-center justify-between p-4 border-b border-gray-200 bg-gray-50">
          <div className="flex items-center gap-2">
            <button
              onClick={goToPrevPage}
              disabled={pageNumber <= 1}
              className="p-2 hover:bg-gray-200 rounded disabled:opacity-50 disabled:cursor-not-allowed"
              title="Previous page"
            >
              <ChevronLeft className="h-4 w-4" />
            </button>

            <span className="text-sm text-gray-600 min-w-[80px] text-center">
              {pageNumber} / {numPages || '?'}
            </span>

            <button
              onClick={goToNextPage}
              disabled={pageNumber >= (numPages || 1)}
              className="p-2 hover:bg-gray-200 rounded disabled:opacity-50 disabled:cursor-not-allowed"
              title="Next page"
            >
              <ChevronRight className="h-4 w-4" />
            </button>
          </div>

          <div className="flex items-center gap-2">
            <button
              onClick={zoomOut}
              className="p-2 hover:bg-gray-200 rounded"
              title="Zoom out"
            >
              <ZoomOut className="h-4 w-4" />
            </button>

            <span className="text-sm text-gray-600 min-w-[60px] text-center">
              {Math.round(scale * 100)}%
            </span>

            <button
              onClick={zoomIn}
              className="p-2 hover:bg-gray-200 rounded"
              title="Zoom in"
            >
              <ZoomIn className="h-4 w-4" />
            </button>

            <button
              onClick={rotate}
              className="p-2 hover:bg-gray-200 rounded"
              title="Rotate"
            >
              <RotateCw className="h-4 w-4" />
            </button>

            <button
              onClick={handleDownload}
              className="p-2 hover:bg-gray-200 rounded text-primary-600 hover:text-primary-700"
              title="Download PDF"
            >
              <Download className="h-4 w-4" />
            </button>
          </div>
        </div>

        {/* PDF Content */}
        <div className="flex-1 overflow-auto p-4 bg-gray-100">
          {loading && (
            <div className="flex items-center justify-center h-full">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-500"></div>
              <span className="ml-4 text-gray-600">Loading PDF...</span>
            </div>
          )}

          {error && (
            <div className="flex items-center justify-center h-full">
              <div className="text-center">
                <X className="h-16 w-16 text-red-500 mx-auto mb-4" />
                <p className="text-red-600">{error}</p>
              </div>
            </div>
          )}

          {pdfUrl && !loading && !error && (
            <div className="flex justify-center">
              <Document
                file={pdfUrl}
                onLoadSuccess={onDocumentLoadSuccess}
                onLoadError={onDocumentLoadError}
                loading={
                  <div className="flex items-center justify-center py-8">
                    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-500"></div>
                    <span className="ml-2 text-gray-600">Loading document...</span>
                  </div>
                }
                error={
                  <div className="text-center py-8">
                    <X className="h-12 w-12 text-red-500 mx-auto mb-4" />
                    <p className="text-red-600">Failed to load PDF</p>
                  </div>
                }
              >
                <Page
                  pageNumber={pageNumber}
                  scale={scale}
                  rotate={rotation}
                  loading={
                    <div className="flex items-center justify-center py-8">
                      <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-500"></div>
                      <span className="ml-2 text-gray-600">Loading page...</span>
                    </div>
                  }
                  error={
                    <div className="text-center py-8">
                      <X className="h-12 w-12 text-red-500 mx-auto mb-4" />
                      <p className="text-red-600">Failed to load page</p>
                    </div>
                  }
                />
              </Document>
            </div>
          )}
        </div>
      </div>
    </Modal>
  )
}