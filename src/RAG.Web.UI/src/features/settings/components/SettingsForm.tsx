// All code comments must be written in English, regardless of the conversation language.

import React, { useState, useEffect, useCallback } from 'react'
import { Save, Loader2, Settings as SettingsIcon, Shield } from 'lucide-react'
import { useToast } from '@/shared/contexts'
import llmService from '@/features/settings/services/llm.service'
import type { LlmSettings, LlmSettingsRequest, AvailableModelsResponse } from '@/features/settings/types/llm'
import { validateLlmSettings } from '@/utils/llmValidation'
import { LlmFormField, ModelSelectField } from './LlmFormFields'
import type { LlmFormErrors } from '@/features/settings/types/settings'
import { logger } from '@/utils/logger'
import { useI18n } from '@/shared/contexts/I18nContext'

interface SettingsFormProps {
  onSettingsChange?: (settings: LlmSettings) => void
}

export function SettingsForm({ onSettingsChange }: SettingsFormProps) {
  const { addToast } = useToast()
  const { t } = useI18n()

  const [settings, setSettings] = useState<LlmSettings>({
    url: '',
    maxTokens: 3000,
    temperature: 0.7,
    model: '',
    isOllama: true,
    timeoutMinutes: 15,
    chatEndpoint: '/api/chat',
    generateEndpoint: '/api/generate'
  })

  const [availableModels, setAvailableModels] = useState<string[]>([])
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [loadingModels, setLoadingModels] = useState(false)
  const [validationErrors, setValidationErrors] = useState<LlmFormErrors>({})

  const loadSettings = useCallback(async () => {
    try {
      setLoading(true)
      const data = await llmService.getLlmSettings()
      setSettings(data)
      onSettingsChange?.(data)
    } catch (error) {
      logger.error('Failed to load LLM settings:', error)
      addToast({
        type: 'error',
        title: t('common.error'),
        message: t('settings.llm.messages.load_error')
      })
    } finally {
      setLoading(false)
    }
  }, [addToast, onSettingsChange, t])

  const loadAvailableModels = useCallback(async () => {
    if (!settings.url.trim()) return

    try {
      setLoadingModels(true)
      const data: AvailableModelsResponse = await llmService.getAvailableLlmModelsFromUrl(
        settings.url,
        settings.isOllama
      )
      setAvailableModels(data.models || [])
    } catch (error) {
      logger.error('Failed to load available models:', error)
      setAvailableModels([])
      // Don't show error toast for model loading as it's expected to fail for invalid URLs
    } finally {
      setLoadingModels(false)
    }
  }, [settings.isOllama, settings.url])

  // Load settings on component mount
  useEffect(() => {
    loadSettings()
  }, [loadSettings])

  // Load available models when URL changes
  useEffect(() => {
    if (settings.url.trim()) {
      loadAvailableModels()
    }
  }, [loadAvailableModels, settings.url])

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value, type } = e.target
    const checked = (e.target as HTMLInputElement).checked

    const newValue = type === 'checkbox' ? checked : type === 'number' ? parseFloat(value) || 0 : value

    setSettings(prev => ({
      ...prev,
      [name]: newValue
    }))

    // Clear validation error for this field
    if (name in validationErrors) {
      setValidationErrors(prev => {
        const updated = { ...prev }
        delete updated[name as keyof LlmFormErrors]
        return updated
      })
    }
  }

  const validateForm = (): boolean => {
    const { isValid, errors } = validateLlmSettings(settings)
    if (!isValid) {
      const translatedErrors = Object.entries(errors).reduce<LlmFormErrors>((acc, [key, value]) => {
        if (value) {
          acc[key as keyof LlmFormErrors] = t(value)
        }
        return acc
      }, {})
      setValidationErrors(translatedErrors)
    } else {
      setValidationErrors({})
    }
    return isValid
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) {
      return
    }

    try {
      setSaving(true)
      const request: LlmSettingsRequest = {
        url: settings.url,
        maxTokens: settings.maxTokens,
        temperature: settings.temperature,
        model: settings.model,
        isOllama: settings.isOllama,
        timeoutMinutes: settings.timeoutMinutes,
        chatEndpoint: settings.chatEndpoint,
        generateEndpoint: settings.generateEndpoint
      }

      await llmService.updateLlmSettings(request)
      addToast({
        type: 'success',
        title: t('common.success'),
        message: t('settings.llm.messages.update_success')
      })
      onSettingsChange?.(settings)
    } catch (error) {
      logger.error('Failed to update LLM settings:', error)
      addToast({
        type: 'error',
        title: t('common.error'),
        message: t('settings.llm.messages.update_error')
      })
    } finally {
      setSaving(false)
    }
  }

  const handleRefreshModels = () => {
    loadAvailableModels()
  }

  if (loading) {
    return (
      <div className="surface flex items-center justify-center gap-3 p-8">
        <Loader2 className="h-8 w-8 animate-spin text-primary-500" />
        <span className="text-gray-600 dark:text-gray-300">{t('settings.llm.loading')}</span>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-3">
        <div className="p-2 bg-primary-100 rounded-lg dark:bg-primary-900/30">
          <SettingsIcon className="h-6 w-6 text-primary-600 dark:text-primary-300" />
        </div>
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">{t('settings.llm.title')}</h1>
          <p className="text-gray-600 dark:text-gray-300">{t('settings.llm.subtitle')}</p>
        </div>
      </div>

      {/* Admin Notice */}
      <div className="surface-muted border border-amber-200 dark:border-amber-800/40 rounded-xl p-4">
        <div className="flex items-center gap-2">
          <Shield className="h-5 w-5 text-amber-600 dark:text-amber-400" />
          <span className="text-sm font-medium text-amber-700 dark:text-amber-300">
            {t('settings.llm.admin_notice_title')}
          </span>
        </div>
        <p className="mt-1 text-sm text-amber-700 dark:text-amber-200">
          {t('settings.llm.admin_notice_description')}
        </p>
      </div>

      {/* Settings Form */}
      <div className="surface p-6">
        <div className="mb-6">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-2">
            {t('settings.llm.section.model_configuration')}
          </h2>
          <p className="text-gray-600 dark:text-gray-300">
            {t('settings.llm.section.model_configuration_description')}
          </p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          <LlmFormField
            id="url"
            name="url"
            label={t('settings.llm.fields.url.label')}
            type="url"
            value={settings.url}
            onChange={handleChange}
            error={validationErrors.url}
            placeholder={t('settings.llm.fields.url.placeholder')}
          />

          <ModelSelectField
            value={settings.model}
            onChange={handleChange}
            availableModels={availableModels}
            onRefresh={handleRefreshModels}
            isLoading={loadingModels}
            disabled={!settings.url.trim()}
            error={validationErrors.model}
          />

          <LlmFormField
            id="maxTokens"
            name="maxTokens"
            label={t('settings.llm.fields.max_tokens.label')}
            type="number"
            value={settings.maxTokens}
            onChange={handleChange}
            error={validationErrors.maxTokens}
            min={1}
            max={100000}
          />

          <LlmFormField
            id="temperature"
            name="temperature"
            label={t('settings.llm.fields.temperature.label')}
            type="number"
            value={settings.temperature}
            onChange={handleChange}
            error={validationErrors.temperature}
            min={0}
            max={2}
            step={0.1}
          />

          <LlmFormField
            id="isOllama"
            name="isOllama"
            label={t('settings.llm.fields.is_ollama.label')}
            type="checkbox"
            value={settings.isOllama}
            onChange={handleChange}
          />

          <LlmFormField
            id="timeoutMinutes"
            name="timeoutMinutes"
            label={t('settings.llm.fields.timeout_minutes.label')}
            type="number"
            value={settings.timeoutMinutes}
            onChange={handleChange}
            error={validationErrors.timeoutMinutes}
            min={1}
            max={120}
          />

          <LlmFormField
            id="chatEndpoint"
            name="chatEndpoint"
            label={t('settings.llm.fields.chat_endpoint.label')}
            type="text"
            value={settings.chatEndpoint}
            onChange={handleChange}
            placeholder={t('settings.llm.fields.chat_endpoint.placeholder')}
          />

          <LlmFormField
            id="generateEndpoint"
            name="generateEndpoint"
            label={t('settings.llm.fields.generate_endpoint.label')}
            type="text"
            value={settings.generateEndpoint}
            onChange={handleChange}
            placeholder={t('settings.llm.fields.generate_endpoint.placeholder')}
          />

          {/* Submit Button */}
          <div className="flex justify-end">
            <button
              type="submit"
              disabled={saving}
              className="btn-primary inline-flex items-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {saving ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin" />
                  <span>{t('settings.llm.actions.saving')}</span>
                </>
              ) : (
                <>
                  <Save className="h-4 w-4" />
                  <span>{t('settings.llm.actions.save')}</span>
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
