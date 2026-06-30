import { AlertTriangle, ClipboardList, HeartPulse, Users, UserX } from 'lucide-react'
import type { ManagerPanelData } from '../types/managerTypes'
import { AuditLogTable } from './AuditLogTable'
import { TeamStatisticsCard } from './TeamStatisticsCard'
import { useManagerT } from './managerTranslations'

interface ManagerDashboardProps {
  data: ManagerPanelData
}

export function ManagerDashboard({ data }: ManagerDashboardProps) {
  const t = useManagerT()
  const stats = data.statistics
  const sickLeaves = data.teamMembers.filter(
    (member) => member.presenceStatus === 'absence'
  ).length

  return (
    <div className="space-y-5">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-5">
        <TeamStatisticsCard
          title={t('dashboard.directReports')}
          value={stats.directReports}
          icon={Users}
          description={t('dashboard.directReportsDesc')}
        />
        <TeamStatisticsCard
          title={t('dashboard.pendingRequests')}
          value={stats.pendingRequests}
          icon={ClipboardList}
          description={t('dashboard.pendingRequestsDesc')}
          tone="warning"
        />
        <TeamStatisticsCard
          title={t('dashboard.absentToday')}
          value={stats.absentToday}
          icon={UserX}
          description={t('dashboard.absentTodayDesc')}
        />
        <TeamStatisticsCard
          title={t('dashboard.sickLeaves')}
          value={sickLeaves}
          icon={HeartPulse}
          description={t('dashboard.sickLeavesDesc')}
          tone="muted"
        />
        <TeamStatisticsCard
          title={t('dashboard.vacationConflicts')}
          value={stats.vacationConflicts}
          icon={AlertTriangle}
          description={t('dashboard.vacationConflictsDesc')}
          tone={stats.vacationConflicts > 0 ? 'danger' : 'neutral'}
        />
      </div>

      {stats.vacationConflicts > 0 && (
        <div className="rounded-2xl border border-amber-200 bg-amber-50 px-5 py-4 text-sm text-amber-800 dark:border-amber-800 dark:bg-amber-900/20 dark:text-amber-300">
          {t('dashboard.conflictAlert')}
        </div>
      )}

      <AuditLogTable logs={data.operationLogs} />
    </div>
  )
}
