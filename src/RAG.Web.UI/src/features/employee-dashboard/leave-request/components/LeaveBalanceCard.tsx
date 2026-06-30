import type { ReactNode } from 'react'
import { CalendarDays, Clock3, TrendingDown, Zap } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'

interface LeaveBalanceCardProps {
  annual: number
  carryover: number
  onDemand: number
}

const ANNUAL_LEAVE_LIMIT = 26
const ON_DEMAND_LEAVE_LIMIT = 4
const SLOT_INDICATOR_COUNT = 4
const CARRYOVER_USE_BY_DATE = '30.09.2026'
const CARRYOVER_PERIOD_START = new Date('2026-01-01T00:00:00')
const CARRYOVER_PERIOD_END = new Date('2026-09-30T23:59:59')

function percentage(value: number, limit: number) {
  if (limit <= 0) {
    return 0
  }

  return Math.min(100, Math.max(0, Math.round((value / limit) * 100)))
}

function carryoverUrgencyPercentage() {
  const now = new Date()
  const totalWindow = CARRYOVER_PERIOD_END.getTime() - CARRYOVER_PERIOD_START.getTime()
  const elapsed = now.getTime() - CARRYOVER_PERIOD_START.getTime()

  return percentage(elapsed, totalWindow)
}

function filledSlots(progress: number) {
  if (progress <= 0) {
    return 0
  }

  return Math.min(SLOT_INDICATOR_COUNT, Math.ceil(progress / (100 / SLOT_INDICATOR_COUNT)))
}

export function LeaveBalanceCard({ annual, carryover, onDemand }: LeaveBalanceCardProps) {
  const { t } = useI18n()
  const total = annual + carryover
  const annualProgress = percentage(annual, ANNUAL_LEAVE_LIMIT)
  const carryoverProgress = carryoverUrgencyPercentage()
  const onDemandProgress = percentage(onDemand, ON_DEMAND_LEAVE_LIMIT)

  return (
    <div className="surface p-5">
      <div className="mb-4 flex items-center gap-2">
        <div className="flex items-center gap-2">
          <div className="p-2 bg-green-50 dark:bg-green-900/20 rounded-lg">
            <CalendarDays className="h-5 w-5 text-green-600 dark:text-green-400" />
          </div>
          <h2 className="font-semibold text-gray-900 dark:text-gray-100">
            {t('employeeDashboard.leave.balance.title')}
          </h2>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
        <BalanceTile
          icon={<CalendarDays className="h-4 w-4" />}
          label={t('employeeDashboard.leave.balance.annual')}
          value={annual}
          description={t('employeeDashboard.leave.balance.availableDays', {
            days: String(annual),
          })}
          ratio={t('employeeDashboard.leave.balance.limitRatio', {
            value: String(annual),
            limit: String(ANNUAL_LEAVE_LIMIT),
          })}
          progress={annualProgress}
          tone="blue"
          slots={{
            total: SLOT_INDICATOR_COUNT,
            available: filledSlots(annualProgress),
          }}
        />
        <BalanceTile
          icon={<Clock3 className="h-4 w-4" />}
          label={t('employeeDashboard.leave.balance.carryover')}
          value={carryover}
          description={t('employeeDashboard.leave.balance.useBy', {
            date: CARRYOVER_USE_BY_DATE,
          })}
          ratio={t('employeeDashboard.leave.balance.urgency')}
          progress={carryoverProgress}
          tone="purple"
          slots={{
            total: SLOT_INDICATOR_COUNT,
            available: filledSlots(carryoverProgress),
          }}
        />
        <BalanceTile
          icon={<Zap className="h-4 w-4" />}
          label={t('employeeDashboard.leave.balance.onDemand')}
          value={onDemand}
          description={t('employeeDashboard.leave.balance.statutoryLimit')}
          ratio={t('employeeDashboard.leave.balance.limitRatio', {
            value: String(onDemand),
            limit: String(ON_DEMAND_LEAVE_LIMIT),
          })}
          progress={onDemandProgress}
          tone="orange"
          slots={{
            total: ON_DEMAND_LEAVE_LIMIT,
            available: onDemand,
          }}
        />
      </div>

      <div className="mt-3 flex items-center gap-1.5 text-xs text-gray-500 dark:text-gray-500">
        <TrendingDown className="h-3.5 w-3.5" />
        <span>{t('employeeDashboard.leave.balance.totalAvailable', { total: String(total) })}</span>
      </div>
    </div>
  )
}

