import { describe, expect, it } from 'vitest'
import { downloadPit11File, getPit11Documents } from './documentsMockData'

describe('PIT-11 mock service', () => {
  it('returns documents sorted by newest tax year', async () => {
    const documents = await getPit11Documents('employee-1')

    expect(documents.map((document) => document.taxYear)).toEqual([2025, 2024, 2023])
  })

  it('prepares a PDF file for download', async () => {
    const file = await downloadPit11File('employee-1', 'pit11-2025')

    expect(file.fileName).toBe('pit-11-2025.pdf')
    expect(file.blob.type).toBe('application/pdf')
    expect(file.blob.size).toBeGreaterThan(0)
  })

  it('rejects an unknown document id', async () => {
    await expect(downloadPit11File('employee-1', 'missing')).rejects.toThrow(
      'PIT-11 document not found: missing'
    )
  })
})
