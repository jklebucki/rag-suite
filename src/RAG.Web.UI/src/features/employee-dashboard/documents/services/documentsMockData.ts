import type { Pit11Document } from '../types/documentsTypes'

const documents: Pit11Document[] = [
  {
    id: 'pit11-2025',
    name: 'PIT-11 za 2025',
    taxYear: 2025,
    generatedAt: '2026-02-15',
  },
  {
    id: 'pit11-2024',
    name: 'PIT-11 za 2024',
    taxYear: 2024,
    generatedAt: '2025-02-14',
  },
  {
    id: 'pit11-2023',
    name: 'PIT-11 za 2023',
    taxYear: 2023,
    generatedAt: '2024-02-15',
  },
]

export async function getPit11Documents(_userId: string): Promise<Pit11Document[]> {
  await new Promise((resolve) => setTimeout(resolve, 350))
  return structuredClone(documents).sort((a, b) => b.taxYear - a.taxYear)
}
