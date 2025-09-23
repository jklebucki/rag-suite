import React from 'react'
import { User } from 'lucide-react'
import { useI18n } from '@/contexts/I18nContext'

export function UserSettings() {
  const { t } = useI18n()

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center space-x-3">
        <div className="p-2 bg-green-100 rounded-lg">
          <User className="h-6 w-6 text-green-600" />
        </div>
        <div>
          <h1 className="text-2xl font-bold text-gray-900">User Settings</h1>
          <p className="text-gray-600">Configure your personal settings</p>
        </div>
      </div>

      {/* Placeholder Content */}
      <div className="bg-white shadow rounded-lg p-6">
        <div className="text-center py-12">
          <User className="h-12 w-12 text-gray-400 mx-auto mb-4" />
          <h3 className="text-lg font-medium text-gray-900 mb-2">User Settings Coming Soon</h3>
          <p className="text-gray-500">
            This section is under development. Personal user settings will be available here.
          </p>
        </div>
      </div>
    </div>
  )
}
