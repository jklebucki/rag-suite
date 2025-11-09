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

interface SettingsFormProps {
  onSettingsChange?: (settings: LlmSettings) => void
}

export function SettingsForm({ onSettingsChange }: SettingsFormProps) {
  const { addToast } = useToast()

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
        title: 'Error',
        message: 'Failed to load LLM settings'
      })
    } finally {
      setLoading(false)
    }
  }, [addToast, onSettingsChange])

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
    setValidationErrors(errors)
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
        title: 'Success',
        message: 'LLM settings updated successfully'
      })
      onSettingsChange?.(settings)
    } catch (error) {
      logger.error('Failed to update LLM settings:', error)
      addToast({
        type: 'error',
        title: 'Error',
        message: 'Failed to update LLM settings'
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
        <span className="text-gray-600 dark:text-gray-300">Loading settings...</span>
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
          <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">LLM Settings</h1>
          <p className="text-gray-600 dark:text-gray-300">Configure your Large Language Model settings</p>
        </div>
      </div>

      {/* Admin Notice */}
      <div className="surface-muted border border-amber-200 dark:border-amber-800/40 rounded-xl p-4">
        <div className="flex items-center gap-2">
          <Shield className="h-5 w-5 text-amber-600 dark:text-amber-400" />
          <span className="text-sm font-medium text-amber-700 dark:text-amber-300">Admin Access Required</span>
        </div>
        <p className="mt-1 text-sm text-amber-700 dark:text-amber-200">
          These settings control the behavior of the LLM service. Changes may affect system performance and functionality.
        </p>
      </div>

      {/* Settings Form */}
      <div className="surface p-6">
        <div className="mb-6">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-2">Model Configuration</h2>
          <p className="text-gray-600 dark:text-gray-300">
            Configure the connection to your LLM service and set generation parameters.
          </p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          <LlmFormField
            id="url"
            name="url"
            label="LLM Service URL"
            type="url"
            value={settings.url}
            onChange={handleChange}
            error={validationErrors.url}
            placeholder="https://api.example.com"
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
            label="Max Tokens"
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
            label="Temperature (0.0 - 2.0)"
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
            label="Is Ollama Service"
            type="checkbox"
            value={settings.isOllama}
            onChange={handleChange}
          />

          <LlmFormField
            id="timeoutMinutes"
            name="timeoutMinutes"
            label="Timeout Minutes"
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
            label="Chat Endpoint"
            type="text"
            value={settings.chatEndpoint}
            onChange={handleChange}
            placeholder="/api/chat"
          />

          <LlmFormField
            id="generateEndpoint"
            name="generateEndpoint"
            label="Generate Endpoint"
            type="text"
            value={settings.generateEndpoint}
            onChange={handleChange}
            placeholder="/api/generate"
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
                  <span>Saving...</span>
                </>
              ) : (
                <>
                  <Save className="h-4 w-4" />
                  <span>Save Settings</span>
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
