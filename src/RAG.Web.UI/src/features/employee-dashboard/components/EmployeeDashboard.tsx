import React from 'react'
import { useI18n } from '@/shared/contexts/I18nContext'
import { useEmployeeDashboardOverview } from '../hooks/useEmployeeDashboardOverview'
import { EmployeeSummaryCard } from './EmployeeSummaryCard'
import { LeaveBalanceTile } from './LeaveBalanceTile'
import { LastPayslipTile } from './LastPayslipTile'
import { HrRequestsTile } from './HrRequestsTile'
import { EmployeeProfileCard } from './EmployeeProfileCard'
import { NotificationsCenter } from './NotificationsCenter'
import { UpcomingEvents } from './UpcomingEvents'
import { QuickActions } from './QuickActions'

export function EmployeeDashboard() {
  const { t } = useI18n()
  const { data, isLoading, error } = useEmployeeDashboardOverview()

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64 text-gray-600 dark:text-gray-400">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600" />
      </div>
    )
  }

  if (error || !data) {
    return (
      <div className="text-center py-12 text-gray-600 dark:text-gray-400">
        <p>{t('common.error')}</p>
      </div>
    )
  }

  return (
    <div className="space-y-4 text-gray-900 dark:text-gray-100">
      {/* Section 1 – Employee summary header */}
      <EmployeeSummaryCard profile={data.profile} />

      {/* Section 2 – Info tiles */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <LeaveBalanceTile balance={data.leaveBalance} />
        <LastPayslipTile payslip={data.lastPayslip} />
        <HrRequestsTile summary={data.hrRequestsSummary} />
      </div>

      {/* Section 6 – Quick actions */}
      <QuickActions />

      {/* Sections 3 & 4 – Profile card + Notifications */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <EmployeeProfileCard profile={data.profile} />
        <NotificationsCenter notifications={data.notifications} />
      </div>

      {/* Section 5 – Upcoming events */}
      <UpcomingEvents events={data.upcomingEvents} />
    </div>
  )
}

