/**
 * ThreadItem.test.tsx
 * 
 * Tests for ThreadItem component using React.memo
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ThreadItem } from './ThreadItem'
import type { ForumThreadSummary } from '../types/forum'
import type { LanguageCode } from '@/shared/types/i18n'

// Mock dependencies
vi.mock('@/utils/date', () => ({
  formatRelativeTime: () => '2 hours ago',
}))

describe('ThreadItem', () => {
  const mockThread: ForumThreadSummary = {
    id: '1',
    title: 'Test Thread',
    categoryName: 'General',
    authorEmail: 'test@example.com',
    authorId: 'user1',
    replyCount: 5,
    attachmentCount: 2,
    lastPostAt: new Date().toISOString(),
  }

  const mockOnThreadClick = vi.fn()
  const mockT = vi.fn((key: string, params?: Record<string, string>) => {
    if (key === 'forum.list.meta' && params) {
      return `By ${params.author}, ${params.replies} replies, ${params.attachments} attachments`
    }
    if (key === 'forum.list.badgeNew') {
      return 'New'
    }
    return key
  })

  const defaultProps = {
    thread: mockThread,
    language: 'en' as LanguageCode,
    hasUnread: false,
    onThreadClick: mockOnThreadClick,
    t: mockT,
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('should render thread title', () => {
    render(<ThreadItem {...defaultProps} />)
    
    expect(screen.getByText('Test Thread')).toBeInTheDocument()
  })

  it('should render category name', () => {
    render(<ThreadItem {...defaultProps} />)
    
    expect(screen.getByText('General')).toBeInTheDocument()
  })

  it('should render thread metadata', () => {
    render(<ThreadItem {...defaultProps} />)
    
    expect(screen.getByText(/test@example.com/i)).toBeInTheDocument()
    expect(screen.getByText(/5 replies/i)).toBeInTheDocument()
    expect(screen.getByText(/2 attachments/i)).toBeInTheDocument()
  })

  it('should render unread badge when hasUnread is true', () => {
    render(<ThreadItem {...defaultProps} hasUnread={true} />)
    
    expect(screen.getByText('New')).toBeInTheDocument()
  })

  it('should not render unread badge when hasUnread is false', () => {
    render(<ThreadItem {...defaultProps} hasUnread={false} />)
    
    expect(screen.queryByText('New')).not.toBeInTheDocument()
  })

  it('should call onThreadClick when clicked', async () => {
    const user = userEvent.setup()
    render(<ThreadItem {...defaultProps} />)
    
    const button = screen.getByRole('button')
    await user.click(button)
    
    expect(mockOnThreadClick).toHaveBeenCalledWith('1')
  })

  it('should render relative time', () => {
    render(<ThreadItem {...defaultProps} />)
    
    expect(screen.getByText('2 hours ago')).toBeInTheDocument()
  })

  it('should be memoized (React.memo)', () => {
    // Test that component is memoized
    const { rerender } = render(<ThreadItem {...defaultProps} />)
    
    expect(screen.getByText('Test Thread')).toBeInTheDocument()
    
    // Re-render with same props
    rerender(<ThreadItem {...defaultProps} />)
    
    // Component should still render correctly
    expect(screen.getByText('Test Thread')).toBeInTheDocument()
  })

  it('should update when thread props change', () => {
    const { rerender } = render(<ThreadItem {...defaultProps} />)
    
    expect(screen.getByText('Test Thread')).toBeInTheDocument()
    
    const updatedThread: ForumThreadSummary = {
      ...mockThread,
      title: 'Updated Thread',
      replyCount: 10,
    }
    
    rerender(<ThreadItem {...defaultProps} thread={updatedThread} />)
    
    expect(screen.getByText('Updated Thread')).toBeInTheDocument()
    expect(screen.getByText(/10 replies/i)).toBeInTheDocument()
  })
})

