import { CalendarDays, TrendingDown } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'

interface LeaveBalanceCardProps {
  annual: number
  carryover: number
  onDemand: number
  total: number
}

export function LeaveBalanceCard({ annual, carryover, onDemand, total }: LeaveBalanceCardProps) {
  const { t } = useI18n()

  return (
    <div className="surface p-5">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <div className="p-2 bg-green-50 dark:bg-green-900/20 rounded-lg">
            <CalendarDays className="h-5 w-5 text-green-600 dark:text-green-400" />
          </div>
          <h2 className="font-semibold text-gray-900 dark:text-gray-100">
            {t('employeeDashboard.leave.balance.title')}
          </h2>
        </div>
        <div className="text-right">
          <span className="text-3xl font-bold text-green-600 dark:text-green-400 tabular-nums">
            {total}
          </span>
          <span className="text-sm font-normal text-gray-500 dark:text-gray-400 ml-1">
            {t('employeeDashboard.leave.balance.days')}
          </span>
        </div>
      </div>

      <div className="grid grid-cols-3 gap-3">
        <BalancePill
          label={t('employeeDashboard.leave.balance.annual')}
          value={annual}
          colorClass="bg-blue-50 dark:bg-blue-900/20 text-blue-700 dark:text-blue-400"
        />
        <BalancePill
          label={t('employeeDashboard.leave.balance.carryover')}
          value={carryover}
          colorClass="bg-purple-50 dark:bg-purple-900/20 text-purple-700 dark:text-purple-400"
        />
        <BalancePill
          label={t('employeeDashboard.leave.balance.onDemand')}
          value={onDemand}
          colorClass="bg-orange-50 dark:bg-orange-900/20 text-orange-700 dark:text-orange-400"
        />
      </div>

      <div className="mt-3 flex items-center gap-1.5 text-xs text-gray-500 dark:text-gray-500">
        <TrendingDown className="h-3.5 w-3.5" />
        <span>{t('employeeDashboard.leave.balance.totalAvailable', { total: String(total) })}</span>
      </div>
    </div>
  )
}

interface BalancePillProps {
  label: string
  value: number
  colorClass: string
}

function BalancePill({ label, value, colorClass }: BalancePillProps) {
  const { t } = useI18n()

  return (
    <div className={`rounded-xl p-3 text-center ${colorClass}`}>
      <div className="text-2xl font-bold tabular-nums">{value}</div>
      <div className="text-xs mt-0.5 font-medium opacity-80">{label}</div>
      <div className="text-xs opacity-60">{t('employeeDashboard.leave.balance.daysUnit')}</div>
    </div>
  )
}
