import { RegistrationConfiguration } from '@/types/configuration'
import { logger } from '@/utils/logger'

class ConfigurationService {
  private baseUrl: string

  constructor() {
    // Use relative URL like other services in the project
    this.baseUrl = '/api/configuration'
  }

  /**
   * Fetches registration configuration from the API
   */
  async getRegistrationConfiguration(): Promise<RegistrationConfiguration> {
    try {
      const response = await fetch(`${this.baseUrl}/registration`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      })

      if (!response.ok) {
        throw new Error(`Failed to fetch configuration: ${response.status} ${response.statusText}`)
      }

      const configuration: RegistrationConfiguration = await response.json()

      // Validate the response structure
      this.validateConfiguration(configuration)

      return configuration
    } catch (error) {
      logger.error('Error fetching registration configuration:', error)
      throw error
    }
  }

  /**
   * Validates the configuration object structure
   */
  private validateConfiguration(config: any): void {
    if (!config) {
      throw new Error('Configuration is null or undefined')
    }

    if (!config.passwordRequirements) {
      throw new Error('Missing passwordRequirements in configuration')
    }

    if (!config.userFieldRequirements) {
      throw new Error('Missing userFieldRequirements in configuration')
    }

    if (!config.securitySettings) {
      throw new Error('Missing securitySettings in configuration')
    }

    // Validate password requirements
    const { passwordRequirements } = config
    if (typeof passwordRequirements.requiredLength !== 'number') {
      throw new Error('Invalid requiredLength in passwordRequirements')
    }

    // Validate user field requirements
    const requiredFields = ['email', 'userName', 'firstName', 'lastName', 'password', 'confirmPassword']
    for (const field of requiredFields) {
      if (!config.userFieldRequirements[field]) {
        throw new Error(`Missing ${field} in userFieldRequirements`)
      }
    }
  }

  /**
   * Returns a default configuration as fallback
   */
  getDefaultConfiguration(): RegistrationConfiguration {
    return {
      passwordRequirements: {
        requiredLength: 6,
        requireDigit: true,
        requireLowercase: true,
        requireUppercase: true,
        requireNonAlphanumeric: false,
        validationMessage: 'Password must contain at least 6 characters, uppercase letter, lowercase letter and digit',
        validationRules: [
          'auth.validation.password_min_length#6',
          'auth.validation.password_require_uppercase',
          'auth.validation.password_require_lowercase',
          'auth.validation.password_require_digit'
        ]
      },
      userFieldRequirements: {
        email: {
          required: true,
          maxLength: 256,
          pattern: '^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$',
          validationMessage: 'auth.validation.email_invalid'
        },
        userName: {
          required: true,
          minLength: 3,
          maxLength: 50,
          validationMessage: 'auth.validation.username_min_length'
        },
        firstName: {
          required: true,
          maxLength: 100,
          validationMessage: 'auth.validation.first_name_required'
        },
        lastName: {
          required: true,
          maxLength: 100,
          validationMessage: 'auth.validation.last_name_required'
        },
        password: {
          required: true,
          minLength: 6,
          validationMessage: 'auth.validation.password_required'
        },
        confirmPassword: {
          required: true,
          validationMessage: 'auth.validation.password_mismatch'
        }
      },
      securitySettings: {
        requireEmailConfirmation: false,
        requireUniqueEmail: true,
        requireTermsAcceptance: true,
        lockout: {
          allowedForNewUsers: true,
          maxFailedAccessAttempts: 5,
          defaultLockoutTimeSpanMinutes: 15
        }
      }
    }
  }
}

export const configurationService = new ConfigurationService()
export default configurationService
