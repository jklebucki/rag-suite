import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { MarkdownMessage } from './MarkdownMessage'

const renderMermaid = vi.fn().mockResolvedValue({
  svg: '<svg><text>Rendered diagram</text></svg>',
})

vi.mock('mermaid', () => ({
  default: {
    initialize: vi.fn(),
    render: renderMermaid,
  },
}))

vi.mock('@/shared/contexts/I18nContext', () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}))

describe('MarkdownMessage', () => {
  it('renders mermaid code blocks as diagrams', async () => {
    render(<MarkdownMessage content={'```mermaid\nflowchart LR\n  A --> B\n```'} />)

    expect(await screen.findByRole('img', { name: 'Mermaid diagram' })).toHaveTextContent('Rendered diagram')
    expect(renderMermaid).toHaveBeenCalledWith(expect.stringMatching(/^mermaid-/), 'flowchart LR\n  A --> B')
  })

  it('shows the source when Mermaid cannot render a diagram', async () => {
    renderMermaid.mockRejectedValueOnce(new Error('Invalid diagram'))

    render(<MarkdownMessage content={'```mermaid\nnot a diagram\n```'} />)

    await waitFor(() => {
      expect(screen.getByText('Unable to render Mermaid diagram.')).toBeInTheDocument()
    })
    expect(screen.getByText('not a diagram')).toBeInTheDocument()
  })

  it('opens the diagram in an 80 percent viewport modal', async () => {
    render(<MarkdownMessage content={'```mermaid\nflowchart LR\n  A --> B\n```'} />)

    fireEvent.click(await screen.findByRole('button', { name: 'Open Mermaid diagram in a larger view' }))

    const dialog = screen.getByRole('dialog')
    expect(dialog).toHaveClass('!w-[80vw]', 'h-[80vh]')
    expect(screen.getByRole('img', { name: 'Expanded Mermaid diagram' })).toBeInTheDocument()
  })
})
