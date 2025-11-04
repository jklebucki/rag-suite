import { useConfiguration, usePasswordValidation } from '@/contexts/ConfigurationContext'

interface RegisterFormData {
  firstName: string
  lastName: string
  userName: string
  email: string
  password: string
  confirmPassword: string
  acceptTerms: boolean
}

export interface ValidationRule {
  required?: string
  minLength?: { value: number; message: string }
  maxLength?: { value: number; message: string }
  pattern?: { value: RegExp; message: string }
  validate?: Record<string, (value: any, formValues?: any) => string | boolean>
}

/**
 * Hook that provides validation rules for the registration form
 * based on dynamic configuration from the backend
 */
export function useRegisterValidation() {
  const { configuration } = useConfiguration()
  const { validatePassword } = usePasswordValidation()

  if (!configuration) {
    return null
  }

  const { userFieldRequirements, passwordRequirements } = configuration

  const validationRules: Record<keyof RegisterFormData, ValidationRule> = {
    firstName: {
      required: 'First name is required',
      maxLength: {
        value: userFieldRequirements.firstName.maxLength || 100,
        message: `First name cannot exceed ${userFieldRequirements.firstName.maxLength || 100} characters`,
      },
      validate: {
        noWhitespace: (value: string) =>
          value.trim().length > 0 || 'First name cannot be only whitespace',
      },
    },

    lastName: {
      required: 'Last name is required',
      maxLength: {
        value: userFieldRequirements.lastName.maxLength || 100,
        message: `Last name cannot exceed ${userFieldRequirements.lastName.maxLength || 100} characters`,
      },
      validate: {
        noWhitespace: (value: string) =>
          value.trim().length > 0 || 'Last name cannot be only whitespace',
      },
    },

    userName: {
      required: 'Username is required',
      minLength: {
        value: userFieldRequirements.userName.minLength || 3,
        message: `Username must be at least ${userFieldRequirements.userName.minLength || 3} characters`,
      },
      maxLength: {
        value: userFieldRequirements.userName.maxLength || 50,
        message: `Username cannot exceed ${userFieldRequirements.userName.maxLength || 50} characters`,
      },
      validate: {
        noWhitespace: (value: string) =>
          value.trim().length > 0 || 'Username cannot be only whitespace',
      },
    },

    email: {
      required: 'Email is required',
      pattern: {
        value: userFieldRequirements.email.pattern
          ? new RegExp(userFieldRequirements.email.pattern)
          : /^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$/i,
        message: 'Invalid email address',
      },
      maxLength: {
        value: userFieldRequirements.email.maxLength || 256,
        message: `Email cannot exceed ${userFieldRequirements.email.maxLength || 256} characters`,
      },
    },

    password: {
      required: 'Password is required',
      validate: {
        dynamicValidation: (value: string) => {
          const result = validatePassword(value)
          if (result.isValid) return true

          // Map first validation error to user-friendly message
          const errorKey = result.errors[0]

          if (errorKey.includes('password_min_length')) {
            return `Password must be at least ${passwordRequirements.requiredLength} characters`
          } else if (errorKey.includes('password_require_digit')) {
            return 'Password must contain at least one digit'
          } else if (errorKey.includes('password_require_uppercase')) {
            return 'Password must contain at least one uppercase letter'
          } else if (errorKey.includes('password_require_lowercase')) {
            return 'Password must contain at least one lowercase letter'
          } else if (errorKey.includes('password_require_special')) {
            return 'Password must contain at least one special character'
          }

          return `Password must be at least ${passwordRequirements.requiredLength} characters`
        },
      },
    },

    confirmPassword: {
      required: 'Please confirm your password',
      validate: {
        passwordMatch: (value: string, formValues: RegisterFormData) =>
          value === formValues.password || 'Passwords do not match',
      },
    },

    acceptTerms: {
      validate: {
        mustAccept: (value: boolean) => value === true || 'You must accept the terms and conditions',
      },
    },
  }

  return validationRules
}

/**
 * Gets password requirement hints for display to the user
 */
export function usePasswordRequirements() {
  const { configuration } = useConfiguration()

  if (!configuration) {
    return []
  }

  const { passwordRequirements } = configuration
  const requirements: string[] = []

  requirements.push(`At least ${passwordRequirements.requiredLength} characters`)

  if (passwordRequirements.requireDigit) {
    requirements.push('One digit (0-9)')
  }

  if (passwordRequirements.requireUppercase) {
    requirements.push('One uppercase letter (A-Z)')
  }

  if (passwordRequirements.requireLowercase) {
    requirements.push('One lowercase letter (a-z)')
  }

  if (passwordRequirements.requireNonAlphanumeric) {
    requirements.push('One special character (!@#$%^&*)')
  }

  return requirements
}
