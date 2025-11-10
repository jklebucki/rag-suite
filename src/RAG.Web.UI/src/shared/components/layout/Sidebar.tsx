import React from 'react'
import { Link } from 'react-router-dom'
import { X } from 'lucide-react'
import { cn } from '@/utils/cn'
import { useI18n } from '@/shared/contexts/I18nContext'

interface NavigationItem {
  name: string
  href: string
  icon: React.ComponentType<{ className?: string }>
  badgeCount?: number
}

interface SidebarProps {
  mainNavigation: NavigationItem[]
  footerNavigation: NavigationItem[]
  isOpen: boolean
  onClose: () => void
  isActiveRoute: (href: string) => boolean
}

export function Sidebar({ mainNavigation, footerNavigation, isOpen, onClose, isActiveRoute }: SidebarProps) {
  const { t } = useI18n()
  
  return (
    <>
      {/* Mobile overlay */}
      {isOpen && (
        <button
          className="fixed inset-0 z-40 bg-black bg-opacity-50 dark:bg-opacity-70 lg:hidden"
          onClick={onClose}
          aria-label="Close sidebar"
          tabIndex={-1}
        />
      )}

      {/* Sidebar */}
      <div className={cn(
        "fixed inset-y-0 left-0 z-50 w-64 bg-white dark:bg-gray-800 shadow-lg transform transition-transform duration-300 ease-in-out",
        isOpen ? "translate-x-0" : "-translate-x-full lg:translate-x-0"
      )}>
        {/* Header */}
        <div className="flex h-16 items-center justify-between px-6 border-b border-gray-200 dark:border-gray-700">
          <h1 className="text-xl font-bold text-gray-900 dark:text-gray-100">{t('app.title')}</h1>
          <button
            onClick={onClose}
            className="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 lg:hidden"
            aria-label="Close sidebar"
          >
            <X className="h-5 w-5 text-gray-700 dark:text-gray-300" />
          </button>
        </div>

        {/* Navigation */}
        <nav className="mt-8 px-4 flex-1">
          <ul className="space-y-2">
            {mainNavigation.map((item) => {
              const isActive = isActiveRoute(item.href)
              return (
                <li key={item.name}>
                  <Link
                    to={item.href}
                    onClick={onClose} // Close sidebar on mobile when navigating
                    className={cn(
                      'flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors',
                      isActive
                        ? 'bg-primary-50 text-primary-700 border-r-2 border-primary-500 dark:bg-primary-900/30 dark:text-primary-400 dark:border-primary-500'
                        : 'text-gray-700 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-700'
                    )}
                  >
                    <item.icon className="h-5 w-5 flex-shrink-0" />
                    <span className="flex-1 truncate">{item.name}</span>
                    {typeof item.badgeCount === 'number' && item.badgeCount > 0 && (
                      <span className="ml-auto rounded-full bg-primary-500 px-2 py-0.5 text-xs font-semibold text-white">
                        {item.badgeCount > 9 ? '9+' : item.badgeCount}
                      </span>
                    )}
                  </Link>
                </li>
              )
            })}

            {/* Cyber Panel is now part of mainNavigation from useLayout; inner CyberPanelSidebar is implemented as nested routes/layout */}
          </ul>

          {/* Footer Navigation */}
          <div className="mt-8 pt-4 border-t border-gray-200 dark:border-gray-700">
            <ul className="space-y-2">
              {footerNavigation.map((item) => {
                const isActive = isActiveRoute(item.href)
                return (
                  <li key={item.name}>
                    <Link
                      to={item.href}
                      onClick={onClose} // Close sidebar on mobile when navigating
                      className={cn(
                        'flex items-center px-3 py-2 rounded-lg text-sm font-medium transition-colors',
                        isActive
                          ? 'bg-primary-50 text-primary-700 border-r-2 border-primary-500 dark:bg-primary-900/30 dark:text-primary-400 dark:border-primary-500'
                          : 'text-gray-700 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-700'
                      )}
                    >
                      <item.icon className="mr-3 h-5 w-5" />
                      {item.name}
                    </Link>
                  </li>
                )
              })}
            </ul>
          </div>
        </nav>
      </div>
    </>
  )
}
