import React from 'react'
import { CalendarDays, TrendingDown } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { LeaveBalance } from '../types/employeeDashboard'

interface Props {
  balance: LeaveBalance
}

interface LeaveRowProps {
  label: string
  days: number
  accent?: boolean
}

function LeaveRow({ label, days, accent = false }: LeaveRowProps) {
  return (
    <div className="flex items-center justify-between py-2 border-b border-gray-100 dark:border-slate-800 last:border-0">
      <span className="text-sm text-gray-600 dark:text-gray-400">{label}</span>
      <span
        className={`text-sm font-semibold tabular-nums ${
          accent
            ? 'text-primary-700 dark:text-primary-300 text-base'
            : 'text-gray-900 dark:text-gray-100'
        }`}
      >
        {days} d
      </span>
    </div>
  )
}

export function LeaveBalanceTile({ balance }: Props) {
  const { t } = useI18n()

  return (
    <div className="surface p-5 flex flex-col gap-3">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <div className="p-2 bg-green-50 dark:bg-green-900/20 rounded-lg">
            <CalendarDays className="h-5 w-5 text-green-600 dark:text-green-400" />
          </div>
          <h2 className="font-semibold text-gray-900 dark:text-gray-100 text-sm">
            {t('employeeDashboard.leave.title')}
          </h2>
        </div>
        <span className="text-2xl font-bold text-green-600 dark:text-green-400 tabular-nums">
          {balance.total}
          <span className="text-sm font-normal text-gray-500 dark:text-gray-400 ml-1">d</span>
        </span>
      </div>

      <div>
        <LeaveRow label={t('employeeDashboard.leave.annual')} days={balance.annual} />
        <LeaveRow label={t('employeeDashboard.leave.carryover')} days={balance.carryover} />
        <LeaveRow label={t('employeeDashboard.leave.onDemand')} days={balance.onDemand} />
      </div>

      <div className="mt-auto pt-1 flex items-center gap-1.5 text-xs text-gray-500 dark:text-gray-500">
        <TrendingDown className="h-3.5 w-3.5" />
        <span>{t('employeeDashboard.leave.totalAvailable', { total: String(balance.total) })}</span>
      </div>
    </div>
  )
}
