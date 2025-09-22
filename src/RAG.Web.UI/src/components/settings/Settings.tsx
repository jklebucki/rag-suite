import React from 'react'
import { Settings as SettingsIcon, Shield } from 'lucide-react'
import { useI18n } from '@/contexts/I18nContext'
import { SettingsForm } from './SettingsForm'

export function Settings() {
  const { t } = useI18n()

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

        <SettingsForm />
      </div>
    </div>
  )
}