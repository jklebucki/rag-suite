import React, { useState, useRef, useEffect } from 'react'
import { Menu, User, LogOut, ChevronDown, Settings, Globe, LogIn } from 'lucide-react'
import { Link } from 'react-router-dom'
import { LanguageSelector } from '@/shared/components/ui/LanguageSelector'
import { ThemeToggle } from '@/shared/components/ui/ThemeToggle'
import { UserAccountModal } from '@/features/settings/components/UserAccountModal'
import { SessionExpiredModal } from '@/shared/components/ui/SessionExpiredModal'
import { useI18n } from '@/shared/contexts/I18nContext'
import { useAuth } from '@/shared/contexts/AuthContext'
import { logger } from '@/utils/logger'

interface TopBarProps {
  onToggleSidebar: () => void
}

export function TopBar({ onToggleSidebar }: TopBarProps) {
  const { t, language } = useI18n()
  const { user, logout, refreshError, logoutAllDevices, clearRefreshError } = useAuth()
  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false)
  const [isAccountModalOpen, setIsAccountModalOpen] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setIsUserMenuOpen(false)
      }
    }

    if (isUserMenuOpen) {
      document.addEventListener('mousedown', handleClickOutside)
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [isUserMenuOpen])
  
  const handleLogout = async () => {
    try {
      await logout()
      setIsUserMenuOpen(false)
    } catch (error) {
      logger.error('Logout error:', error)
    }
  }

  const handleAccountClick = () => {
    setIsAccountModalOpen(true)
    setIsUserMenuOpen(false)
  }

  const handleTryAgain = () => {
    // Clear the refresh error and try to refresh auth
    clearRefreshError()
    // The auth context will automatically try to refresh tokens
  }

  return (
    <header className="sticky top-0 z-30 h-16 w-full border-b border-gray-200 dark:border-gray-700 bg-white/95 dark:bg-gray-900/95 flex items-center justify-between px-6 shadow-sm backdrop-blur supports-[backdrop-filter]:bg-white/80 supports-[backdrop-filter]:dark:bg-gray-900/80">
      <div className="flex items-center gap-4">
        {/* Hamburger first so it is not overlapped by the logo on small screens */}
        <button
          onClick={onToggleSidebar}
          className="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 lg:hidden"
          aria-label={t('common.toggle_menu')}
          title={t('common.toggle_menu')}
        >
          <Menu className="h-5 w-5 text-gray-700 dark:text-gray-300" />
        </button>

        {/* Mobile: show citronex svg instead of the full logo to avoid overlap with hamburger */}
        <img
          src="/icon-citronex.svg"
          alt="Citronex"
          className="block md:hidden h-full max-h-10 w-auto object-contain"
        />

        {/* Desktop: keep existing full logo and hide on small screens */}
        <img src="/logo-citronex.png" alt="Citronex Logo" className="hidden md:block h-8 w-auto" />
      </div>

      <div className="flex items-center gap-3">
        {/* Theme toggle */}
        <ThemeToggle />

        {/* Language selector: render always so dropdown can be opened from mobile globe button; the selector's own toggle is hidden on mobile */}
        <LanguageSelector />

        <button
          onClick={() => {
            // On mobile we open the same language selector UI by focusing/clicking it.
            // Simpler approach: when mobile button clicked, toggle the desktop selector by dispatching a click
            // to the LanguageSelector button if present. This keeps behaviour consistent without duplicating logic.
            const el = document.querySelector('[data-language-selector-toggle]') as HTMLElement | null
            if (el) el.click()
          }}
          className="flex items-center gap-2 p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 md:hidden"
          title={t('common.toggle_menu')}
          aria-label={t('common.toggle_menu')}
        >
          <Globe className="h-5 w-5 text-gray-700 dark:text-gray-300" />
          <span className="text-sm font-medium text-gray-700 dark:text-gray-300 uppercase">{language}</span>
        </button>
        
        {!user && (
          <Link
            to="/login"
            className="flex items-center gap-2 p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700"
            title={t('auth.login.sign_in')}
            aria-label={t('auth.login.sign_in')}
          >
            <LogIn className="h-5 w-5 text-gray-700 dark:text-gray-300" />
            <span className="text-sm font-medium text-gray-700 dark:text-gray-300 hidden sm:inline">{t('auth.login.sign_in')}</span>
          </Link>
        )}
        
        {user && (
          <div className="relative" ref={menuRef}>
            <button
              onClick={() => setIsUserMenuOpen(!isUserMenuOpen)}
              className="flex items-center gap-2 p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700"
              title={t('common.user_menu')}
              aria-label={t('common.user_menu')}
            >
              <User className="h-5 w-5 text-gray-700 dark:text-gray-300" />
              <span className="text-sm font-medium text-gray-700 dark:text-gray-300 hidden sm:inline">
                {user.fullName || user.email}
              </span>
              <ChevronDown className={`h-4 w-4 text-gray-700 dark:text-gray-300 transition-transform ${isUserMenuOpen ? 'rotate-180' : ''}`} />
            </button>

            {isUserMenuOpen && (
              <div className="absolute right-0 mt-1 w-64 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg z-50">
                <div className="px-4 py-3 border-b border-gray-100 dark:border-gray-700">
                  <p className="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">{user.userName}</p>
                  <p className="text-xs text-gray-500 dark:text-gray-400 truncate">{user.email}</p>
                  {user.roles && user.roles.length > 0 && (
                    <div className="flex flex-wrap gap-1 mt-1">
                      {user.roles.map((role) => (
                        <span
                          key={role}
                          className="px-1.5 py-0.5 text-xs font-medium bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300 rounded"
                        >
                          {role}
                        </span>
                      ))}
                    </div>
                  )}
                </div>
                
                <div className="py-1">
                  <button
                    onClick={handleAccountClick}
                    className="w-full flex items-center gap-3 px-4 py-2 text-sm text-left text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700"
                  >
                    <Settings className="h-4 w-4" />
                    {t('account.manage_account')}
                  </button>
                  
                  <button
                    onClick={handleLogout}
                    className="w-full flex items-center gap-3 px-4 py-2 text-sm text-left text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700"
                  >
                    <LogOut className="h-4 w-4" />
                    {t('auth.logout')}
                  </button>
                </div>
              </div>
            )}
          </div>
        )}
      </div>

      {/* User Account Modal */}
      <UserAccountModal 
        isOpen={isAccountModalOpen} 
        onClose={() => setIsAccountModalOpen(false)} 
      />

      {/* Session Expired Modal */}
      <SessionExpiredModal
        isOpen={refreshError}
        onClose={clearRefreshError}
        onTryAgain={handleTryAgain}
        onLogout={logoutAllDevices}
      />
    </header>
  )
}
