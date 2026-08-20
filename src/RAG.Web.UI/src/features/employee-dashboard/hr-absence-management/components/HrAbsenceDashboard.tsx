import { AlertTriangle, CalendarClock, ClipboardList, Hourglass } from 'lucide-react'
import { TeamStatisticsCard } from '../../manager-panel/components/TeamStatisticsCard'
import { getHrAbsenceStatistics } from '../services/hrAbsenceSelectors'
import type { HrAbsenceRequest, HrAbsenceRequestFilters } from '../types/hrAbsenceTypes'
import { useHrAbsenceT } from './hrAbsenceTranslations'

interface HrAbsenceDashboardProps {
  requests: HrAbsenceRequest[]
  onNavigate: (filters: HrAbsenceRequestFilters) => void
}

export function HrAbsenceDashboard({ requests, onNavigate }: HrAbsenceDashboardProps) {
  const t = useHrAbsenceT()
  const statistics = getHrAbsenceStatistics(requests)

  return (
    <div className="grid grid-cols-[repeat(auto-fit,minmax(13.5rem,1fr))] gap-4">
      <TeamStatisticsCard
        title={t('dashboard.all')}
        value={statistics.all}
        icon={ClipboardList}
        description={t('dashboard.allDesc')}
        onClick={() => onNavigate({})}
      />
      <TeamStatisticsCard
        title={t('dashboard.pending')}
        value={statistics.pending}
        icon={Hourglass}
        description={t('dashboard.pendingDesc')}
        tone="warning"
        onClick={() => onNavigate({ status: 'pending' })}
      />
      <TeamStatisticsCard
        title={t('dashboard.payrollClosure')}
        value={statistics.payrollClosure}
        icon={CalendarClock}
        description={t('dashboard.payrollClosureDesc')}
        tone="warning"
        onClick={() => onNavigate({ category: 'payrollClosure' })}
      />
      <TeamStatisticsCard
        title={t('dashboard.overdue')}
        value={statistics.overdue}
        icon={AlertTriangle}
        description={t('dashboard.overdueDesc')}
        tone={statistics.overdue > 0 ? 'danger' : 'neutral'}
        onClick={() => onNavigate({ category: 'overdue' })}
      />
    </div>
  )
}
