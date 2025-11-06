/**
 * Reusable validation utilities
 * Centralized validation functions for form inputs
 */

/**
 * Validates email format
 */
export function validateEmail(email: string): boolean {
  if (!email || !email.trim()) {
    return false
  }
  // More strict email regex that rejects consecutive dots and invalid formats
  const emailRegex = /^[^\s@.]+(\.[^\s@.]+)*@[^\s@.]+(\.[^\s@.]+)*\.[a-zA-Z]{2,}$/
  return emailRegex.test(email.trim())
}

/**
 * Validates password strength
 */
export function validatePassword(
  password: string,
  minLength: number = 6,
  maxLength?: number
): { isValid: boolean; error?: string } {
  if (!password) {
    return { isValid: false, error: 'Password is required' }
  }

  if (password.length < minLength) {
    return {
      isValid: false,
      error: `Password must be at least ${minLength} characters long`,
    }
  }

  if (maxLength && password.length > maxLength) {
    return {
      isValid: false,
      error: `Password cannot exceed ${maxLength} characters`,
    }
  }

  return { isValid: true }
}

/**
 * Validates that passwords match
 */
export function validatePasswordMatch(
  password: string,
  confirmPassword: string
): { isValid: boolean; error?: string } {
  if (password !== confirmPassword) {
    return { isValid: false, error: 'Passwords do not match' }
  }

  return { isValid: true }
}

/**
 * Validates required field
 */
export function validateRequired(
  value: string,
  fieldName: string = 'Field'
): { isValid: boolean; error?: string } {
  if (!value || !value.trim()) {
    return { isValid: false, error: `${fieldName} is required` }
  }

  return { isValid: true }
}

/**
 * Validates string length
 */
export function validateLength(
  value: string,
  minLength?: number,
  maxLength?: number,
  fieldName: string = 'Field'
): { isValid: boolean; error?: string } {
  if (minLength !== undefined && value.length < minLength) {
    return {
      isValid: false,
      error: `${fieldName} must be at least ${minLength} characters long`,
    }
  }

  if (maxLength !== undefined && value.length > maxLength) {
    return {
      isValid: false,
      error: `${fieldName} cannot exceed ${maxLength} characters`,
    }
  }

  return { isValid: true }
}

/**
 * Validates username format (alphanumeric, underscores, hyphens)
 */
export function validateUsername(username: string): { isValid: boolean; error?: string } {
  if (!username) {
    return { isValid: false, error: 'Username is required' }
  }

  const usernameRegex = /^[a-zA-Z0-9_-]+$/
  if (!usernameRegex.test(username)) {
    return {
      isValid: false,
      error: 'Username can only contain letters, numbers, underscores, and hyphens',
    }
  }

  return { isValid: true }
}

/**
 * Combines multiple validation results
 */
export function combineValidations(
  ...validations: Array<{ isValid: boolean; error?: string }>
): { isValid: boolean; errors: string[] } {
  const errors: string[] = []

  for (const validation of validations) {
    if (!validation.isValid && validation.error) {
      errors.push(validation.error)
    }
  }

  return {
    isValid: errors.length === 0,
    errors,
  }
}

/**
 * Creates a validation function for a specific field
 */
export function createFieldValidator<T>(
  fieldName: string,
  validators: Array<(value: T) => { isValid: boolean; error?: string }>
) {
  return (value: T): { isValid: boolean; errors: string[] } => {
    const errors: string[] = []

    for (const validator of validators) {
      const result = validator(value)
      if (!result.isValid && result.error) {
        errors.push(result.error)
      }
    }

    return {
      isValid: errors.length === 0,
      errors,
    }
  }
}