interface BalanceTileProps {
  icon: ReactNode
  label: string
  value: number
  description: string
  ratio: string
  progress: number
  tone: 'blue' | 'purple' | 'orange'
  slots?: {
    total: number
    available: number
  }
}

const toneClasses = {
  blue: {
    tile: 'bg-blue-50/70 text-blue-700 ring-blue-100 dark:bg-blue-900/10 dark:text-blue-300 dark:ring-blue-900/30',
    icon: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300',
    bar: 'bg-blue-500 dark:bg-blue-400',
    dot: 'bg-blue-500 dark:bg-blue-400',
  },
  purple: {
    tile: 'bg-purple-50/70 text-purple-700 ring-purple-100 dark:bg-purple-900/10 dark:text-purple-300 dark:ring-purple-900/30',
    icon: 'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-300',
    bar: 'bg-purple-500 dark:bg-purple-400',
    dot: 'bg-purple-500 dark:bg-purple-400',
  },
  orange: {
    tile: 'bg-orange-50/70 text-orange-700 ring-orange-100 dark:bg-orange-900/10 dark:text-orange-300 dark:ring-orange-900/30',
    icon: 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-300',
    bar: 'bg-orange-500 dark:bg-orange-400',
    dot: 'bg-orange-500 dark:bg-orange-400',
  },
}

function BalanceTile({
  icon,
  label,
  value,
  description,
  ratio,
  progress,
  tone,
  slots,
}: BalanceTileProps) {
  const { t } = useI18n()
  const classes = toneClasses[tone]

  return (
    <div className={`rounded-xl p-4 ring-1 ${classes.tile}`}>
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <div className="text-xs font-semibold uppercase tracking-wide opacity-80">
            {label}
          </div>
          <div className="mt-2 flex items-baseline gap-1.5">
            <span className="text-3xl font-bold leading-none tabular-nums text-gray-900 dark:text-gray-100">
              {value}
            </span>
            <span className="text-sm font-medium text-gray-500 dark:text-gray-400">
              {t('employeeDashboard.leave.balance.daysUnit')}
            </span>
          </div>
        </div>
        <div className={`rounded-lg p-2 ${classes.icon}`}>
          {icon}
        </div>
      </div>

      <div className="mt-3 min-h-10">
        <p className="text-sm font-medium text-gray-700 dark:text-gray-200">
          {description}
        </p>
        <p className="mt-0.5 text-xs text-gray-500 dark:text-gray-400">
          {ratio}
        </p>
      </div>

      <ProgressBar value={progress} barClassName={classes.bar} />

      {slots && (
        <div className="mt-3 flex items-center gap-1.5" aria-hidden="true">
          {Array.from({ length: slots.total }).map((_, index) => (
            <span
              key={index}
              className={`h-2 w-2 rounded-full ${
                index < slots.available
                  ? classes.dot
                  : 'bg-gray-200 dark:bg-slate-700'
              }`}
            />
          ))}
        </div>
      )}
    </div>
  )
}

interface ProgressBarProps {
  value: number
  barClassName: string
}

function ProgressBar({ value, barClassName }: ProgressBarProps) {
  return (
    <div className="mt-3 h-2 overflow-hidden rounded-full bg-gray-200/80 dark:bg-slate-800">
      <div
        className={`h-full rounded-full transition-all ${barClassName}`}
        style={{ width: `${value}%` }}
      />
    </div>
  )
}
