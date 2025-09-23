import { useState } from 'react'
import { useLocation } from 'react-router-dom'
import {
  Search,
  MessageSquare,
  BarChart3,
  Settings,
  Info,
} from 'lucide-react'
import { useI18n } from '@/contexts/I18nContext'
import { useAuth } from '@/contexts/AuthContext'

export function useLayout() {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false)
  const location = useLocation()
  const { t } = useI18n()
  const { user } = useAuth()

  const isAdmin = user?.roles?.includes('Admin') ?? false

  const baseNavigation = [
    { name: t('nav.dashboard'), href: '/', icon: BarChart3 },
    { name: t('nav.chat'), href: '/chat', icon: MessageSquare },
    { name: t('nav.search'), href: '/search', icon: Search },
  ]

  const adminNavigation = [
    { name: 'Settings', href: '/settings', icon: Settings },
  ]

  const footerNavigation = [
    { name: t('nav.app_info'), href: '/about', icon: Info },
  ]

  const mainNavigation = isAdmin ? [...baseNavigation, ...adminNavigation] : baseNavigation

  const navigation = [...mainNavigation, ...footerNavigation]

  const toggleSidebar = () => {
    setIsSidebarOpen(!isSidebarOpen)
  }

  const closeSidebar = () => {
    setIsSidebarOpen(false)
  }

  const isActiveRoute = (href: string) => {
    return location.pathname === href
  }

  return {
    // State
    isSidebarOpen,

    // Data
    navigation,
    mainNavigation,
    footerNavigation,
    currentPath: location.pathname,

    // Actions
    toggleSidebar,
    closeSidebar,
    isActiveRoute,
  }
}
