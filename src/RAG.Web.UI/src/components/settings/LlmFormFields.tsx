// All code comments must be written in English, regardless of the conversation language.

import React from 'react'
import { RefreshCw } from 'lucide-react'

interface LlmFormFieldProps {
  id: string
  name: string
  label: string
  type?: 'text' | 'number' | 'url' | 'checkbox'
  value: string | number | boolean
  onChange: (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => void
  error?: string
  placeholder?: string
  min?: number
  max?: number
  step?: number
  description?: string
}

export function LlmFormField({
  id,
  name,
  label,
  type = 'text',
  value,
  onChange,
  error,
  placeholder,
  min,
  max,
  step,
  description
}: LlmFormFieldProps) {
  if (type === 'checkbox') {
    return (
      <div className="flex items-center">
        <input
          type="checkbox"
          id={id}
          name={name}
          checked={value as boolean}
          onChange={onChange}
          className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
        />
        <label htmlFor={id} className="ml-2 block text-sm text-gray-700">
          {label}
        </label>
        {description && <p className="ml-2 text-sm text-gray-500">{description}</p>}
      </div>
    )
  }

  return (
    <div>
      <label htmlFor={id} className="block text-sm font-medium text-gray-700 mb-1">
        {label}
      </label>
      <input
        type={type}
        id={id}
        name={name}
        value={value as string | number}
        onChange={onChange}
        min={min}
        max={max}
        step={step}
        className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
          error ? 'border-red-500' : 'border-gray-300'
        }`}
        placeholder={placeholder}
      />
      {description && <p className="mt-1 text-sm text-gray-500">{description}</p>}
      {error && <p className="mt-1 text-sm text-red-600">{error}</p>}
    </div>
  )
}

interface ModelSelectFieldProps {
  value: string
  onChange: (e: React.ChangeEvent<HTMLSelectElement>) => void
  availableModels: string[]
  onRefresh: () => void
  isLoading: boolean
  disabled: boolean
  error?: string
}

export function ModelSelectField({
  value,
  onChange,
  availableModels,
  onRefresh,
  isLoading,
  disabled,
  error
}: ModelSelectFieldProps) {
  return (
    <div>
      <label htmlFor="model" className="block text-sm font-medium text-gray-700 mb-1">
        Model
      </label>
      <div className="flex space-x-2">
        <select
          id="model"
          name="model"
          value={value}
          onChange={onChange}
          className={`flex-1 px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${
            error ? 'border-red-500' : 'border-gray-300'
          }`}
        >
          <option value="">Select a model...</option>
          {availableModels.map(model => (
            <option key={model} value={model}>{model}</option>
          ))}
        </select>
        <button
          type="button"
          onClick={onRefresh}
          disabled={isLoading || disabled}
          className="px-3 py-2 bg-gray-100 hover:bg-gray-200 disabled:bg-gray-50 disabled:cursor-not-allowed rounded-md flex items-center"
          title="Refresh available models"
        >
          <RefreshCw className={`h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} />
        </button>
      </div>
      {error && <p className="mt-1 text-sm text-red-600">{error}</p>}
      {availableModels.length === 0 && !isLoading && !disabled && (
        <p className="mt-1 text-sm text-gray-500">No models available. Check the URL and try refreshing.</p>
      )}
    </div>
  )
}
