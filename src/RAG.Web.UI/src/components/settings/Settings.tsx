import React, { useState } from 'react'
import { Settings as SettingsIcon, User } from 'lucide-react'
import { useI18n } from '@/contexts/I18nContext'
import { SettingsForm } from './SettingsForm'
import { UserSettings } from './UserSettings'
import { SettingsSidebar } from './SettingsSidebar'

type Tab = 'llm' | 'user'

export function Settings() {
  const { t } = useI18n()
  const [activeTab, setActiveTab] = useState<Tab>('llm')

  return (
    // On mobile we want the sidebar topbar above content (column). On md+ use row with sidebar left.
    <div className="flex flex-col md:flex-row h-full">
      <SettingsSidebar activeTab={activeTab} setActiveTab={setActiveTab} />

      {/* Main Content */}
      <div className="flex-1 p-6">
        {activeTab === 'llm' && <SettingsForm />}
        {activeTab === 'user' && <UserSettings />}
      </div>
    </div>
  )
}
