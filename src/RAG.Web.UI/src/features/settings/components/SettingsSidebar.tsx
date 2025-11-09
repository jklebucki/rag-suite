// All code comments must be written in English, regardless of the conversation language.

import React, { useEffect, useRef, useState } from 'react'
import { Settings as SettingsIcon, User, Menu } from 'lucide-react'
import type { SettingsTab } from '@/features/settings/types/settings'

export function SettingsSidebar({
  activeTab,
  setActiveTab,
}: {
  activeTab: SettingsTab
  setActiveTab: (tab: SettingsTab) => void
}) {
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
      className={`w-full flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium text-left transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 focus-visible:ring-offset-white dark:focus-visible:ring-offset-gray-900 ${
        activeTab === tab
          ? 'bg-primary-50 text-primary-700 border border-primary-200 shadow-sm dark:bg-primary-900/30 dark:text-primary-300 dark:border-primary-700'
          : 'text-gray-700 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-slate-800'
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
      <div className="md:hidden surface border border-transparent rounded-xl shadow-sm mb-4">
        <div className="flex items-center justify-between">
          <div />
          <div className="relative" ref={menuRef}>
            <button
              ref={toggleRef}
              onClick={() => setIsMenuOpen(!isMenuOpen)}
              aria-expanded={isMenuOpen}
              aria-controls="settings-sidebar"
              className="p-2 rounded-md hover:bg-gray-100 dark:hover:bg-slate-800 active:bg-gray-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 focus-visible:ring-offset-white dark:focus-visible:ring-offset-gray-900 transition-colors"
              aria-label="Settings menu"
              title="Settings menu"
            >
              <Menu className="h-5 w-5 text-gray-700 dark:text-gray-200" />
            </button>

            {isMenuOpen && (
              <div className="absolute right-0 top-full mt-1 w-64 surface border border-gray-200 dark:border-slate-700 rounded-xl shadow-lg z-50">
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
      <aside id="settings-sidebar" className="hidden md:block w-64 surface p-6 sticky top-6 h-full self-start">
        <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">Settings</h2>
        <nav className="space-y-2" aria-label="Settings navigation">
          {navButton('llm', 'LLM Settings', <SettingsIcon className="h-5 w-5" />)}
          {navButton('user', 'User Settings', <User className="h-5 w-5" />)}
        </nav>
      </aside>

      {/* Desktop overlay is handled above; mobile uses dropdown, no full-screen drawer */}
    </>
  )
}
