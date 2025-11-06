import { describe, it, expect } from 'vitest'
import {
  validateEmail,
  validatePassword,
  validatePasswordMatch,
  validateRequired,
  validateLength,
  validateUsername,
  combineValidations,
  createFieldValidator,
} from './validation'

describe('validation', () => {
  describe('validateEmail', () => {
    it('should validate correct email addresses', () => {
      expect(validateEmail('test@example.com')).toBe(true)
      expect(validateEmail('user.name@example.co.uk')).toBe(true)
      expect(validateEmail('user+tag@example.com')).toBe(true)
      expect(validateEmail('user123@example-domain.com')).toBe(true)
    })

    it('should reject invalid email addresses', () => {
      expect(validateEmail('invalid')).toBe(false)
      expect(validateEmail('invalid@')).toBe(false)
      expect(validateEmail('@example.com')).toBe(false)
      expect(validateEmail('invalid@example')).toBe(false)
      expect(validateEmail('')).toBe(false)
      expect(validateEmail('invalid..email@example.com')).toBe(false)
    })
  })

  describe('validatePassword', () => {
    it('should validate password with default min length (6)', () => {
      expect(validatePassword('password123').isValid).toBe(true)
      expect(validatePassword('123456').isValid).toBe(true)
    })

    it('should reject password shorter than min length', () => {
      const result = validatePassword('12345')
      expect(result.isValid).toBe(false)
      expect(result.error).toContain('at least 6 characters')
    })

    it('should validate password with custom min length', () => {
      expect(validatePassword('12345678', 8).isValid).toBe(true)
      const result = validatePassword('1234567', 8)
      expect(result.isValid).toBe(false)
      expect(result.error).toContain('at least 8 characters')
    })

    it('should validate password with max length', () => {
      expect(validatePassword('123456', 6, 10).isValid).toBe(true)
      const result = validatePassword('12345678901', 6, 10)
      expect(result.isValid).toBe(false)
      expect(result.error).toContain('cannot exceed 10 characters')
    })

    it('should reject empty password', () => {
      const result = validatePassword('')
      expect(result.isValid).toBe(false)
      expect(result.error).toBe('Password is required')
    })
  })

  describe('validatePasswordMatch', () => {
    it('should validate matching passwords', () => {
      expect(validatePasswordMatch('password123', 'password123').isValid).toBe(true)
    })

    it('should reject non-matching passwords', () => {
      const result = validatePasswordMatch('password123', 'password456')
      expect(result.isValid).toBe(false)
      expect(result.error).toBe('Passwords do not match')
    })
  })

  describe('validateRequired', () => {
    it('should validate non-empty values', () => {
      expect(validateRequired('value').isValid).toBe(true)
      expect(validateRequired('  value  ').isValid).toBe(true)
    })

    it('should reject empty values', () => {
      const result1 = validateRequired('')
      expect(result1.isValid).toBe(false)
      expect(result1.error).toBe('Field is required')

      const result2 = validateRequired('   ')
      expect(result2.isValid).toBe(false)
      expect(result2.error).toBe('Field is required')
    })

    it('should use custom field name in error message', () => {
      const result = validateRequired('', 'Email')
      expect(result.isValid).toBe(false)
      expect(result.error).toBe('Email is required')
    })
  })

  describe('validateLength', () => {
    it('should validate length within range', () => {
      expect(validateLength('test', 2, 10).isValid).toBe(true)
      expect(validateLength('test', 4, 4).isValid).toBe(true)
    })

    it('should reject string shorter than min length', () => {
      const result = validateLength('test', 5)
      expect(result.isValid).toBe(false)
      expect(result.error).toContain('at least 5 characters')
    })

    it('should reject string longer than max length', () => {
      const result = validateLength('test', 2, 3)
      expect(result.isValid).toBe(false)
      expect(result.error).toContain('cannot exceed 3 characters')
    })

    it('should use custom field name in error message', () => {
      const result = validateLength('test', 5, undefined, 'Username')
      expect(result.isValid).toBe(false)
      expect(result.error).toContain('Username')
    })
  })

  describe('validateUsername', () => {
    it('should validate correct usernames', () => {
      expect(validateUsername('username').isValid).toBe(true)
      expect(validateUsername('user_name').isValid).toBe(true)
      expect(validateUsername('user-name').isValid).toBe(true)
      expect(validateUsername('user123').isValid).toBe(true)
      expect(validateUsername('User123_Name').isValid).toBe(true)
    })

    it('should reject invalid usernames', () => {
      expect(validateUsername('user name').isValid).toBe(false)
      expect(validateUsername('user@name').isValid).toBe(false)
      expect(validateUsername('user.name').isValid).toBe(false)
      expect(validateUsername('user!name').isValid).toBe(false)
    })

    it('should reject empty username', () => {
      const result = validateUsername('')
      expect(result.isValid).toBe(false)
      expect(result.error).toBe('Username is required')
    })
  })

  describe('combineValidations', () => {
    it('should return valid when all validations pass', () => {
      const result = combineValidations(
        { isValid: true },
        { isValid: true },
        { isValid: true }
      )
      expect(result.isValid).toBe(true)
      expect(result.errors).toHaveLength(0)
    })

    it('should return invalid and collect errors when validations fail', () => {
      const result = combineValidations(
        { isValid: true },
        { isValid: false, error: 'Error 1' },
        { isValid: false, error: 'Error 2' }
      )
      expect(result.isValid).toBe(false)
      expect(result.errors).toEqual(['Error 1', 'Error 2'])
    })

    it('should ignore validations without error messages', () => {
      const result = combineValidations(
        { isValid: false },
        { isValid: false, error: 'Error 1' }
      )
      expect(result.isValid).toBe(false)
      expect(result.errors).toEqual(['Error 1'])
    })
  })

  describe('createFieldValidator', () => {
    it('should create a validator that combines multiple validators', () => {
      const validateField = createFieldValidator('TestField', [
        (value: string) => validateRequired(value, 'TestField'),
        (value: string) => validateLength(value, 3, 10, 'TestField'),
      ])

      const result1 = validateField('test')
      expect(result1.isValid).toBe(true)
      expect(result1.errors).toHaveLength(0)

      const result2 = validateField('')
      expect(result2.isValid).toBe(false)
      expect(result2.errors.length).toBeGreaterThan(0)

      const result3 = validateField('ab')
      expect(result3.isValid).toBe(false)
      expect(result3.errors.length).toBeGreaterThan(0)
    })
  })
})

