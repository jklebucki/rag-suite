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
    errors.url = 'settings.llm.validation.url_required'
  } else if (!settings.url.startsWith('http://') && !settings.url.startsWith('https://')) {
    errors.url = 'settings.llm.validation.url_protocol'
  }

  if (settings.maxTokens <= 0) {
    errors.maxTokens = 'settings.llm.validation.max_tokens'
  }

  if (settings.temperature < 0 || settings.temperature > 2) {
    errors.temperature = 'settings.llm.validation.temperature'
  }

  if (!settings.model.trim()) {
    errors.model = 'settings.llm.validation.model_required'
  }

  if (settings.timeoutMinutes <= 0) {
    errors.timeoutMinutes = 'settings.llm.validation.timeout'
  }

  return {
    isValid: Object.keys(errors).length === 0,
    errors
  }
}
