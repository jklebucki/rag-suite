import React, { useState, useRef, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { BookOpen, List, Menu, Settings } from 'lucide-react'
import { useAuth } from '@/shared/contexts/AuthContext'
import { useI18n } from '@/shared/contexts/I18nContext'

interface Props {
  isActiveRoute: (href: string) => boolean
}

export function CyberPanelSidebar({ isActiveRoute }: Props) {
  const { user } = useAuth()
  const roles = user?.roles || []
  const isAdmin = roles.includes('Admin')
  const isPowerUser = roles.includes('PowerUser')
  const isUser = roles.includes('User')
  const { t } = useI18n()
  const [isMenuOpen, setIsMenuOpen] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)

  // Close menu when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setIsMenuOpen(false)
      }
    }

    if (isMenuOpen) {
      document.addEventListener('mousedown', handleClickOutside)
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [isMenuOpen])

  const itemBase = 'w-full flex items-center space-x-3 px-3 py-2 rounded-xl text-left transition-colors border'
  const activeClasses = 'bg-primary-50 text-primary-700 border-primary-200 dark:bg-primary-900/20 dark:text-primary-300 dark:border-primary-500/50'
  const inactiveClasses = 'text-gray-700 dark:text-gray-200 border-transparent hover:bg-gray-100 dark:hover:bg-slate-800'

  const navigationItems = [
    {
      to: '/cyberpanel/quizzes',
      icon: List,
      label: t('cyberpanel.quizzes'),
      visible: true,
    },
    {
      to: '/cyberpanel/manager',
      icon: Settings,
      label: 'Quiz Manager',
      visible: isAdmin || isPowerUser,
    },
    {
      to: '/cyberpanel/results',
      icon: BookOpen,
      label: t('cyberpanel.results'),
      visible: isAdmin || isPowerUser || isUser,
    },
  ].filter(item => item.visible)

  return (
    <>
      {/* Mobile topbar: hamburger menu */}
      <div className="md:hidden bg-white dark:bg-slate-900 border-b border-gray-200 dark:border-slate-800 transition-colors">
        <div className="flex items-center justify-between p-3">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">{t('nav.cyberpanel')}</h2>

          <div className="relative" ref={menuRef}>
            <button
              onClick={() => setIsMenuOpen(!isMenuOpen)}
              className="p-2 rounded-md hover:bg-gray-100 active:bg-gray-200 dark:hover:bg-slate-800 dark:active:bg-slate-700 transition-colors"
              aria-label="CyberPanel menu"
              title="CyberPanel menu"
            >
              <Menu className="h-5 w-5 text-gray-700 dark:text-gray-200" />
            </button>

            {isMenuOpen && (
              <div className="absolute right-0 top-full mt-1 w-64 bg-white dark:bg-slate-900 border border-gray-200 dark:border-slate-800 rounded-xl shadow-lg z-50">
                <div className="p-2 space-y-1">
                  {navigationItems.map((item) => {
                    const Icon = item.icon
                    return (
                      <Link
                        key={item.to}
                        to={item.to}
                        onClick={() => setIsMenuOpen(false)}
                        className={`${itemBase} ${isActiveRoute(item.to) ? activeClasses : inactiveClasses}`}
                        aria-current={isActiveRoute(item.to) ? 'true' : undefined}
                      >
                        <Icon className="h-5 w-5" />
                        <span className="text-sm">{item.label}</span>
                      </Link>
                    )
                  })}
                </div>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Desktop / tablet vertical sidebar */}
      <div className="hidden md:flex w-80 border-r border-gray-200 dark:border-slate-800 flex-col bg-white dark:bg-slate-950 transition-colors">
        <div className="p-4 border-b border-gray-200 dark:border-slate-800">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">{t('nav.cyberpanel')}</h2>
        </div>

        <nav className="flex-1 overflow-y-auto p-4 space-y-2 scrollbar-hide">
          {navigationItems.map((item) => {
            const Icon = item.icon
            return (
              <Link
                key={item.to}
                to={item.to}
                className={`${itemBase} ${isActiveRoute(item.to) ? activeClasses : inactiveClasses}`}
                aria-current={isActiveRoute(item.to) ? 'true' : undefined}
              >
                <Icon className="h-5 w-5" />
                <span className="text-sm">{item.label}</span>
              </Link>
            )
          })}
        </nav>
      </div>
    </>
  )
}
