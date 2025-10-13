// All code comments must be written in English, regardless of the conversation language.

import React, { useState, useEffect } from 'react'
import { Save, Loader2, Settings as SettingsIcon, Shield } from 'lucide-react'
import { useToast, useI18n } from '@/contexts'
import apiClient from '@/services/api'
import type { LlmSettings, LlmSettingsRequest, AvailableModelsResponse } from '@/types'
import { validateLlmSettings } from './llmValidation'
import { LlmFormField, ModelSelectField } from './LlmFormFields'
import type { LlmFormErrors } from '@/types'

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

  // Load settings on component mount
  useEffect(() => {
    loadSettings()
  }, [])

  // Load available models when URL changes
  useEffect(() => {
    if (settings.url.trim()) {
      loadAvailableModels()
    }
  }, [settings.url])

  const loadSettings = async () => {
    try {
      setLoading(true)
      const data = await apiClient.getLlmSettings()
      setSettings(data)
      onSettingsChange?.(data)
    } catch (error) {
      console.error('Failed to load LLM settings:', error)
      addToast({
        type: 'error',
        title: 'Error',
        message: 'Failed to load LLM settings'
      })
    } finally {
      setLoading(false)
    }
  }

  const loadAvailableModels = async () => {
    if (!settings.url.trim()) return

    try {
      setLoadingModels(true)
      const data: AvailableModelsResponse = await apiClient.getAvailableLlmModelsFromUrl(settings.url, settings.isOllama)
      setAvailableModels(data.models || [])
    } catch (error) {
      console.error('Failed to load available models:', error)
      setAvailableModels([])
      // Don't show error toast for model loading as it's expected to fail for invalid URLs
    } finally {
      setLoadingModels(false)
    }
  }

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

      await apiClient.updateLlmSettings(request)
      addToast({
        type: 'success',
        title: 'Success',
        message: 'LLM settings updated successfully'
      })
      onSettingsChange?.(settings)
    } catch (error) {
      console.error('Failed to update LLM settings:', error)
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
      <div className="flex items-center justify-center p-8">
        <Loader2 className="h-8 w-8 animate-spin text-blue-500" />
        <span className="ml-2 text-gray-600">Loading settings...</span>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center space-x-3">
        <div className="p-2 bg-blue-100 rounded-lg">
          <SettingsIcon className="h-6 w-6 text-blue-600" />
        </div>
        <div>
          <h1 className="text-2xl font-bold text-gray-900">LLM Settings</h1>
          <p className="text-gray-600">Configure your Large Language Model settings</p>
        </div>
      </div>

      {/* Admin Notice */}
      <div className="bg-amber-50 border border-amber-200 rounded-lg p-4">
        <div className="flex items-center space-x-2">
          <Shield className="h-5 w-5 text-amber-600" />
          <span className="text-sm font-medium text-amber-800">Admin Access Required</span>
        </div>
        <p className="mt-1 text-sm text-amber-700">
          These settings control the behavior of the LLM service. Changes may affect system performance and functionality.
        </p>
      </div>

      {/* Settings Form */}
      <div className="bg-white shadow rounded-lg p-6">
        <div className="mb-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-2">Model Configuration</h2>
          <p className="text-gray-600">
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
              className="inline-flex items-center px-4 py-2 bg-blue-600 hover:bg-blue-700 disabled:bg-blue-400 disabled:cursor-not-allowed text-white rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
            >
              {saving ? (
                <Loader2 className="h-4 w-4 animate-spin mr-2" />
              ) : (
                <Save className="h-4 w-4 mr-2" />
              )}
              {saving ? 'Saving...' : 'Save Settings'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
