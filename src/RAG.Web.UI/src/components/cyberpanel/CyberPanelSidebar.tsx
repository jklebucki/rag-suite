import React from 'react'
import { Link } from 'react-router-dom'
import { BookOpen, Hammer, List } from 'lucide-react'
import { cn } from '@/utils/cn'
import { useAuth } from '@/contexts/AuthContext'
import { useI18n } from '@/contexts/I18nContext'

interface Props {
  onClose: () => void
  isActiveRoute: (href: string) => boolean
}

export default function CyberPanelSidebar({ onClose, isActiveRoute }: Props) {
  const { user } = useAuth()
  const roles = user?.roles || []
  const isAdmin = roles.includes('Admin')
  const isPowerUser = roles.includes('PowerUser')
  const { t } = useI18n()

  const itemBase = 'w-full flex items-center space-x-3 px-3 py-2 rounded-md text-left transition-colors'

  const activeClasses = 'bg-blue-100 text-blue-700 border border-blue-200'
  const inactiveClasses = 'text-gray-700 hover:bg-gray-100'

  return (
    <nav className="space-y-2">
      <Link
        to="/cyberpanel/quizzes"
        onClick={onClose}
        className={`${itemBase} ${isActiveRoute('/cyberpanel/quizzes') ? activeClasses : inactiveClasses}`}
        aria-current={isActiveRoute('/cyberpanel/quizzes') ? 'true' : undefined}
      >
        <List className="h-5 w-5" />
        <span className="text-sm">{t('cyberpanel.quizzes')}</span>
      </Link>

      {isAdmin && (
        <Link
          to="/cyberpanel/builder"
          onClick={onClose}
          className={`${itemBase} ${isActiveRoute('/cyberpanel/builder') ? activeClasses : inactiveClasses}`}
          aria-current={isActiveRoute('/cyberpanel/builder') ? 'true' : undefined}
        >
          <Hammer className="h-5 w-5" />
          <span className="text-sm">{t('cyberpanel.builder')}</span>
        </Link>
      )}

      {(isAdmin || isPowerUser) && (
        <Link
          to="/cyberpanel/results"
          onClick={onClose}
          className={`${itemBase} ${isActiveRoute('/cyberpanel/results') ? activeClasses : inactiveClasses}`}
          aria-current={isActiveRoute('/cyberpanel/results') ? 'true' : undefined}
        >
          <BookOpen className="h-5 w-5" />
          <span className="text-sm">{t('cyberpanel.results')}</span>
        </Link>
      )}
    </nav>
  )
}
