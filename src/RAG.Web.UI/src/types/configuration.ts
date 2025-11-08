// Configuration types matching backend models
export interface RegistrationConfiguration {
  passwordRequirements: PasswordRequirements
  userFieldRequirements: UserFieldRequirements
  securitySettings: SecuritySettings
}

export interface PasswordRequirements {
  requiredLength: number
  requireDigit: boolean
  requireLowercase: boolean
  requireUppercase: boolean
  requireNonAlphanumeric: boolean
  validationMessage: string
  validationRules: string[]
}

export interface UserFieldRequirements {
  email: FieldRequirement
  userName: FieldRequirement
  firstName: FieldRequirement
  lastName: FieldRequirement
  password: FieldRequirement
  confirmPassword: FieldRequirement
}

export interface FieldRequirement {
  required: boolean
  minLength?: number | null
  maxLength?: number | null
  pattern?: string | null
  validationMessage: string
}

export interface SecuritySettings {
  requireEmailConfirmation: boolean
  requireUniqueEmail: boolean
  requireTermsAcceptance: boolean
  lockout: LockoutSettings
}

export interface LockoutSettings {
  allowedForNewUsers: boolean
  maxFailedAccessAttempts: number
  defaultLockoutTimeSpanMinutes: number
}

// State management types
export interface ConfigurationState {
  configuration: RegistrationConfiguration | null
  loading: boolean
  error: string | null
  lastFetched: Date | null
}

export interface ConfigurationContextType extends ConfigurationState {
  fetchConfiguration: () => Promise<void>
  refreshConfiguration: () => Promise<void>
  clearError: () => void
}
