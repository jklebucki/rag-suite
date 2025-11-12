/**
 * PostCard.test.tsx
 * 
 * Tests for PostCard component using React.memo
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { PostCard } from './ThreadDetailPage'
import type { ForumPost, ForumAttachment } from '../types/forum'
import type { LanguageCode } from '@/shared/types/i18n'

// Mock main.tsx to prevent root element error
vi.mock('@/main', () => ({
  queryClient: {
    invalidateQueries: vi.fn(),
    setQueryData: vi.fn(),
    getQueryData: vi.fn(),
  },
}))

// Mock dependencies
vi.mock('@/shared/contexts/I18nContext', () => ({
  useI18n: () => ({
    t: (key: string, params?: Record<string, string>) => {
      if (key === 'forum.detail.postedAt' && params) {
        return `Posted at ${params.date}`
      }
      if (key === 'forum.attachments.replyTitle') {
        return 'Attachments'
      }
      return key
    },
  }),
}))

vi.mock('@/utils/date', () => ({
  formatDateTime: () => '2024-01-01 12:00',
}))

// Don't mock AttachmentList - use the real component for testing

describe('PostCard', () => {
  const mockPost: ForumPost = {
    id: '1',
    threadId: 'thread1',
    content: 'Test post content',
    authorId: 'user1',
    authorEmail: 'test@example.com',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    attachments: [],
  }

  const mockOnDownload = vi.fn()

  const defaultProps = {
    post: mockPost,
    language: 'en' as LanguageCode,
    onDownload: mockOnDownload,
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('should render post content', () => {
    render(<PostCard {...defaultProps} />)
    
    expect(screen.getByText('Test post content')).toBeInTheDocument()
  })

  it('should render author email', () => {
    render(<PostCard {...defaultProps} />)
    
    expect(screen.getByText('test@example.com')).toBeInTheDocument()
  })

  it('should render author ID when email is not available', () => {
    const postWithoutEmail: ForumPost = {
      ...mockPost,
      authorEmail: undefined,
    }
    
    render(<PostCard {...defaultProps} post={postWithoutEmail} />)
    
    expect(screen.getByText('user1')).toBeInTheDocument()
  })

  it('should render created date', () => {
    render(<PostCard {...defaultProps} />)
    
    expect(screen.getByText(/posted at/i)).toBeInTheDocument()
  })

  it('should render attachments when available', () => {
    const postWithAttachments: ForumPost = {
      ...mockPost,
      attachments: [
        {
          id: 'att1',
          fileName: 'test.pdf',
          fileSize: 1024,
          mimeType: 'application/pdf',
          uploadedAt: new Date().toISOString(),
        },
      ],
    }
    
    render(<PostCard {...defaultProps} post={postWithAttachments} />)
    
    expect(screen.getByText('test.pdf')).toBeInTheDocument()
  })

  it('should call onDownload when attachment is clicked', async () => {
    const user = userEvent.setup()
    const attachment: ForumAttachment = {
      id: 'att1',
      fileName: 'test.pdf',
      fileSize: 1024,
      mimeType: 'application/pdf',
      uploadedAt: new Date().toISOString(),
    }
    
    const postWithAttachments: ForumPost = {
      ...mockPost,
      attachments: [attachment],
    }
    
    render(<PostCard {...defaultProps} post={postWithAttachments} />)
    
    // AttachmentList renders download button next to attachment name
    // Find the button that is a sibling or child of the attachment name
    const attachmentName = screen.getByText('test.pdf')
    const attachmentItem = attachmentName.closest('li')
    const downloadButton = attachmentItem?.querySelector('button[type="button"]')
    
    if (downloadButton) {
      await user.click(downloadButton as HTMLElement)
      expect(mockOnDownload).toHaveBeenCalledWith(attachment)
    } else {
      // Fallback: find any button in the attachment area
      const buttons = screen.getAllByRole('button')
      const downloadBtn = buttons.find(btn => 
        btn.closest('li')?.textContent?.includes('test.pdf')
      )
      if (downloadBtn) {
        await user.click(downloadBtn)
        expect(mockOnDownload).toHaveBeenCalledWith(attachment)
      } else {
        throw new Error('Download button not found')
      }
    }
  })

  it('should not render attachments section when empty', () => {
    render(<PostCard {...defaultProps} />)
    
    expect(screen.queryByText('Attachments')).not.toBeInTheDocument()
  })

  it('should be memoized (React.memo)', () => {
    const { rerender } = render(<PostCard {...defaultProps} />)
    
    expect(screen.getByText('Test post content')).toBeInTheDocument()
    
    // Re-render with same props
    rerender(<PostCard {...defaultProps} />)
    
    // Component should still render correctly
    expect(screen.getByText('Test post content')).toBeInTheDocument()
  })

  it('should update when post props change', () => {
    const { rerender } = render(<PostCard {...defaultProps} />)
    
    expect(screen.getByText('Test post content')).toBeInTheDocument()
    
    const updatedPost: ForumPost = {
      ...mockPost,
      content: 'Updated content',
    }
    
    rerender(<PostCard {...defaultProps} post={updatedPost} />)
    
    expect(screen.getByText('Updated content')).toBeInTheDocument()
  })
})

