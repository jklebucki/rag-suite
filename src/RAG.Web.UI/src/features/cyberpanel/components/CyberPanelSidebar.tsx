import React from 'react'
import { BookOpen, List, Settings } from 'lucide-react'
import { PanelSidebar, type PanelSidebarItem } from '@/shared/components/layout'
import { useAuth } from '@/shared/contexts/AuthContext'
import { useI18n } from '@/shared/contexts/I18nContext'

interface Props {
  isActiveRoute: (href: string) => boolean
}

type CyberPanelNavigationItem = PanelSidebarItem & {
  visible: boolean
}

export function CyberPanelSidebar({ isActiveRoute }: Props) {
  const { user } = useAuth()
  const roles = user?.roles || []
  const isAdmin = roles.includes('Admin')
  const isPowerUser = roles.includes('PowerUser')
  const isUser = roles.includes('User')
  const { t } = useI18n()

  // Cyber panel navigation: routes, labels, icons, and role visibility stay local to this feature.
  const navigationItems: PanelSidebarItem[] = ([
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
  ] satisfies CyberPanelNavigationItem[])
    .filter(item => item.visible)
    .map(({ visible, ...item }) => item)

  return (
    <PanelSidebar
      title={t('nav.cyberpanel')}
      menuLabel="CyberPanel menu"
      items={navigationItems}
      isActiveRoute={isActiveRoute}
    />
  )
}
