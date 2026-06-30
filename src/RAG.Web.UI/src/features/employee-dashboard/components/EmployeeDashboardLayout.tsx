import React from 'react'
import { useLocation } from 'react-router-dom'
import { EmployeeDashboardSidebar } from './EmployeeDashboardSidebar'
import { PanelLayout } from '@/shared/components/layout'
import { EmploymentContextDropdown, EmploymentContextProvider } from '../employment-context'

export function EmployeeDashboardLayout() {
  const location = useLocation()

  return (
    <EmploymentContextProvider>
      <PanelLayout
        sidebar={
          <EmployeeDashboardSidebar
            isActiveRoute={(href) => location.pathname.startsWith(href)}
          />
        }
        header={
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-end">
            <EmploymentContextDropdown />
          </div>
        }
      />
    </EmploymentContextProvider>
  )
}
