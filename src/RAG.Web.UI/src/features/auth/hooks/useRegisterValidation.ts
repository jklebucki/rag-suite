import { useConfiguration, usePasswordValidation } from '@/shared/contexts/ConfigurationContext'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { TranslationKeys } from '@/shared/types/i18n'
import { ALLOWED_USER_NAME_CHARACTERS, validateUserNameCharacters } from '@/utils/usernameSuggestion'

interface RegisterFormData {
  firstName: string
  lastName: string
  userName: string
  email: string
  password: string
  confirmPassword: string
  acceptTerms: boolean
}

type LengthRule = { value: number; message: string }
type PatternRule = { value: RegExp; message: string }
type Validator<TValue> = (value: TValue, formValues: RegisterFormData) => string | boolean

export interface ValidationRule<TValue> {
  required?: string
  minLength?: TValue extends string ? LengthRule : never
  maxLength?: TValue extends string ? LengthRule : never
  pattern?: TValue extends string ? PatternRule : never
  validate?: Record<string, Validator<TValue>>
}

/**
 * Hook that provides validation rules for the registration form
 * based on dynamic configuration from the backend
 */
export function useRegisterValidation() {
  const { configuration } = useConfiguration()
  const { validatePassword } = usePasswordValidation()
  const { t } = useI18n()

  if (!configuration) {
    return null
  }

  const { userFieldRequirements } = configuration

  const firstNameMaxLength = userFieldRequirements.firstName.maxLength || 100
  const lastNameMaxLength = userFieldRequirements.lastName.maxLength || 100
  const userNameMinLength = userFieldRequirements.userName.minLength || 3
  const userNameMaxLength = userFieldRequirements.userName.maxLength || 50
  const emailMaxLength = userFieldRequirements.email.maxLength || 256

  const validationRules: { [K in keyof RegisterFormData]: ValidationRule<RegisterFormData[K]> } = {
    firstName: {
      required: t('auth.validation.first_name_required'),
      maxLength: {
        value: firstNameMaxLength,
        message: t('auth.validation.first_name_max_length', { max: String(firstNameMaxLength) }),
      },
      validate: {
        noWhitespace: (value: string, _formValues: RegisterFormData) =>
          value.trim().length > 0 || t('auth.validation.first_name_whitespace'),
      },
    },

    lastName: {
      required: t('auth.validation.last_name_required'),
      maxLength: {
        value: lastNameMaxLength,
        message: t('auth.validation.last_name_max_length', { max: String(lastNameMaxLength) }),
      },
      validate: {
        noWhitespace: (value: string, _formValues: RegisterFormData) =>
          value.trim().length > 0 || t('auth.validation.last_name_whitespace'),
      },
    },

    userName: {
      required: t('auth.validation.username_required'),
      minLength: {
        value: userNameMinLength,
        message: t('auth.validation.username_min_length', { min: String(userNameMinLength) }),
      },
      maxLength: {
        value: userNameMaxLength,
        message: t('auth.validation.username_max_length', { max: String(userNameMaxLength) }),
      },
      validate: {
        noWhitespace: (value: string, _formValues: RegisterFormData) =>
          value.trim().length > 0 || t('auth.validation.username_whitespace'),
        // Mirrors ASP.NET Identity's AllowedUserNameCharacters, which otherwise
        // rejects the account server-side with an opaque 400.
        allowedCharacters: (value: string, _formValues: RegisterFormData) => {
          const result = validateUserNameCharacters(value)
          if (result.isValid) return true

          // Invisible characters (most often a space) need a readable label.
          const invalid = result.invalidCharacters
            .map((char) => (char === ' ' ? t('auth.validation.username_char_space') : `"${char}"`))
            .join(', ')

          return invalid.length > 0
            ? t('auth.validation.username_invalid_chars_found', {
                allowed: ALLOWED_USER_NAME_CHARACTERS,
                invalid,
              })
            : t('auth.validation.username_invalid_chars', { allowed: ALLOWED_USER_NAME_CHARACTERS })
        },
      },
    },

    email: {
      required: t('auth.validation.email_required'),
      pattern: {
        value: userFieldRequirements.email.pattern
          ? new RegExp(userFieldRequirements.email.pattern)
          : /^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$/i,
        message: t('auth.validation.email_invalid'),
      },
      maxLength: {
        value: emailMaxLength,
        message: t('auth.validation.email_max_length', { max: String(emailMaxLength) }),
      },
    },

    password: {
      required: t('auth.validation.password_required'),
      validate: {
        dynamicValidation: (value: string, _formValues: RegisterFormData) => {
          const result = validatePassword(value)
          if (result.isValid) return true

          // Errors are emitted as 'translation.key#param' by the configuration context.
          const [errorKey, param] = result.errors[0].split('#')
          return t(errorKey as keyof TranslationKeys, param ? { min: param } : undefined)
        },
      },
    },

    confirmPassword: {
      required: t('auth.validation.confirm_password_required'),
      validate: {
        passwordMatch: (value: string, formValues: RegisterFormData) =>
          value === formValues.password || t('auth.validation.passwords_do_not_match'),
      },
    },

    acceptTerms: {
      validate: {
        mustAccept: (value: boolean, _formValues: RegisterFormData) =>
          value === true || t('auth.validation.terms_required'),
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
  const { t } = useI18n()

  if (!configuration) {
    return []
  }

  const { passwordRequirements } = configuration
  const requirements: string[] = []

  requirements.push(
    t('auth.requirements.password_length', { min: String(passwordRequirements.requiredLength) })
  )

  if (passwordRequirements.requireDigit) {
    requirements.push(t('auth.requirements.password_digit'))
  }

  if (passwordRequirements.requireUppercase) {
    requirements.push(t('auth.requirements.password_uppercase'))
  }

  if (passwordRequirements.requireLowercase) {
    requirements.push(t('auth.requirements.password_lowercase'))
  }

  if (passwordRequirements.requireNonAlphanumeric) {
    requirements.push(t('auth.requirements.password_special'))
  }

  return requirements
}

/**
 * Gets username requirement hints for display to the user
 */
export function useUserNameRequirements() {
  const { configuration } = useConfiguration()
  const { t } = useI18n()

  if (!configuration) {
    return []
  }

  const { userName } = configuration.userFieldRequirements

  return [
    t('auth.requirements.username_length', {
      min: String(userName.minLength || 3),
      max: String(userName.maxLength || 50),
    }),
    t('auth.requirements.username_allowed_chars', { allowed: ALLOWED_USER_NAME_CHARACTERS }),
    t('auth.requirements.username_no_spaces'),
  ]
}
