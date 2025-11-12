/**
 * SearchResultItem.test.tsx
 * 
 * Tests for SearchResultItem component using React.memo
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { SearchResultItem } from './SearchResults'
import type { SearchResult } from '../types/search'
import type { LanguageCode } from '@/shared/types/i18n'

// Mock dependencies
vi.mock('@/shared/services/file.service', () => ({
  default: {
    downloadFile: vi.fn(),
  },
}))

vi.mock('@/utils/date', () => ({
  formatDate: () => '2024-01-01',
}))

vi.mock('@/utils/logger', async () => {
  const actual = await vi.importActual<typeof import('@/utils/logger')>('@/utils/logger')
  return {
    ...actual,
    logger: {
      error: vi.fn(),
    },
  }
})

describe('SearchResultItem', () => {
  const mockResult: SearchResult = {
    id: '1',
    title: 'Test Document',
    content: 'Test content',
    score: 85,
    source: 'test-source',
    documentType: 'PDF',
    updatedAt: new Date(),
    createdAt: new Date(),
    metadata: {},
  }

  const mockOnViewDetails = vi.fn()
  const mockOnViewPDF = vi.fn()

  const defaultProps = {
    result: mockResult,
    onViewDetails: mockOnViewDetails,
    onViewPDF: mockOnViewPDF,
    language: 'en' as LanguageCode,
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('should render result title', () => {
    render(<SearchResultItem {...defaultProps} />)
    
    expect(screen.getByText('Test Document')).toBeInTheDocument()
  })

  it('should render result content', () => {
    render(<SearchResultItem {...defaultProps} />)
    
    expect(screen.getByText('Test content')).toBeInTheDocument()
  })

  it('should render score', () => {
    render(<SearchResultItem {...defaultProps} />)
    
    expect(screen.getByText('85% match')).toBeInTheDocument()
  })

  it('should render document type', () => {
    render(<SearchResultItem {...defaultProps} />)
    
    expect(screen.getByText('PDF')).toBeInTheDocument()
  })

  it('should render source', () => {
    render(<SearchResultItem {...defaultProps} />)
    
    expect(screen.getByText('test-source')).toBeInTheDocument()
  })

  it('should render reconstructed badge when metadata.reconstructed is true', () => {
    const reconstructedResult: SearchResult = {
      ...mockResult,
      metadata: { reconstructed: true },
    }
    
    render(<SearchResultItem {...defaultProps} result={reconstructedResult} />)
    
    expect(screen.getByText('Reconstructed')).toBeInTheDocument()
  })

  it('should render chunks info when available', () => {
    const chunkedResult: SearchResult = {
      ...mockResult,
      metadata: {
        chunksFound: 3,
        totalChunks: 5,
      },
    }
    
    render(<SearchResultItem {...defaultProps} result={chunkedResult} />)
    
    expect(screen.getByText(/3\/5 chunks/i)).toBeInTheDocument()
  })

  it('should call onViewDetails when View Details is clicked', async () => {
    const user = userEvent.setup()
    render(<SearchResultItem {...defaultProps} />)
    
    const viewDetailsButton = screen.getByText('View Details')
    await user.click(viewDetailsButton)
    
    expect(mockOnViewDetails).toHaveBeenCalledTimes(1)
  })

  it('should call onViewPDF when PDF view button is clicked', async () => {
    const user = userEvent.setup()
    const resultWithFile: SearchResult = {
      ...mockResult,
      filePath: '/path/to/file.pdf',
    }
    
    render(<SearchResultItem {...defaultProps} result={resultWithFile} />)
    
    const viewPDFButton = screen.getByTitle('View PDF')
    await user.click(viewPDFButton)
    
    expect(mockOnViewPDF).toHaveBeenCalledWith('/path/to/file.pdf')
  })

  it('should render highlights when available', () => {
    const resultWithHighlights: SearchResult = {
      ...mockResult,
      metadata: {
        highlights: '<mark>highlighted</mark> text',
      },
    }
    
    render(<SearchResultItem {...defaultProps} result={resultWithHighlights} />)
    
    const highlightsDiv = document.querySelector('.search-highlights')
    expect(highlightsDiv).toBeInTheDocument()
    expect(highlightsDiv?.innerHTML).toContain('<mark>highlighted</mark>')
  })

  it('should be memoized (React.memo)', () => {
    const { rerender } = render(<SearchResultItem {...defaultProps} />)
    
    expect(screen.getByText('Test Document')).toBeInTheDocument()
    
    // Re-render with same props
    rerender(<SearchResultItem {...defaultProps} />)
    
    // Component should still render correctly
    expect(screen.getByText('Test Document')).toBeInTheDocument()
  })

  it('should update when result props change', () => {
    const { rerender } = render(<SearchResultItem {...defaultProps} />)
    
    expect(screen.getByText('Test Document')).toBeInTheDocument()
    
    const updatedResult: SearchResult = {
      ...mockResult,
      title: 'Updated Document',
      score: 90,
    }
    
    rerender(<SearchResultItem {...defaultProps} result={updatedResult} />)
    
    expect(screen.getByText('Updated Document')).toBeInTheDocument()
    expect(screen.getByText('90% match')).toBeInTheDocument()
  })
})

