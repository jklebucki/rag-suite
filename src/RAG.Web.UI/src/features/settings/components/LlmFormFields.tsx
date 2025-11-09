// All code comments must be written in English, regardless of the conversation language.

import React from 'react'
import { RefreshCw } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'

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
      <div className="space-y-1">
        <label htmlFor={id} className="inline-flex items-center gap-2 text-sm font-medium text-gray-700 dark:text-gray-200">
          <input
            type="checkbox"
            id={id}
            name={name}
            checked={value as boolean}
            onChange={onChange}
            className="form-checkbox"
          />
          {label}
        </label>
        {description && <p className="text-sm text-gray-500 dark:text-gray-400">{description}</p>}
      </div>
    )
  }

  return (
    <div>
      <label htmlFor={id} className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">
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
        className={`form-input ${error ? 'form-input-error' : ''}`}
        placeholder={placeholder}
      />
      {description && <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">{description}</p>}
      {error && <p className="mt-1 text-sm text-red-600 dark:text-red-400">{error}</p>}
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
  const { t } = useI18n()

  return (
    <div>
      <label htmlFor="model" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">
        {t('settings.llm.fields.model.label')}
      </label>
      <div className="flex gap-2">
        <select
          id="model"
          name="model"
          value={value}
          onChange={onChange}
          className={`form-select flex-1 ${error ? 'form-input-error' : ''}`}
          disabled={disabled}
        >
          <option value="">{t('settings.llm.fields.model.placeholder')}</option>
          {availableModels.map(model => (
            <option key={model} value={model}>{model}</option>
          ))}
        </select>
        <button
          type="button"
          onClick={onRefresh}
          disabled={isLoading || disabled}
          className="btn-secondary px-3 py-2 flex items-center disabled:opacity-50 disabled:cursor-not-allowed"
          title={t('settings.llm.fields.model.refresh_title')}
          aria-label={t('settings.llm.fields.model.refresh_title')}
        >
          <RefreshCw className={`h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} />
        </button>
      </div>
      {error && <p className="mt-1 text-sm text-red-600 dark:text-red-400">{error}</p>}
      {availableModels.length === 0 && !isLoading && !disabled && (
        <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
          {t('settings.llm.fields.model.none')}
        </p>
      )}
    </div>
  )
}
