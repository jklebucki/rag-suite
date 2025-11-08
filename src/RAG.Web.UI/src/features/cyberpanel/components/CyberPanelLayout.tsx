import React from 'react'
import { Outlet, useLocation } from 'react-router-dom'
import { CyberPanelSidebar } from '@/features/cyberpanel/components/CyberPanelSidebar'

export function CyberPanelLayout() {
  const location = useLocation()

  return (
    <div className="flex flex-col md:flex-row h-[calc(100vh-8rem)] w-[95%] mx-auto bg-white rounded-lg shadow-sm border overflow-hidden">
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
