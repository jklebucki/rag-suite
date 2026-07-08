import React from 'react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, act } from '@testing-library/react'
import { MessageInput } from './MessageInput'

// Mock i18n
vi.mock('@/shared/contexts/I18nContext', () => ({
  useI18n: () => ({
    t: (k: string) => k,
  }),
}))

describe('MessageInput', () => {
  const defaultProps = {
    message: '',
    onMessageChange: vi.fn(),
    onSendMessage: vi.fn(),
    isSending: false,
    useDocumentSearch: false,
    onUseDocumentSearchChange: vi.fn(),
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders with default rows = 3', () => {
    render(<MessageInput {...defaultProps} />)
    const textarea = screen.getByPlaceholderText('chat.input.placeholder') as HTMLTextAreaElement
    expect(textarea).toBeInTheDocument()
    expect(textarea.getAttribute('rows')).toBe('3')
  })

  it('Enter without Shift triggers onSendMessage', async () => {
    const onSend = vi.fn()
    render(<MessageInput {...defaultProps} message="hello" onSendMessage={onSend} />)
    const textarea = screen.getByPlaceholderText('chat.input.placeholder') as HTMLTextAreaElement

    await act(async () => {
      fireEvent.keyDown(textarea, { key: 'Enter', code: 'Enter', shiftKey: false })
    })

    expect(onSend).toHaveBeenCalled()
  })

  it('Shift+Enter inserts newline and does not send', async () => {
    const onSend = vi.fn()
    const onMsg = vi.fn()
    render(<MessageInput {...defaultProps} message={'line1'} onSendMessage={onSend} onMessageChange={onMsg} />)
    const textarea = screen.getByPlaceholderText('chat.input.placeholder') as HTMLTextAreaElement

    await act(async () => {
      fireEvent.keyDown(textarea, { key: 'Enter', code: 'Enter', shiftKey: true })
    })

    expect(onSend).not.toHaveBeenCalled()
  })

  it('autosizes up to max rows and sets overflow when exceeded', async () => {
    // Render the input with some initial text
    const { rerender } = render(<MessageInput {...defaultProps} message={'short'} />)
    const textarea = screen.getByPlaceholderText('chat.input.placeholder') as HTMLTextAreaElement

    // Provide an inline line-height so getComputedStyle returns px value
    textarea.style.lineHeight = '20px'

    // Simulate small scrollHeight (smaller than min rows)
    Object.defineProperty(textarea, 'scrollHeight', { value: 40, configurable: true })

    // trigger effect
    await act(async () => {
      rerender(<MessageInput {...defaultProps} message={'short'} />)
    })

    // minRows = 3 => height = 20 * 3 = 60
    expect(textarea.style.height).toBe('60px')
    expect(textarea.style.overflowY).toBe('hidden')

    // Now simulate large content that exceeds max (10 rows -> 200px)
    Object.defineProperty(textarea, 'scrollHeight', { value: 1000, configurable: true })
    await act(async () => {
      rerender(<MessageInput {...defaultProps} message={'big\n'.repeat(200)} />)
    })

    expect(textarea.style.height).toBe('200px')
    expect(textarea.style.overflowY).toBe('auto')
  })

  it('renders attachment chips only when attachments are provided', () => {
    const onRemoveAttachment = vi.fn()

    const { rerender } = render(<MessageInput {...defaultProps} />)
    expect(screen.queryByText('notes.md')).not.toBeInTheDocument()

    rerender(
      <MessageInput
        {...defaultProps}
        attachments={[
          {
            id: 'attachment-1',
            fileName: 'notes.md',
            contentType: 'text/markdown',
            sizeBytes: 128,
            tokenCount: 42,
            uploadedAt: new Date().toISOString(),
          },
        ]}
        onRemoveAttachment={onRemoveAttachment}
      />
    )

    expect(screen.getByText('notes.md')).toBeInTheDocument()
    expect(screen.getByText('42')).toBeInTheDocument()

    fireEvent.click(screen.getByLabelText('chat.attachments.remove'))
    expect(onRemoveAttachment).toHaveBeenCalledWith('attachment-1')
  })

  it('shows context usage and disables composer when session limit is reached', () => {
    render(
      <MessageInput
        {...defaultProps}
        message="blocked"
        contextUsage={{
          usedTokens: 9600,
          limitTokens: 9600,
          percentUsed: 100,
          isLimitExceeded: true,
          attachmentTokens: 0,
          attachmentLimitTokens: 12000,
          attachments: [],
        }}
      />
    )

    expect(screen.getByText('100%')).toBeInTheDocument()
    expect(screen.getByText('chat.context_limit_reached')).toBeInTheDocument()
    expect(screen.getByPlaceholderText('chat.context_limit_reached')).toBeDisabled()
    expect(screen.getByRole('button', { name: 'chat.send' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'chat.attachments.add' })).toBeDisabled()
  })
})
