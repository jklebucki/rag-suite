import React, { ReactNode, useMemo } from 'react'
import { useLayout } from '@/shared/hooks/useLayout'
import { Sidebar } from './Sidebar'
import { TopBar } from './TopBar'
import { useAuth } from '@/shared/contexts/AuthContext'
import { useThreadBadges } from '@/features/forum/hooks/useForumQueries'

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
  const { isAuthenticated } = useAuth()
  const { data: badgesData } = useThreadBadges(isAuthenticated)

  const forumBadgeCount = badgesData?.badges.filter((badge) => badge.hasUnreadReplies).length ?? 0

  const navigationWithBadges = useMemo(
    () =>
      mainNavigation.map((item) =>
        item.href === '/forum' ? { ...item, badgeCount: forumBadgeCount } : item,
      ),
    [mainNavigation, forumBadgeCount],
  )

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <Sidebar
        mainNavigation={navigationWithBadges}
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

