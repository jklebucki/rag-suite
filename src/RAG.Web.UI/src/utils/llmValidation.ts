// All code comments must be written in English, regardless of the conversation language.

import type { LlmSettings } from '@/features/settings/types/llm'
import type { LlmFormErrors } from '@/features/settings/types/settings'

/**
 * Validate LLM settings form
 */
export function validateLlmSettings(settings: LlmSettings): {
  isValid: boolean
  errors: LlmFormErrors
} {
  const errors: LlmFormErrors = {}

  if (!settings.url.trim()) {
    errors.url = 'URL is required'
  } else if (!settings.url.startsWith('http://') && !settings.url.startsWith('https://')) {
    errors.url = 'URL must start with http:// or https://'
  }

  if (settings.maxTokens <= 0) {
    errors.maxTokens = 'Max tokens must be greater than 0'
  }

  if (settings.temperature < 0 || settings.temperature > 2) {
    errors.temperature = 'Temperature must be between 0 and 2'
  }

  if (!settings.model.trim()) {
    errors.model = 'Model is required'
  }

  if (settings.timeoutMinutes <= 0) {
    errors.timeoutMinutes = 'Timeout must be greater than 0'
  }

  return {
    isValid: Object.keys(errors).length === 0,
    errors
  }
}
