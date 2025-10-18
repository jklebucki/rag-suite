import React from 'react'
import { Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '@/contexts/AuthContext'
import CyberPanelSidebar from '@/components/cyberpanel/CyberPanelSidebar'
import { useI18n } from '@/contexts/I18nContext'

export default function CyberPanelLayout() {
  const { user } = useAuth()
  const roles = user?.roles || []
  const isAdmin = roles.includes('Admin')
  const isPowerUser = roles.includes('PowerUser')
  const location = useLocation()
  const { t } = useI18n()

  return (
    <div className="flex flex-col md:flex-row h-[calc(100vh-8rem)] max-w-7xl mx-auto bg-white rounded-lg shadow-sm border overflow-hidden">
      {/* Sidebar - responsive (mobile topbar + desktop sidebar) */}
      <CyberPanelSidebar
        isActiveRoute={(href) => location.pathname.startsWith(href)}
      />

      {/* Main Content */}
      <div className="flex-1 overflow-y-auto p-4 md:p-6">
        <Outlet />
      </div>
    </div>
  )
}
