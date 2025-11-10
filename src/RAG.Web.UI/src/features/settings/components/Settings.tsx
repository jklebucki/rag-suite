// All code comments must be written in English, regardless of the conversation language.

import React, { useEffect, useMemo, useState } from 'react'
import { SettingsForm } from './SettingsForm'
import { UserSettings } from './UserSettings'
import { SettingsSidebar } from './SettingsSidebar'
import { ForumSettingsPanel } from './ForumSettingsPanel'
import type { SettingsTab } from '@/features/settings/types/settings'
import { FeedbackAdminPanel } from './FeedbackAdminPanel'
import { useAuth } from '@/shared/contexts/AuthContext'

export function Settings() {
  const [activeTab, setActiveTab] = useState<SettingsTab>('llm')
  const { user } = useAuth()

  const roles = user?.roles ?? []
  const canManageUsers = roles.includes('Admin')
  const canManageFeedback = roles.includes('Admin') || roles.includes('PowerUser')
  const canManageForum = roles.includes('Admin')

  const availableTabs = useMemo<SettingsTab[]>(() => {
    const tabs: SettingsTab[] = ['llm']
    if (canManageUsers) tabs.push('user')
    if (canManageFeedback) tabs.push('feedback')
    if (canManageForum) tabs.push('forum')
    return tabs
  }, [canManageFeedback, canManageForum, canManageUsers])

  useEffect(() => {
    if (activeTab === 'user' && !canManageUsers) {
      setActiveTab(canManageFeedback ? 'feedback' : 'llm')
    } else if (activeTab === 'feedback' && !canManageFeedback) {
      setActiveTab(canManageUsers ? 'user' : 'llm')
    } else if (activeTab === 'forum' && !canManageForum) {
      setActiveTab(canManageUsers ? 'user' : canManageFeedback ? 'feedback' : 'llm')
    } else if (!availableTabs.includes(activeTab)) {
      setActiveTab('llm')
    }
  }, [activeTab, availableTabs, canManageFeedback, canManageForum, canManageUsers])

  return (
    // On mobile we want the sidebar topbar above content (column). On md+ use row with sidebar left.
    <div className="flex flex-col md:flex-row gap-6 lg:gap-10 min-h-full">
      <SettingsSidebar
        activeTab={activeTab}
        setActiveTab={setActiveTab}
        canManageUsers={canManageUsers}
        canManageFeedback={canManageFeedback}
        canManageForum={canManageForum}
      />

      {/* Main Content */}
      <div className="flex-1 space-y-6 p-6 md:p-8">
        {activeTab === 'llm' && <SettingsForm />}
        {activeTab === 'user' && <UserSettings />}
        {activeTab === 'feedback' && canManageFeedback && <FeedbackAdminPanel />}
        {activeTab === 'forum' && canManageForum && <ForumSettingsPanel />}
      </div>
    </div>
  )
}
