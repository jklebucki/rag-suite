import React from 'react'
import { BarChart3, Briefcase, User, CalendarDays, Banknote, FolderOpen } from 'lucide-react'
import { PanelSidebar, type PanelSidebarItem } from '@/shared/components/layout'
import { useAuth } from '@/shared/contexts/AuthContext'
import { useI18n } from '@/shared/contexts/I18nContext'

interface Props {
  isActiveRoute: (href: string) => boolean
}

type EmployeeDashboardNavigationItem = PanelSidebarItem & {
  visible: boolean
}

export function EmployeeDashboardSidebar({ isActiveRoute }: Props) {
  const { user } = useAuth()
  const roles = user?.roles || []
  const isManagerOrAdmin = roles.includes('Admin') || roles.includes('Manager')
  const { t } = useI18n()

  // Employee dashboard navigation: routes, labels, icons, and role visibility stay local to this feature.
  const navigationItems: PanelSidebarItem[] = ([
    {
      to: '/employee-dashboard/overview',
      icon: BarChart3,
      label: t('employeeDashboard.dashboard'),
      visible: true,
    },
    {
      to: '/employee-dashboard/manager',
      icon: Briefcase,
      label: t('employeeDashboard.managerPanel'),
      visible: isManagerOrAdmin,
    },
    {
      to: '/employee-dashboard/personal',
      icon: User,
      label: t('employeeDashboard.personalData'),
      visible: true,
    },
    {
      to: '/employee-dashboard/leave',
      icon: CalendarDays,
      label: t('employeeDashboard.leaveRequest'),
      visible: true,
    },
    {
      to: '/employee-dashboard/salary',
      icon: Banknote,
      label: t('employeeDashboard.salary'),
      visible: true,
    },
    {
      to: '/employee-dashboard/documents',
      icon: FolderOpen,
      label: t('employeeDashboard.documents'),
      visible: true,
    },
  ] satisfies EmployeeDashboardNavigationItem[])
    .filter((item) => item.visible)
    .map(({ visible: _visible, ...item }) => item)

  return (
    <PanelSidebar
      title={t('nav.employeeDashboard')}
      menuLabel="Employee Dashboard menu"
      items={navigationItems}
      isActiveRoute={isActiveRoute}
    />
  )
}
