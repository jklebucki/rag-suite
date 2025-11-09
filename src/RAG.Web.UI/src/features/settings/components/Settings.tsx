// All code comments must be written in English, regardless of the conversation language.

import React, { useState } from 'react'
import { SettingsForm } from './SettingsForm'
import { UserSettings } from './UserSettings'
import { SettingsSidebar } from './SettingsSidebar'
import type { SettingsTab } from '@/features/settings/types/settings'

export function Settings() {
  const [activeTab, setActiveTab] = useState<SettingsTab>('llm')

  return (
    // On mobile we want the sidebar topbar above content (column). On md+ use row with sidebar left.
    <div className="flex flex-col md:flex-row gap-6 lg:gap-10 min-h-full">
      <SettingsSidebar activeTab={activeTab} setActiveTab={setActiveTab} />

      {/* Main Content */}
      <div className="flex-1 space-y-6 p-6 md:p-8">
        {activeTab === 'llm' && <SettingsForm />}
        {activeTab === 'user' && <UserSettings />}
      </div>
    </div>
  )
}
