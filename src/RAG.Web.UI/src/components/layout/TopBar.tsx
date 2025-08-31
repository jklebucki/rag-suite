import React from 'react'
import { Menu, User } from 'lucide-react'
import { LanguageSelector } from '@/components/ui/LanguageSelector'
import { useI18n } from '@/contexts/I18nContext'

interface TopBarProps {
  onToggleSidebar: () => void
}

export function TopBar({ onToggleSidebar }: TopBarProps) {
  const { t } = useI18n()
  
  return (
    <header className="h-16 bg-white border-b border-gray-200 flex items-center justify-between px-6">
      <button
        onClick={onToggleSidebar}
        className="p-2 rounded-lg hover:bg-gray-100 lg:hidden"
        aria-label={t('common.toggle_menu')}
        title={t('common.toggle_menu')}
      >
        <Menu className="h-5 w-5" />
      </button>

      <div className="flex items-center gap-3 ml-auto">
        <LanguageSelector />
        
        <button
          className="p-2 rounded-lg hover:bg-gray-100"
          title={t('common.user_menu')}
          aria-label={t('common.user_menu')}
        >
          <User className="h-5 w-5" />
        </button>
      </div>
    </header>
  )
}
