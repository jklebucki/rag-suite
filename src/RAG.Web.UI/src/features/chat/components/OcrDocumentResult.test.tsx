import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { OcrDocumentResult } from './OcrDocumentResult'

const { getChatDocumentMarkdown } = vi.hoisted(() => ({
  getChatDocumentMarkdown: vi.fn(),
}))

vi.mock('@/features/chat/services/chat.service', () => ({
  getChatDocumentMarkdown,
  downloadChatDocumentExport: vi.fn(),
}))

vi.mock('./MarkdownMessage', () => ({
  MarkdownMessage: ({ content }: { content: string }) => <div data-testid="rendered-markdown">{content}</div>,
}))

describe('OcrDocumentResult', () => {
  it('loads and displays the canonical Markdown only when the user expands the document', async () => {
    getChatDocumentMarkdown.mockResolvedValue('| Product | Seats |\n| --- | ---: |\n| ERP | 13 |')

    render(
      <OcrDocumentResult
        sessionId="session-1"
        document={{
          id: 'document-1',
          fileName: 'contract.pdf',
          contentType: 'application/pdf',
          sizeBytes: 128,
          pageCount: 1,
          provider: 'docling',
        }}
      />,
    )

    expect(getChatDocumentMarkdown).not.toHaveBeenCalled()
    fireEvent.click(screen.getByRole('button', { name: 'Pokaż Markdown' }))

    await waitFor(() => {
      expect(getChatDocumentMarkdown).toHaveBeenCalledWith('session-1', 'document-1')
    })
    expect(screen.getByTestId('rendered-markdown')).toHaveTextContent('| ERP | 13 |')
  })
})
