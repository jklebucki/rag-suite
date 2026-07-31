import { describe, it, expect } from 'vitest'
import { removeDiacritics, suggestUserName, validateUserNameCharacters } from './usernameSuggestion'

describe('usernameSuggestion', () => {
  describe('removeDiacritics', () => {
    it('should transliterate Polish diacritics', () => {
      expect(removeDiacritics('Kłębucki')).toBe('Klebucki')
      expect(removeDiacritics('Jarosław')).toBe('Jaroslaw')
      expect(removeDiacritics('ąćęłńóśźż')).toBe('acelnoszz')
      expect(removeDiacritics('ĄĆĘŁŃÓŚŹŻ')).toBe('ACELNOSZZ')
    })

    it('should transliterate other Latin diacritics', () => {
      expect(removeDiacritics('Ștefănescu')).toBe('Stefanescu')
      expect(removeDiacritics('Győző')).toBe('Gyozo')
      expect(removeDiacritics('Müller')).toBe('Muller')
    })

    it('should leave plain ASCII untouched', () => {
      expect(removeDiacritics('Helbrecht')).toBe('Helbrecht')
      expect(removeDiacritics('')).toBe('')
    })
  })

  describe('suggestUserName', () => {
    it('should combine initials of given names with the surname', () => {
      expect(suggestUserName('Jarosław Piotr', 'Kłębucki-Pasisz')).toBe('jpklebucki-pasisz')
      expect(suggestUserName('Paulina', 'Helbrecht')).toBe('phelbrecht')
    })

    it('should treat a space-separated surname the same as a hyphenated one', () => {
      expect(suggestUserName('Jarosław Piotr', 'Kłębucki  Pasisz')).toBe('jpklebucki-pasisz')
    })

    it('should ignore surrounding whitespace and unsupported characters', () => {
      expect(suggestUserName('  Anna  ', ' Nowak ')).toBe('anowak')
      expect(suggestUserName("Jan", "O'Brien")).toBe('jobrien')
    })

    it('should handle missing parts gracefully', () => {
      expect(suggestUserName('', '')).toBe('')
      expect(suggestUserName('Anna', '')).toBe('a')
      expect(suggestUserName('', 'Nowak')).toBe('nowak')
    })
  })

  describe('validateUserNameCharacters', () => {
    it('should accept the characters allowed by ASP.NET Identity', () => {
      expect(validateUserNameCharacters('phelbrecht').isValid).toBe(true)
      expect(validateUserNameCharacters('jpklebucki-pasisz').isValid).toBe(true)
      expect(validateUserNameCharacters('user.name_1').isValid).toBe(true)
      expect(validateUserNameCharacters('user.name@host+1').isValid).toBe(true)
    })

    it('should reject a username containing a space and report it', () => {
      const result = validateUserNameCharacters('Paulina Helbrecht')

      expect(result.isValid).toBe(false)
      expect(result.invalidCharacters).toEqual([' '])
    })

    it('should reject accented characters and report each one once', () => {
      const result = validateUserNameCharacters('kłębucki')

      expect(result.isValid).toBe(false)
      expect(result.invalidCharacters).toEqual(['ł', 'ę'])
    })

    it('should treat an empty username as invalid', () => {
      expect(validateUserNameCharacters('').isValid).toBe(false)
    })
  })
})
