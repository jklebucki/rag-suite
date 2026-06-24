import React from 'react'
import { useLocation } from 'react-router-dom'
import { EmployeeDashboardSidebar } from './EmployeeDashboardSidebar'
import { PanelLayout } from '@/shared/components/layout'

export function EmployeeDashboardLayout() {
  const location = useLocation()

  return (
    <PanelLayout
      sidebar={
        <EmployeeDashboardSidebar
          isActiveRoute={(href) => location.pathname.startsWith(href)}
        />
      }
    />
  )
}
