import React, { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { Menu, type LucideIcon } from 'lucide-react'

export interface PanelSidebarItem {
  to: string
  icon: LucideIcon
  label: string
}

interface PanelSidebarProps {
  title: string
  menuLabel: string
  items: PanelSidebarItem[]
  isActiveRoute: (href: string) => boolean
}

export function PanelSidebar({
  title,
  menuLabel,
  items,
  isActiveRoute,
}: PanelSidebarProps) {
  const [isMenuOpen, setIsMenuOpen] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)

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

  const itemBase =
    'w-full flex items-center space-x-3 px-3 py-2 rounded-xl text-left transition-colors border'
  const activeClasses =
    'bg-primary-50 text-primary-700 border-primary-200 dark:bg-primary-900/20 dark:text-primary-300 dark:border-primary-500/50'
  const inactiveClasses =
    'text-gray-700 dark:text-gray-200 border-transparent hover:bg-gray-100 dark:hover:bg-slate-800'

  const renderItem = (item: PanelSidebarItem, onClick?: () => void) => {
    const Icon = item.icon

    return (
      <Link
        key={item.to}
        to={item.to}
        onClick={onClick}
        className={`${itemBase} ${isActiveRoute(item.to) ? activeClasses : inactiveClasses}`}
        aria-current={isActiveRoute(item.to) ? 'true' : undefined}
      >
        <Icon className="h-5 w-5" />
        <span className="text-sm">{item.label}</span>
      </Link>
    )
  }

  return (
    <>
      <div className="md:hidden bg-white dark:bg-slate-900 border-b border-gray-200 dark:border-slate-800 transition-colors">
        <div className="flex items-center justify-between p-3">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
            {title}
          </h2>

          <div className="relative" ref={menuRef}>
            <button
              onClick={() => setIsMenuOpen(!isMenuOpen)}
              className="p-2 rounded-md hover:bg-gray-100 active:bg-gray-200 dark:hover:bg-slate-800 dark:active:bg-slate-700 transition-colors"
              aria-label={menuLabel}
              title={menuLabel}
            >
              <Menu className="h-5 w-5 text-gray-700 dark:text-gray-200" />
            </button>

            {isMenuOpen && (
              <div className="absolute right-0 top-full mt-1 w-64 bg-white dark:bg-slate-900 border border-gray-200 dark:border-slate-800 rounded-xl shadow-lg z-50">
                <div className="p-2 space-y-1">
                  {items.map((item) => renderItem(item, () => setIsMenuOpen(false)))}
                </div>
              </div>
            )}
          </div>
        </div>
      </div>

      <div className="hidden md:flex w-80 border-r border-gray-200 dark:border-slate-800 flex-col bg-white dark:bg-slate-950 transition-colors">
        <div className="p-4 border-b border-gray-200 dark:border-slate-800">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
            {title}
          </h2>
        </div>

        <nav className="flex-1 overflow-y-auto p-4 space-y-2 scrollbar-hide">
          {items.map((item) => renderItem(item))}
        </nav>
      </div>
    </>
  )
}
