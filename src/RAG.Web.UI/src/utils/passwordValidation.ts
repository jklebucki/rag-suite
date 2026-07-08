// All code comments must be written in English, regardless of the conversation language.

import type { PasswordStrength } from '@/features/settings/types/settings'
import type { PasswordRequirements } from '@/features/settings/types/configuration'

const defaultPasswordRequirements: PasswordRequirements = {
  requiredLength: 8,
  requireDigit: true,
  requireLowercase: true,
  requireUppercase: true,
  requireNonAlphanumeric: true,
  validationMessage: '',
  validationRules: [],
}

/**
 * Calculate password strength based on various criteria
 */
export function getPasswordStrength(
  password: string,
  requirements: PasswordRequirements | null = null
): PasswordStrength {
  const effectiveRequirements = requirements ?? defaultPasswordRequirements
  let strength = 0
  const checks = {
    length: password.length >= effectiveRequirements.requiredLength,
    uppercase: /[A-Z]/.test(password),
    lowercase: /[a-z]/.test(password),
    number: /\d/.test(password),
    special: /[^a-zA-Z0-9]/.test(password)
  }

  const requiredChecks = [
    checks.length,
    !effectiveRequirements.requireUppercase || checks.uppercase,
    !effectiveRequirements.requireLowercase || checks.lowercase,
    !effectiveRequirements.requireDigit || checks.number,
    !effectiveRequirements.requireNonAlphanumeric || checks.special,
  ]

  strength = requiredChecks.filter(Boolean).length

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

export function validatePasswordRequirements(
  password: string,
  requirements: PasswordRequirements | null = null
): {
  isValid: boolean
  checks: PasswordStrength['checks']
} {
  const effectiveRequirements = requirements ?? defaultPasswordRequirements
  const checks = getPasswordStrength(password, effectiveRequirements).checks

  const isValid =
    checks.length &&
    (!effectiveRequirements.requireUppercase || checks.uppercase) &&
    (!effectiveRequirements.requireLowercase || checks.lowercase) &&
    (!effectiveRequirements.requireDigit || checks.number) &&
    (!effectiveRequirements.requireNonAlphanumeric || checks.special)

  return {
    isValid,
    checks,
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
  const passwordValidation = validatePasswordRequirements(password)

  if (!password) {
    errors.push('Password is required')
  }

  if (!passwordValidation.checks.length) {
    errors.push(`Password must be at least ${defaultPasswordRequirements.requiredLength} characters long`)
  }

  if (password !== confirmPassword) {
    errors.push('Passwords do not match')
  }

  if (!passwordValidation.isValid) {
    errors.push('Password does not meet the required complexity rules')
  }

  return {
    isValid: errors.length === 0,
    errors
  }
}
