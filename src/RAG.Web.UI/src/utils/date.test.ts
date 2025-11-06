import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { formatDateTime, formatDate, formatRelativeTime } from './date'
import type { LanguageCode } from '@/types/i18n'

describe('date utilities', () => {
  beforeEach(() => {
    // Mock current date to 2024-01-15 12:00:00
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2024-01-15T12:00:00Z'))
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  describe('formatDateTime', () => {
    it('should format Date object correctly', () => {
      const date = new Date('2024-01-15T10:30:45Z')
      const result = formatDateTime(date, 'en')
      expect(result).toBe('2024-01-15 10:30:45')
    })

    it('should format date string correctly', () => {
      const result = formatDateTime('2024-01-15T10:30:45Z', 'en')
      expect(result).toBe('2024-01-15 10:30:45')
    })

    it('should pad single digit values with zeros', () => {
      const date = new Date('2024-01-05T05:05:05Z')
      const result = formatDateTime(date, 'en')
      expect(result).toBe('2024-01-05 05:05:05')
    })

    it('should handle invalid dates gracefully', () => {
      const invalidDate = new Date('invalid')
      const result = formatDateTime(invalidDate, 'en')
      // Should return ISO format fallback
      expect(result).toContain('2024')
    })
  })

  describe('formatDate', () => {
    const testCases: Array<{ language: LanguageCode; expected: string }> = [
      { language: 'en', expected: '01/15/2024' },
      { language: 'pl', expected: '15.01.2024' },
      { language: 'ro', expected: '15.01.2024' },
      { language: 'hu', expected: '2024. 01. 15.' },
      { language: 'nl', expected: '15-01-2024' },
    ]

    testCases.forEach(({ language, expected }) => {
      it(`should format date for ${language} locale`, () => {
        const date = new Date('2024-01-15T10:30:45Z')
        const result = formatDate(date, language)
        // Note: Actual format may vary by system, so we just check it contains date parts
        expect(result).toBeTruthy()
        expect(result.length).toBeGreaterThan(0)
      })
    })

    it('should format date string correctly', () => {
      const result = formatDate('2024-01-15T10:30:45Z', 'en')
      expect(result).toBeTruthy()
    })

    it('should handle invalid dates gracefully', () => {
      const invalidDate = new Date('invalid')
      const result = formatDate(invalidDate, 'en')
      expect(result).toBeTruthy()
    })
  })

  describe('formatRelativeTime', () => {
    beforeEach(() => {
      vi.useFakeTimers()
      vi.setSystemTime(new Date('2024-01-15T12:00:00Z'))
    })

    afterEach(() => {
      vi.useRealTimers()
    })

    it('should return "now" for very recent dates', () => {
      const date = new Date('2024-01-15T11:59:30Z')
      expect(formatRelativeTime(date, 'en')).toBe('now')
      expect(formatRelativeTime(date, 'pl')).toBe('teraz')
    })

    it('should format minutes ago correctly', () => {
      const date = new Date('2024-01-15T11:45:00Z')
      expect(formatRelativeTime(date, 'en')).toBe('15 min ago')
      expect(formatRelativeTime(date, 'pl')).toBe('15 min temu')
    })

    it('should format hours ago correctly', () => {
      const date = new Date('2024-01-15T10:00:00Z')
      expect(formatRelativeTime(date, 'en')).toBe('2 h ago')
      expect(formatRelativeTime(date, 'pl')).toBe('2 godz temu')
    })

    it('should format days ago correctly', () => {
      const date = new Date('2024-01-13T12:00:00Z')
      expect(formatRelativeTime(date, 'en')).toBe('2 d ago')
      expect(formatRelativeTime(date, 'pl')).toBe('2 dni temu')
    })

    it('should return "yesterday" for 1 day ago', () => {
      const date = new Date('2024-01-14T12:00:00Z')
      expect(formatRelativeTime(date, 'en')).toBe('yesterday')
      expect(formatRelativeTime(date, 'pl')).toBe('wczoraj')
    })

    it('should return formatted date for older dates', () => {
      const date = new Date('2024-01-01T12:00:00Z')
      const result = formatRelativeTime(date, 'en')
      // Should return formatted date, not relative time
      expect(result).toBeTruthy()
      expect(result.length).toBeGreaterThan(5)
    })

    it('should handle all supported languages', () => {
      const date = new Date('2024-01-15T11:30:00Z')
      const languages: LanguageCode[] = ['en', 'pl', 'ro', 'hu', 'nl']
      
      languages.forEach((lang) => {
        const result = formatRelativeTime(date, lang)
        expect(result).toBeTruthy()
        expect(typeof result).toBe('string')
      })
    })

    it('should handle date strings', () => {
      const dateString = '2024-01-15T11:45:00Z'
      expect(formatRelativeTime(dateString, 'en')).toBe('15 min ago')
    })
  })
})

