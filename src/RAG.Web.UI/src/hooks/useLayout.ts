import { useState } from 'react'
import { useLocation } from 'react-router-dom'
import {
  Search,
  MessageSquare,
  BarChart3,
  Settings,
  Info,
  Users,
} from 'lucide-react'
import { useI18n } from '@/contexts/I18nContext'
import { useAuth } from '@/contexts/AuthContext'

export function useLayout() {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false)
  const location = useLocation()
  const { t } = useI18n()
  const { user, isAuthenticated } = useAuth()

  const isAdmin = user?.roles?.includes('Admin') ?? false
  const isPowerUser = user?.roles?.includes('PowerUser') ?? false

  const baseNavigation = [
    { name: t('nav.dashboard'), href: '/', icon: BarChart3 },
    { name: t('nav.chat'), href: '/chat', icon: MessageSquare },
    { name: t('nav.search'), href: '/search', icon: Search },
  ]

  const addressBookNavigation = [
    { name: t('nav.addressBook'), href: '/address-book', icon: Users },
  ]

  const adminNavigation = [
    { name: 'Settings', href: '/settings', icon: Settings },
  ]

  // Cyber Panel navigation entry - visible to any authenticated user and placed before admin items
  const cyberNavigation = isAuthenticated
    ? [{ name: t('nav.cyberpanel'), href: '/cyberpanel', icon: BarChart3 }]
    : []

  const footerNavigation = [
    { name: t('nav.app_info'), href: '/about', icon: Info },
  ]

  const mainNavigation = isAdmin
    ? [...baseNavigation, ...addressBookNavigation, ...cyberNavigation, ...adminNavigation]
    : [...baseNavigation, ...addressBookNavigation, ...cyberNavigation]

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
