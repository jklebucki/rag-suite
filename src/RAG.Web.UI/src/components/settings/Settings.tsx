import React, { useState } from 'react'
import { Settings as SettingsIcon, User } from 'lucide-react'
import { useI18n } from '@/contexts/I18nContext'
import { SettingsForm } from './SettingsForm'
import { UserSettings } from './UserSettings'

type Tab = 'llm' | 'user'

export function Settings() {
  const { t } = useI18n()
  const [activeTab, setActiveTab] = useState<Tab>('llm')

  return (
    <div className="flex h-full">
      {/* Sidebar */}
      <div className="w-64 bg-gray-50 border-r border-gray-200 p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Settings</h2>
        <nav className="space-y-2">
          <button
            onClick={() => setActiveTab('llm')}
            className={`w-full flex items-center space-x-3 px-3 py-2 rounded-md text-left transition-colors ${
              activeTab === 'llm'
                ? 'bg-blue-100 text-blue-700 border border-blue-200'
                : 'text-gray-700 hover:bg-gray-100'
            }`}
          >
            <SettingsIcon className="h-5 w-5" />
            <span>LLM Settings</span>
          </button>
          <button
            onClick={() => setActiveTab('user')}
            className={`w-full flex items-center space-x-3 px-3 py-2 rounded-md text-left transition-colors ${
              activeTab === 'user'
                ? 'bg-green-100 text-green-700 border border-green-200'
                : 'text-gray-700 hover:bg-gray-100'
            }`}
          >
            <User className="h-5 w-5" />
            <span>User Settings</span>
          </button>
        </nav>
      </div>

      {/* Main Content */}
      <div className="flex-1 p-6">
        {activeTab === 'llm' && <SettingsForm />}
        {activeTab === 'user' && <UserSettings />}
      </div>
    </div>
  )
}
