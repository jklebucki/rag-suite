import '@testing-library/jest-dom'
import { cleanup } from '@testing-library/react'
import { afterEach, vi } from 'vitest'

// Cleanup after each test
afterEach(() => {
  cleanup()
})

// Mock PDFViewerModal to avoid DOMMatrix error from pdfjs-dist
vi.mock('@/shared/components/ui/PDFViewerModal', () => ({
  PDFViewerModal: ({ isOpen, onClose, filePath, title }: any) => {
    if (!isOpen) return null
    const React = require('react')
    return React.createElement('div', { 'data-testid': 'pdf-viewer-modal' },
      React.createElement('div', null, `PDF Viewer: ${title || filePath}`),
      React.createElement('button', { onClick: onClose }, 'Close')
    )
  },
}))

// Mock react-pdf to avoid DOMMatrix error
vi.mock('react-pdf', () => ({
  Document: ({ children }: any) => {
    const React = require('react')
    return React.createElement('div', { 'data-testid': 'pdf-document' }, children)
  },
  Page: () => {
    const React = require('react')
    return React.createElement('div', { 'data-testid': 'pdf-page' }, 'PDF Page')
  },
  pdfjs: {
    GlobalWorkerOptions: {
      workerSrc: '',
    },
    version: '5.4.296',
  },
}))

// Mock window.matchMedia
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation(query => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
})

// Mock IntersectionObserver
global.IntersectionObserver = class IntersectionObserver {
  constructor() {}
  disconnect() {}
  observe() {}
  takeRecords() {
    return []
  }
  unobserve() {}
} as unknown as typeof IntersectionObserver

// Mock ResizeObserver
global.ResizeObserver = class ResizeObserver {
  constructor() {}
  disconnect() {}
  observe() {}
  unobserve() {}
} as unknown as typeof ResizeObserver

