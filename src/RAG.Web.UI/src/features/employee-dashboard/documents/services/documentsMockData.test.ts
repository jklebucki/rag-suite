import { describe, expect, it } from 'vitest'
import { getPit11Documents } from './documentsMockData'

describe('PIT-11 mock service', () => {
  it('returns documents sorted by newest tax year', async () => {
    const documents = await getPit11Documents('employee-1')

    expect(documents.map((document) => document.taxYear)).toEqual([2025, 2024, 2023])
  })
})
