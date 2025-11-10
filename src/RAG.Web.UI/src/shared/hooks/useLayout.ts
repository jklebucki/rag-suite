import { useState } from 'react'
import { useLocation } from 'react-router-dom'
import {
  Search,
  MessageSquare,
  BarChart3,
  Settings,
  Info,
  Users,
  BookOpen,
  Home,
  UsersRound,
} from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import { useAuth } from '@/shared/contexts/AuthContext'

export function useLayout() {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false)
  const location = useLocation()
  const { t } = useI18n()
  const { user, isAuthenticated } = useAuth()

  const isAdmin = user?.roles?.includes('Admin') ?? false

  const baseNavigation = [
    { name: t('nav.landing'), href: '/', icon: Home },
    { name: t('nav.dashboard'), href: '/dashboard', icon: BarChart3 },
    { name: t('nav.chat'), href: '/chat', icon: MessageSquare },
    { name: t('nav.search'), href: '/search', icon: Search },
    { name: t('nav.forum'), href: '/forum', icon: UsersRound },
  ]

  const addressBookNavigation = [
    { name: t('nav.addressBook'), href: '/address-book', icon: Users },
  ]

  const adminNavigation = [
    { name: t('nav.settings'), href: '/settings', icon: Settings },
  ]

  // Cyber Panel navigation entry - visible to any authenticated user and placed before admin items
  const cyberNavigation = isAuthenticated
    ? [{ name: t('nav.cyberpanel'), href: '/cyberpanel', icon: BarChart3 }]
    : []

  const footerNavigation = [
    { name: t('nav.user_guide'), href: '/guide', icon: BookOpen },
    { name: t('nav.app_info'), href: '/about', icon: Info },
  ]

  const mainNavigation = isAdmin
    ? [...baseNavigation, ...addressBookNavigation, ...cyberNavigation, ...adminNavigation]
    : [...baseNavigation, ...addressBookNavigation, ...cyberNavigation]

  const navigation = [...mainNavigation, ...footerNavigation]

  const toggleSidebar = () => {
    setIsSidebarOpen((prev) => !prev)
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
