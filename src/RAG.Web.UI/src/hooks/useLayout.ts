import { useState } from 'react'
import { useLocation } from 'react-router-dom'
import {
  Search,
  MessageSquare,
  BarChart3,
  Settings,
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

  const navigation = isAdmin ? [...baseNavigation, ...adminNavigation] : baseNavigation

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
    currentPath: location.pathname,

    // Actions
    toggleSidebar,
    closeSidebar,
    isActiveRoute,
  }
}
