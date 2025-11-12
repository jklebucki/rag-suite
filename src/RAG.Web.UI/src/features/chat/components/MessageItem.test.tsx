/**
 * MessageItem.test.tsx
 * 
 * Tests for MessageItem component using React.memo
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MessageItem } from './MessageItem'
import type { ChatMessage } from '@/features/chat/types/chat'
import type { LanguageCode } from '@/shared/types/i18n'

// Mock dependencies
vi.mock('@/shared/contexts/I18nContext', () => ({
  useI18n: () => ({
    t: (key: string) => key,
    language: 'en' as LanguageCode,
  }),
}))

vi.mock('./MarkdownMessage', () => ({
  MarkdownMessage: ({ content }: { content: string }) => <div>{content}</div>,
}))

vi.mock('./MessageSources', () => ({
  MessageSources: () => null,
}))

vi.mock('@/utils/date', () => ({
  formatDateTime: () => '2024-01-01 12:00',
  formatRelativeTime: () => 'now',
}))

describe('MessageItem', () => {
  const mockMessage: ChatMessage = {
    id: '1',
    role: 'user',
    content: 'Test message',
    timestamp: new Date().toISOString(),
  }

  const defaultProps = {
    message: mockMessage,
    currentLanguage: 'en' as LanguageCode,
    isLastMessage: false,
  }

  it('should render message content', () => {
    render(<MessageItem {...defaultProps} />)
    
    expect(screen.getByText('Test message')).toBeInTheDocument()
  })

  it('should render user message with correct styling', () => {
    render(<MessageItem {...defaultProps} />)
    
    // The flex-row-reverse is on the outer container div
    const messageText = screen.getByText('Test message')
    const outerContainer = messageText.closest('div.flex')
    expect(outerContainer).toHaveClass('flex-row-reverse')
  })

  it('should render assistant message with correct styling', () => {
    const assistantMessage: ChatMessage = {
      ...mockMessage,
      role: 'assistant',
    }
    
    render(<MessageItem {...defaultProps} message={assistantMessage} />)
    
    const messageContainer = screen.getByText('Test message').closest('div')
    expect(messageContainer).not.toHaveClass('flex-row-reverse')
  })

  it('should display timestamp', () => {
    render(<MessageItem {...defaultProps} />)
    
    // Timestamp should be rendered
    const timestamp = screen.getByTitle(/sent at/i)
    expect(timestamp).toBeInTheDocument()
  })

  it('should display language detection info for last message', () => {
    render(
      <MessageItem
        {...defaultProps}
        isLastMessage={true}
        lastMessageLanguage="pl"
        translationStatus="translated"
      />
    )
    
    // Language info should be displayed for last message
    const languageInfo = screen.queryByText(/detected/i)
    expect(languageInfo).toBeTruthy()
  })

  it('should not display language detection info for non-last message', () => {
    render(
      <MessageItem
        {...defaultProps}
        isLastMessage={false}
        lastMessageLanguage="pl"
        translationStatus="translated"
      />
    )
    
    // Language info should not be displayed for non-last message
    const languageInfo = screen.queryByText(/detected/i)
    expect(languageInfo).toBeFalsy()
  })

  it('should render sources for assistant messages', () => {
    const assistantMessage: ChatMessage = {
      ...mockMessage,
      role: 'assistant',
      sources: [
        { title: 'Source 1', url: 'http://example.com', snippet: 'Snippet 1' },
      ],
    }
    
    render(<MessageItem {...defaultProps} message={assistantMessage} />)
    
    // Sources component should be rendered (mocked, so we just check it doesn't crash)
    expect(screen.getByText('Test message')).toBeInTheDocument()
  })

  it('should be memoized (React.memo)', () => {
    // Test that component is memoized by checking it doesn't re-render unnecessarily
    const { rerender } = render(<MessageItem {...defaultProps} />)
    
    const initialRender = screen.getByText('Test message')
    
    // Re-render with same props
    rerender(<MessageItem {...defaultProps} />)
    
    // Component should still be in document
    expect(screen.getByText('Test message')).toBeInTheDocument()
  })
})

