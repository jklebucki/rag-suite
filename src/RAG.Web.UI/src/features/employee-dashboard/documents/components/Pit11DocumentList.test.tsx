import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import type { Pit11Document } from '../types/documentsTypes'
import { Pit11DocumentList } from './Pit11DocumentList'

vi.mock('@/shared/contexts/I18nContext', () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}))

const documents: Pit11Document[] = [
  {
    id: 'pit11-2025',
    name: 'PIT-11 za 2025',
    taxYear: 2025,
    generatedAt: '2026-02-15',
    fileName: 'pit-11-2025.pdf',
  },
]

describe('Pit11DocumentList', () => {
  it('renders the empty state', () => {
    render(
      <Pit11DocumentList
        documents={[]}
        downloadingDocumentId={null}
        onDownload={vi.fn()}
      />
    )

    expect(screen.getByText('employeeDashboard.pit11.empty')).toBeInTheDocument()
  })

  it('renders PIT-11 data and handles download', () => {
    const onDownload = vi.fn()
    render(
      <Pit11DocumentList
        documents={documents}
        downloadingDocumentId={null}
        onDownload={onDownload}
      />
    )

    expect(screen.getByText('PIT-11 za 2025')).toBeInTheDocument()
    expect(screen.getByText('2025')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'employeeDashboard.pit11.download' }))
    expect(onDownload).toHaveBeenCalledWith('pit11-2025')
  })
})
