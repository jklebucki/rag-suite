import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { MarkdownMessage } from './MarkdownMessage'

const { getArtifact } = vi.hoisted(() => ({
  getArtifact: vi.fn(),
}))

const renderMermaid = vi.fn().mockResolvedValue({
  svg: '<svg><text>Rendered diagram</text></svg>',
})

vi.mock('mermaid', () => ({
  default: {
    initialize: vi.fn(),
    render: renderMermaid,
  },
}))

vi.mock('@/shared/services/api/httpClients', () => ({
  apiHttpClient: {
    get: getArtifact,
  },
}))

vi.mock('@/shared/contexts/I18nContext', () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}))

describe('MarkdownMessage', () => {
  const originalCreateObjectUrl = URL.createObjectURL
  const originalRevokeObjectUrl = URL.revokeObjectURL
  const originalAnchorClick = HTMLAnchorElement.prototype.click

  beforeEach(() => {
    getArtifact.mockResolvedValue({
      data: new Blob(['artifact']),
      headers: { 'content-disposition': 'attachment; filename="ocr-result.docx"' },
    })
    URL.createObjectURL = vi.fn(() => 'blob:artifact')
    URL.revokeObjectURL = vi.fn()
    HTMLAnchorElement.prototype.click = vi.fn()
  })

  afterEach(() => {
    vi.clearAllMocks()
    URL.createObjectURL = originalCreateObjectUrl
    URL.revokeObjectURL = originalRevokeObjectUrl
    HTMLAnchorElement.prototype.click = originalAnchorClick
  })

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

  it('opens the diagram in a near-fullscreen modal', async () => {
    render(<MarkdownMessage content={'```mermaid\nflowchart LR\n  A --> B\n```'} />)

    fireEvent.click(await screen.findByRole('button', { name: 'Open Mermaid diagram in a larger view' }))

    const dialog = screen.getByRole('dialog')
    expect(dialog).toHaveClass('!w-[80vw]', 'h-[95vh]')
    expect(screen.getByRole('img', { name: 'Expanded Mermaid diagram' })).toBeInTheDocument()
  })

  it('downloads generated artifacts relative to the API client base path', async () => {
    render(<MarkdownMessage content="[Pobierz wynik](/api/user-chat/artifacts/artifact-1/download)" />)

    fireEvent.click(screen.getByRole('button', { name: 'Pobierz wynik' }))

    await waitFor(() => {
      expect(getArtifact).toHaveBeenCalledWith('/user-chat/artifacts/artifact-1/download', { responseType: 'blob' })
    })
    expect(HTMLAnchorElement.prototype.click).toHaveBeenCalledOnce()
  })

  it('keeps wide code blocks inside a horizontal scroll container', () => {
    render(<MarkdownMessage content={'```text\n' + 'x'.repeat(1000) + '\n```'} />)

    expect(screen.getByTestId('markdown-code-block')).toHaveClass('max-w-full', 'overflow-x-auto')
  })
})
