import { useMemo, useState } from 'react'
import { CalendarRange, XCircle } from 'lucide-react'
import { useEmploymentContext } from '../../employment-context'
import { useHrAbsenceRequests } from '../hooks/useHrAbsenceRequests'
import { selectHrAbsenceRequestsByCompany } from '../services/hrAbsenceSelectors'
import type { HrAbsenceRequestFilters } from '../types/hrAbsenceTypes'
import { HrAbsenceDashboard } from './HrAbsenceDashboard'
import { HrAbsenceRequestsTable } from './HrAbsenceRequestsTable'
import { HrAbsenceTabs, type HrAbsenceTab } from './HrAbsenceTabs'
import { useHrAbsenceT } from './hrAbsenceTranslations'

export function HrAbsenceManagement() {
  const t = useHrAbsenceT()
  const { activeContextId, activeContext, isLoading: isEmploymentLoading } = useEmploymentContext()
  const { requests, isLoading, error } = useHrAbsenceRequests()
  const [activeTab, setActiveTab] = useState<HrAbsenceTab>('dashboard')
  const [requestFilters, setRequestFilters] = useState<HrAbsenceRequestFilters>({})

  const companyRequests = useMemo(
    () => selectHrAbsenceRequestsByCompany(requests, activeContextId),
    [activeContextId, requests]
  )

  function handleDashboardNavigation(filters: HrAbsenceRequestFilters) {
    setRequestFilters(filters)
    setActiveTab('requests')
  }

  function handleTabChange(tab: HrAbsenceTab) {
    setRequestFilters({})
    setActiveTab(tab)
  }

  if (isLoading || isEmploymentLoading) {
    return (
      <div className="flex h-64 items-center justify-center text-gray-600 dark:text-gray-400">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary-200 border-t-primary-600" />
      </div>
    )
  }

  if (error) {
    return (
      <div className="surface p-6 text-center text-red-600 dark:text-red-400">
        <XCircle className="mx-auto mb-2 h-8 w-8" />
        <p className="text-sm">{t(error)}</p>
      </div>
    )
  }

  return (
    <div className="space-y-6 text-gray-900 dark:text-gray-100">
      <div className="flex items-center gap-3">
        <CalendarRange className="h-8 w-8 flex-shrink-0 text-primary-600 dark:text-primary-400" />
        <div>
          <h1 className="text-2xl font-bold">{t('title')}</h1>
          <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
            {t('subtitle')} · {activeContext?.companyName}
          </p>
        </div>
      </div>

      <HrAbsenceTabs active={activeTab} onChange={handleTabChange} />

      {activeTab === 'dashboard' && (
        <HrAbsenceDashboard requests={companyRequests} onNavigate={handleDashboardNavigation} />
      )}

      {activeTab === 'requests' && (
        <HrAbsenceRequestsTable
          key={`${activeContextId}-${JSON.stringify(requestFilters)}`}
          requests={companyRequests}
          initialFilters={requestFilters}
        />
      )}
    </div>
  )
}
