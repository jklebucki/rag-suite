// All code comments must be written in English, regardless of the conversation language.

export type SettingsTab = 'llm' | 'user' | 'feedback'

export interface UserFilters {
  searchText: string
  selectedRoles: string[]
  activeStatus: 'all' | 'active' | 'inactive'
  createdDateFrom: string
  createdDateTo: string
  lastLoginFrom: string
  lastLoginTo: string
}

export interface DatePreset {
  label: string
  days: number
}

export interface PasswordStrength {
  score: number
  checks: {
    length: boolean
    uppercase: boolean
    lowercase: boolean
    number: boolean
    special: boolean
  }
  label: 'Weak' | 'Fair' | 'Good' | 'Strong'
}

export interface LlmFormErrors {
  url?: string
  maxTokens?: string
  temperature?: string
  model?: string
  timeoutMinutes?: string
}
