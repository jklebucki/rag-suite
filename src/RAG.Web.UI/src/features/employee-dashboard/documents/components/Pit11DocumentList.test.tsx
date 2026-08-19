import { render, screen } from '@testing-library/react'
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
  },
]

describe('Pit11DocumentList', () => {
  it('renders the empty state', () => {
    render(
      <Pit11DocumentList
        documents={[]}
      />
    )

    expect(screen.getByText('employeeDashboard.pit11.empty')).toBeInTheDocument()
  })

  it('renders PIT-11 data with a disabled download button', () => {
    render(<Pit11DocumentList documents={documents} />)

    expect(screen.getByText('PIT-11 za 2025')).toBeInTheDocument()
    expect(screen.getByText('2025')).toBeInTheDocument()

    expect(
      screen.getByRole('button', { name: 'employeeDashboard.pit11.download' })
    ).toBeDisabled()
  })
})
