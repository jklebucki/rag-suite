// All code comments must be written in English, regardless of the conversation language.

import React, { useEffect, useRef, useState } from 'react'
import { Settings as SettingsIcon, User, Menu } from 'lucide-react'
import { useI18n } from '@/contexts/I18nContext'
import type { SettingsTab } from '../../types/settings'

export function SettingsSidebar({
  activeTab,
  setActiveTab,
}: {
  activeTab: SettingsTab
  setActiveTab: (t: SettingsTab) => void
}) {
  const { t } = useI18n()
  const [isMenuOpen, setIsMenuOpen] = useState(false)
  const menuRef = useRef<HTMLDivElement | null>(null)
  const toggleRef = useRef<HTMLButtonElement | null>(null)

  // close menu on Escape and click outside
  useEffect(() => {
    function onKey(e: KeyboardEvent) {
      if (e.key === 'Escape') setIsMenuOpen(false)
    }
    function onClickOutside(e: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setIsMenuOpen(false)
      }
    }

    if (isMenuOpen) {
      document.addEventListener('keydown', onKey)
      document.addEventListener('mousedown', onClickOutside)
    }
    return () => {
      document.removeEventListener('keydown', onKey)
      document.removeEventListener('mousedown', onClickOutside)
    }
  }, [isMenuOpen])

  useEffect(() => {
    if (!isMenuOpen) toggleRef.current?.focus()
    else menuRef.current?.querySelector<HTMLElement>('button, [tabindex]')?.focus()
  }, [isMenuOpen])

  const navButton = (tab: SettingsTab, label: string, icon: React.ReactNode) => (
    <button
      onClick={() => {
        setActiveTab(tab)
        setIsMenuOpen(false)
      }}
      className={`w-full flex items-center space-x-3 px-3 py-2 rounded-md text-left transition-colors ${
        activeTab === tab
          ? 'bg-blue-100 text-blue-700 border border-blue-200'
          : 'text-gray-700 hover:bg-gray-100'
      }`}
    >
      {icon}
      <span>{label}</span>
    </button>
  )

  return (
    <>
      {/* Mobile toggle */}
      {/* Mobile topbar: hamburger menu */}
      <div className="md:hidden bg-white border-b border-gray-200">
        <div className="flex items-center justify-between p-3">
          <div />
          <div className="relative" ref={menuRef}>
            <button
              ref={toggleRef}
              onClick={() => setIsMenuOpen(!isMenuOpen)}
              aria-expanded={isMenuOpen}
              aria-controls="settings-sidebar"
              className="p-2 rounded-md hover:bg-gray-100 active:bg-gray-200 transition-colors"
              aria-label="Settings menu"
              title="Settings menu"
            >
              <Menu className="h-5 w-5 text-gray-700" />
            </button>

            {isMenuOpen && (
              <div className="absolute right-0 top-full mt-1 w-64 bg-white border border-gray-200 rounded-lg shadow-lg z-50">
                <div className="p-2">
                  <nav className="space-y-2">
                    {navButton('llm', 'LLM Settings', <SettingsIcon className="h-5 w-5" />)}
                    {navButton('user', 'User Settings', <User className="h-5 w-5" />)}
                  </nav>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Desktop sidebar */}
      <aside id="settings-sidebar" className="hidden md:block w-64 bg-gray-50 border-r border-gray-200 p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Settings</h2>
        <nav className="space-y-2" aria-label="Settings navigation">
          {navButton('llm', 'LLM Settings', <SettingsIcon className="h-5 w-5" />)}
          {navButton('user', 'User Settings', <User className="h-5 w-5" />)}
        </nav>
      </aside>

      {/* Desktop overlay is handled above; mobile uses dropdown, no full-screen drawer */}
    </>
  )
}
