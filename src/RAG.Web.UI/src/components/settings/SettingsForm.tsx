import React, { useState, useEffect } from 'react'
import { Save, Loader2, RefreshCw, Settings as SettingsIcon, Shield } from 'lucide-react'
import { useToast } from '@/contexts/ToastContext'
import { useI18n } from '@/contexts/I18nContext'
import apiClient from '@/services/api'
import type { LlmSettings, LlmSettingsRequest, AvailableModelsResponse } from '@/types'

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
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({})

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

    // Clear validation error
    if (validationErrors[name]) {
      setValidationErrors(prev => ({
        ...prev,
        [name]: ''
      }))
    }
  }

  const validateForm = (): boolean => {
    const errors: Record<string, string> = {}

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

    setValidationErrors(errors)
    return Object.keys(errors).length === 0
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
      {/* URL */}
      <div>
        <label htmlFor="url" className="block text-sm font-medium text-gray-700 mb-1">
          LLM Service URL
        </label>
        <input
          type="url"
          id="url"
          name="url"
          value={settings.url}
          onChange={handleChange}
          className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
            validationErrors.url ? 'border-red-500' : 'border-gray-300'
          }`}
          placeholder="https://api.example.com"
        />
        {validationErrors.url && (
          <p className="mt-1 text-sm text-red-600">{validationErrors.url}</p>
        )}
      </div>

      {/* Model */}
      <div>
        <label htmlFor="model" className="block text-sm font-medium text-gray-700 mb-1">
          Model
        </label>
        <div className="flex space-x-2">
          <select
            id="model"
            name="model"
            value={settings.model}
            onChange={handleChange}
            className={`flex-1 px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
              validationErrors.model ? 'border-red-500' : 'border-gray-300'
            }`}
          >
            <option value="">Select a model...</option>
            {availableModels.map(model => (
              <option key={model} value={model}>{model}</option>
            ))}
          </select>
          <button
            type="button"
            onClick={handleRefreshModels}
            disabled={loadingModels || !settings.url.trim()}
            className="px-3 py-2 bg-gray-100 hover:bg-gray-200 disabled:bg-gray-50 disabled:cursor-not-allowed rounded-md flex items-center"
            title="Refresh available models"
          >
            <RefreshCw className={`h-4 w-4 ${loadingModels ? 'animate-spin' : ''}`} />
          </button>
        </div>
        {validationErrors.model && (
          <p className="mt-1 text-sm text-red-600">{validationErrors.model}</p>
        )}
        {availableModels.length === 0 && settings.url.trim() && !loadingModels && (
          <p className="mt-1 text-sm text-gray-500">No models available. Check the URL and try refreshing.</p>
        )}
      </div>

      {/* Max Tokens */}
      <div>
        <label htmlFor="maxTokens" className="block text-sm font-medium text-gray-700 mb-1">
          Max Tokens
        </label>
        <input
          type="number"
          id="maxTokens"
          name="maxTokens"
          value={settings.maxTokens}
          onChange={handleChange}
          min="1"
          max="100000"
          className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
            validationErrors.maxTokens ? 'border-red-500' : 'border-gray-300'
          }`}
        />
        {validationErrors.maxTokens && (
          <p className="mt-1 text-sm text-red-600">{validationErrors.maxTokens}</p>
        )}
      </div>

      {/* Temperature */}
      <div>
        <label htmlFor="temperature" className="block text-sm font-medium text-gray-700 mb-1">
          Temperature (0.0 - 2.0)
        </label>
        <input
          type="number"
          id="temperature"
          name="temperature"
          value={settings.temperature}
          onChange={handleChange}
          min="0"
          max="2"
          step="0.1"
          className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
            validationErrors.temperature ? 'border-red-500' : 'border-gray-300'
          }`}
        />
        {validationErrors.temperature && (
          <p className="mt-1 text-sm text-red-600">{validationErrors.temperature}</p>
        )}
      </div>

      {/* Is Ollama */}
      <div className="flex items-center">
        <input
          type="checkbox"
          id="isOllama"
          name="isOllama"
          checked={settings.isOllama}
          onChange={handleChange}
          className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
        />
        <label htmlFor="isOllama" className="ml-2 block text-sm text-gray-700">
          Is Ollama Service
        </label>
      </div>

      {/* Timeout Minutes */}
      <div>
        <label htmlFor="timeoutMinutes" className="block text-sm font-medium text-gray-700 mb-1">
          Timeout Minutes
        </label>
        <input
          type="number"
          id="timeoutMinutes"
          name="timeoutMinutes"
          value={settings.timeoutMinutes}
          onChange={handleChange}
          min="1"
          max="120"
          className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
            validationErrors.timeoutMinutes ? 'border-red-500' : 'border-gray-300'
          }`}
        />
        {validationErrors.timeoutMinutes && (
          <p className="mt-1 text-sm text-red-600">{validationErrors.timeoutMinutes}</p>
        )}
      </div>

      {/* Chat Endpoint */}
      <div>
        <label htmlFor="chatEndpoint" className="block text-sm font-medium text-gray-700 mb-1">
          Chat Endpoint
        </label>
        <input
          type="text"
          id="chatEndpoint"
          name="chatEndpoint"
          value={settings.chatEndpoint}
          onChange={handleChange}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
          placeholder="/api/chat"
        />
      </div>

      {/* Generate Endpoint */}
      <div>
        <label htmlFor="generateEndpoint" className="block text-sm font-medium text-gray-700 mb-1">
          Generate Endpoint
        </label>
        <input
          type="text"
          id="generateEndpoint"
          name="generateEndpoint"
          value={settings.generateEndpoint}
          onChange={handleChange}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
          placeholder="/api/generate"
        />
      </div>

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
