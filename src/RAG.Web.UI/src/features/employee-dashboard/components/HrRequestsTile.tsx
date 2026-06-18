import React from 'react'
import { FileCheck } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { HrRequestsSummary } from '../types/employeeDashboard'

interface Props {
  summary: HrRequestsSummary
}

interface KpiProps {
  label: string
  value: number
  colorClass: string
  bgClass: string
}

function KpiBlock({ label, value, colorClass, bgClass }: KpiProps) {
  return (
    <div className={`flex flex-col items-center justify-center p-3 rounded-xl ${bgClass} flex-1`}>
      <span className={`text-2xl font-bold tabular-nums ${colorClass}`}>{value}</span>
      <span className="text-xs text-gray-500 dark:text-gray-400 mt-0.5 text-center leading-tight">
        {label}
      </span>
    </div>
  )
}

export function HrRequestsTile({ summary }: Props) {
  const { t } = useI18n()

  return (
    <div className="surface p-5 flex flex-col gap-3">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <div className="p-2 bg-purple-50 dark:bg-purple-900/20 rounded-lg">
            <FileCheck className="h-5 w-5 text-purple-600 dark:text-purple-400" />
          </div>
          <h2 className="font-semibold text-gray-900 dark:text-gray-100 text-sm">
            {t('employeeDashboard.hrRequests.title')}
          </h2>
        </div>
        <span className="text-xs text-gray-500 dark:text-gray-400 font-medium">
          {t('employeeDashboard.hrRequests.total', { total: String(summary.total) })}
        </span>
      </div>

      <div className="flex gap-2">
        <KpiBlock
          label={t('employeeDashboard.hrRequests.pending')}
          value={summary.pending}
          colorClass="text-amber-600 dark:text-amber-400"
          bgClass="bg-amber-50 dark:bg-amber-900/20"
        />
        <KpiBlock
          label={t('employeeDashboard.hrRequests.approved')}
          value={summary.approved}
          colorClass="text-green-600 dark:text-green-400"
          bgClass="bg-green-50 dark:bg-green-900/20"
        />
        <KpiBlock
          label={t('employeeDashboard.hrRequests.rejected')}
          value={summary.rejected}
          colorClass="text-red-600 dark:text-red-400"
          bgClass="bg-red-50 dark:bg-red-900/20"
        />
      </div>

      <div className="mt-auto">
        <div className="flex h-2 rounded-full overflow-hidden bg-gray-100 dark:bg-slate-800">
          {summary.approved > 0 && (
            <div
              className="bg-green-500 dark:bg-green-600 transition-all"
              style={{ width: `${(summary.approved / summary.total) * 100}%` }}
            />
          )}
          {summary.pending > 0 && (
            <div
              className="bg-amber-400 dark:bg-amber-500 transition-all"
              style={{ width: `${(summary.pending / summary.total) * 100}%` }}
            />
          )}
          {summary.rejected > 0 && (
            <div
              className="bg-red-500 dark:bg-red-600 transition-all"
              style={{ width: `${(summary.rejected / summary.total) * 100}%` }}
            />
          )}
        </div>
      </div>
    </div>
  )
}
