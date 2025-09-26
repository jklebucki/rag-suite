import React from 'react'
import { Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '@/contexts/AuthContext'
import CyberPanelSidebar from '@/components/cyberpanel/CyberPanelSidebar'
import { useI18n } from '@/contexts/I18nContext'

export function CyberPanelLayout() {
  const { user } = useAuth()
  const roles = user?.roles || []
  const isAdmin = roles.includes('Admin')
  const isPowerUser = roles.includes('PowerUser')
  const location = useLocation()
  const { t } = useI18n()

  return (
    <div className="flex h-full">
      {/* Sidebar (match Settings layout) */}
      <div className="w-64 bg-gray-50 border-r border-gray-200 p-6">
        <div className="sticky top-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">{t('nav.cyberpanel')}</h2>
          <CyberPanelSidebar
            onClose={() => {}}
            isActiveRoute={(href) => location.pathname.startsWith(href)}
          />
        </div>
      </div>

      {/* Main Content (match Settings layout padding) */}
      <div className="flex-1 p-6">
        <Outlet />
      </div>
    </div>
  )
}
