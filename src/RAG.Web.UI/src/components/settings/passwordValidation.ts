// All code comments must be written in English, regardless of the conversation language.

import type { PasswordStrength } from '../types'

/**
 * Calculate password strength based on various criteria
 */
export function getPasswordStrength(password: string): PasswordStrength {
  let strength = 0
  const checks = {
    length: password.length >= 8,
    uppercase: /[A-Z]/.test(password),
    lowercase: /[a-z]/.test(password),
    number: /\d/.test(password),
    special: /[!@#$%^&*(),.?":{}|<>]/.test(password)
  }

  strength = Object.values(checks).filter(Boolean).length

  const label = 
    strength <= 2 ? 'Weak' : 
    strength <= 3 ? 'Fair' : 
    strength <= 4 ? 'Good' : 'Strong'

  return {
    score: strength,
    checks,
    label
  }
}

/**
 * Validate password meets minimum requirements
 */
export function validatePassword(password: string, confirmPassword: string): {
  isValid: boolean
  errors: string[]
} {
  const errors: string[] = []

  if (!password) {
    errors.push('Password is required')
  }

  if (password.length < 8) {
    errors.push('Password must be at least 8 characters long')
  }

  if (password !== confirmPassword) {
    errors.push('Passwords do not match')
  }

  const strength = getPasswordStrength(password)
  if (strength.score < 3) {
    errors.push('Password is too weak')
  }

  return {
    isValid: errors.length === 0,
    errors
  }
}
