import React, { useState } from 'react'
import { Document, Page, pdfjs } from 'react-pdf'
import { ChevronLeft, ChevronRight, ZoomIn, ZoomOut, RotateCw, Download, X } from 'lucide-react'
import { Modal } from './Modal'
import fileService from '@/shared/services/fileService'
import { logger } from '@/utils/logger'

// Configure PDF.js worker
//pdfjs.GlobalWorkerOptions.workerSrc = new URL('/pdf.worker.min.js', import.meta.url).toString()
pdfjs.GlobalWorkerOptions.workerSrc = `https://unpkg.com/pdfjs-dist@${pdfjs.version}/build/pdf.worker.min.mjs`;

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
  const [pdfBlob, setPdfBlob] = useState<Blob | null>(null)
  const [optimalScale, setOptimalScale] = useState(1.0)

  // Calculate optimal scale based on window size
  const calculateOptimalScale = React.useCallback(() => {
    const windowWidth = window.innerWidth
    const windowHeight = window.innerHeight - 80 // Subtract toolbar height

    // A4 page dimensions in pixels at 100% scale (assuming 72 DPI)
    const a4Width = 595 // A4 width in points
    const a4Height = 842 // A4 height in points

    // Calculate scale to fit width with some margin
    const maxWidth = windowWidth - 80 // 40px margin on each side
    const maxHeight = windowHeight - 80 // 40px margin on each side

    const scaleX = maxWidth / a4Width
    const scaleY = maxHeight / a4Height

    // Use the smaller scale to ensure the page fits
    const optimal = Math.min(scaleX, scaleY, 1.0) // Don't scale up beyond 100%
    return Math.max(optimal, 0.1) // Minimum scale of 10%
  }, [])

  // Update optimal scale on window resize
  React.useEffect(() => {
    const handleResize = () => {
      const newOptimalScale = calculateOptimalScale()
      setOptimalScale(newOptimalScale)
    }

    if (isOpen) {
      handleResize()
      window.addEventListener('resize', handleResize)
    }

    return () => {
      window.removeEventListener('resize', handleResize)
    }
  }, [isOpen, calculateOptimalScale])

  // Set initial scale to optimal when PDF loads
  React.useEffect(() => {
    if (numPages && optimalScale) {
      setScale(optimalScale)
    }
  }, [numPages, optimalScale])

  const loadPDF = React.useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      // Use the convert endpoint to get PDF
      const response = await fileService.downloadFileWithConversion(filePath, false)
      if (response.data) {
        // Store the blob for download functionality
        setPdfBlob(response.data)
        // Create blob URL from the response for PDF viewer
        const url = URL.createObjectURL(response.data)
        setPdfUrl(url)
      }
    } catch (err) {
      logger.error('Failed to load PDF:', err)
      setError('Failed to load PDF document')
    } finally {
      setLoading(false)
    }
  }, [filePath])

  React.useEffect(() => {
    if (!isOpen) {
      setPdfUrl(null)
      setPdfBlob(null)
      setError(null)
      setPageNumber(1)
      setScale(1.0)
      setRotation(0)
      return
    }

    if (filePath) {
      void loadPDF()
    }
  }, [filePath, isOpen, loadPDF])

  React.useEffect(() => {
    return () => {
      if (pdfUrl) {
        URL.revokeObjectURL(pdfUrl)
      }
    }
  }, [pdfUrl])

  const onDocumentLoadSuccess = ({ numPages }: { numPages: number }) => {
    setNumPages(numPages)
    setPageNumber(1)
  }

  const onDocumentLoadError = (error: Error) => {
    logger.error('PDF load error:', error)
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

  const resetZoom = () => {
    setScale(optimalScale)
  }

  const handleDownload = async () => {
    if (!pdfBlob) {
      logger.error('No PDF blob available for download')
      return
    }

    try {
      // Create download link using the stored blob
      const url = URL.createObjectURL(pdfBlob)
      const link = document.createElement('a')
      link.href = url
      link.setAttribute('download', `${filePath.split('/').pop()?.replace(/\.[^/.]+$/, '') || 'document'}.pdf`)
      document.body.appendChild(link)
      link.click()
      link.remove()
      URL.revokeObjectURL(url)
    } catch (err) {
      logger.error('Download failed:', err)
    }
  }

  const handleClose = () => {
    if (pdfUrl) {
      URL.revokeObjectURL(pdfUrl)
    }
    setPdfUrl(null)
    setPdfBlob(null)
    setError(null)
    setPageNumber(1)
    setScale(1.0)
    setRotation(0)
    onClose()
  }

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title={title || 'PDF Viewer'}
      size="xl"
      fullscreen={true}
    >
      <div className="flex flex-col h-full bg-gray-900">
        {/* Toolbar - fullscreen version */}
        <div className="flex items-center justify-between p-4 bg-gray-800 text-white border-b border-gray-700">
          <div className="flex items-center gap-4">
            {/* Close button */}
            <button
              onClick={handleClose}
              className="p-2 hover:bg-gray-700 rounded-lg transition-colors"
              title="Close PDF Viewer"
            >
              <X className="h-5 w-5" />
            </button>

            {/* Navigation */}
            <div className="flex items-center gap-2">
              <button
                onClick={goToPrevPage}
                disabled={pageNumber <= 1}
                className="p-2 hover:bg-gray-700 rounded disabled:opacity-50 disabled:cursor-not-allowed"
                title="Previous page"
              >
                <ChevronLeft className="h-4 w-4" />
              </button>

              <span className="text-sm min-w-[80px] text-center">
                {pageNumber} / {numPages || '?'}
              </span>

              <button
                onClick={goToNextPage}
                disabled={pageNumber >= (numPages || 1)}
                className="p-2 hover:bg-gray-700 rounded disabled:opacity-50 disabled:cursor-not-allowed"
                title="Next page"
              >
                <ChevronRight className="h-4 w-4" />
              </button>
            </div>
          </div>

          <div className="flex items-center gap-2">
            {/* Zoom controls */}
            <button
              onClick={zoomOut}
              className="p-2 hover:bg-gray-700 rounded"
              title="Zoom out"
            >
              <ZoomOut className="h-4 w-4" />
            </button>

            <button
              onClick={resetZoom}
              className="px-3 py-2 hover:bg-gray-700 rounded text-sm"
              title="Fit to window"
            >
              Fit
            </button>

            <span className="text-sm min-w-[60px] text-center">
              {Math.round(scale * 100)}%
            </span>

            <button
              onClick={zoomIn}
              className="p-2 hover:bg-gray-700 rounded"
              title="Zoom in"
            >
              <ZoomIn className="h-4 w-4" />
            </button>

            {/* Rotate */}
            <button
              onClick={rotate}
              className="p-2 hover:bg-gray-700 rounded"
              title="Rotate"
            >
              <RotateCw className="h-4 w-4" />
            </button>

            {/* Download */}
            <button
              onClick={handleDownload}
              className="p-2 hover:bg-gray-700 rounded text-blue-400 hover:text-blue-300"
              title="Download PDF"
            >
              <Download className="h-4 w-4" />
            </button>
          </div>
        </div>

        {/* PDF Content - fullscreen */}
        <div className="flex-1 overflow-auto bg-gray-100 p-4">
          {loading && (
            <div className="flex items-center justify-center h-full">
              <div className="animate-spin rounded-full h-16 w-16 border-b-4 border-gray-800"></div>
              <span className="ml-4 text-gray-600 text-lg">Loading PDF...</span>
            </div>
          )}

          {error && (
            <div className="flex items-center justify-center h-full">
              <div className="text-center">
                <X className="h-24 w-24 text-red-500 mx-auto mb-4" />
                <p className="text-red-600 text-xl">{error}</p>
              </div>
            </div>
          )}

          {pdfUrl && !loading && !error && (
            <div className="flex justify-center min-h-full">
              <Document
                file={pdfUrl}
                onLoadSuccess={onDocumentLoadSuccess}
                onLoadError={onDocumentLoadError}
                loading={
                  <div className="flex items-center justify-center py-12">
                    <div className="animate-spin rounded-full h-12 w-12 border-b-4 border-gray-800"></div>
                    <span className="ml-4 text-gray-600 text-lg">Loading document...</span>
                  </div>
                }
                error={
                  <div className="text-center py-12">
                    <X className="h-16 w-16 text-red-500 mx-auto mb-4" />
                    <p className="text-red-600 text-lg">Failed to load PDF</p>
                  </div>
                }
              >
                <Page
                  pageNumber={pageNumber}
                  scale={scale}
                  rotate={rotation}
                  renderTextLayer={false}
                  renderAnnotationLayer={false}
                  loading={
                    <div className="flex items-center justify-center py-12">
                      <div className="animate-spin rounded-full h-12 w-12 border-b-4 border-gray-800"></div>
                      <span className="ml-4 text-gray-600 text-lg">Loading page...</span>
                    </div>
                  }
                  error={
                    <div className="text-center py-12">
                      <X className="h-16 w-16 text-red-500 mx-auto mb-4" />
                      <p className="text-red-600 text-lg">Failed to load page</p>
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