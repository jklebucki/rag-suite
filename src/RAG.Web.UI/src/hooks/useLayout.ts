import { useState } from 'react'
import { useLocation } from 'react-router-dom'
import {
  Search,
  MessageSquare,
  BarChart3,
  Settings,
} from 'lucide-react'

export function useLayout() {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false)
  const location = useLocation()

  const navigation = [
    { name: 'Dashboard', href: '/', icon: BarChart3 },
    { name: 'Chat', href: '/chat', icon: MessageSquare },
    { name: 'Search', href: '/search', icon: Search },
    { name: 'Settings', href: '/settings', icon: Settings },
  ]

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
