import React, { ReactNode } from 'react'
import { useLayout } from '@/hooks/useLayout'
import { Sidebar } from './layout/Sidebar'
import { TopBar } from './layout/TopBar'

interface LayoutProps {
  children: ReactNode
}

export function Layout({ children }: LayoutProps) {
  const {
    isSidebarOpen,
    mainNavigation,
    footerNavigation,
    toggleSidebar,
    closeSidebar,
    isActiveRoute,
  } = useLayout()

  return (
    <div className="min-h-screen bg-gray-50">
      <Sidebar
        mainNavigation={mainNavigation}
        footerNavigation={footerNavigation}
        isOpen={isSidebarOpen}
        onClose={closeSidebar}
        isActiveRoute={isActiveRoute}
      />

      {/* Main content */}
      <div className="lg:pl-64">
        <TopBar onToggleSidebar={toggleSidebar} />

        {/* Page content */}
        <main className="p-6">
          {children}
        </main>
      </div>
    </div>
  )
}
