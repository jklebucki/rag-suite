import React from 'react'
import { AlertTriangle, FileClock, ShieldCheck } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import type {
  ExpiringDocument,
  ExpiringDocumentStatus,
} from '../types/employeeDashboard'

interface Props {
  documents: ExpiringDocument[]
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  })
}

function statusConfig(status: ExpiringDocumentStatus) {
  switch (status) {
    case 'critical':
      return {
        icon: AlertTriangle,
        rowClass:
          'border-red-200 bg-red-50/80 dark:border-red-900/40 dark:bg-red-900/10',
        accentClass: 'bg-red-500',
        badgeClass:
          'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300',
        iconClass: 'text-red-600 dark:text-red-400',
        titleClass: 'font-semibold text-gray-950 dark:text-gray-50',
      }
    case 'warning':
      return {
        icon: AlertTriangle,
        rowClass:
          'border-amber-200 bg-amber-50/70 dark:border-amber-900/40 dark:bg-amber-900/10',
        accentClass: 'bg-amber-500',
        badgeClass:
          'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300',
        iconClass: 'text-amber-600 dark:text-amber-400',
        titleClass: 'font-medium text-gray-900 dark:text-gray-100',
      }
    default:
      return {
        icon: ShieldCheck,
        rowClass:
          'border-gray-100 bg-white dark:border-slate-800 dark:bg-slate-900',
        accentClass: 'bg-transparent',
        badgeClass:
          'bg-gray-100 text-gray-600 dark:bg-slate-800 dark:text-gray-300',
        iconClass: 'text-green-600 dark:text-green-400',
        titleClass: 'font-medium text-gray-800 dark:text-gray-200',
      }
  }
}

function statusLabel(document: ExpiringDocument, t: ReturnType<typeof useI18n>['t']) {
  if (document.status === 'ok') {
    return t('employeeDashboard.expiringDocuments.status.ok')
  }

  return t('employeeDashboard.expiringDocuments.status.expiresIn', {
    days: String(document.daysUntilExpiry),
  })
}

export function ExpiringDocumentsTile({ documents }: Props) {
  const { t } = useI18n()

  const sortedDocuments = [...documents].sort(
    (a, b) => a.daysUntilExpiry - b.daysUntilExpiry
  )
  const urgentDocuments = sortedDocuments.filter((document) => document.status !== 'ok')
  const visibleDocuments = (urgentDocuments.length > 0 ? sortedDocuments : urgentDocuments).slice(0, 3)
  const hasCriticalDocument = visibleDocuments.some(
    (document) => document.status === 'critical'
  )

  return (
    <div className="surface p-5 flex flex-col gap-3">
      <div className="flex items-center justify-between gap-3">
        <div className="flex items-center gap-2 min-w-0">
          <div className="p-2 bg-amber-50 dark:bg-amber-900/20 rounded-lg">
            <FileClock className="h-5 w-5 text-amber-600 dark:text-amber-400" />
          </div>
          <h2 className="font-semibold text-gray-900 dark:text-gray-100 text-sm truncate">
            {t('employeeDashboard.expiringDocuments.title')}
          </h2>
        </div>
        {hasCriticalDocument && (
          <span className="inline-flex items-center gap-1 rounded-full bg-red-100 px-2 py-0.5 text-xs font-semibold text-red-700 dark:bg-red-900/40 dark:text-red-300">
            <AlertTriangle className="h-3 w-3" />
            {t('employeeDashboard.expiringDocuments.attention')}
          </span>
        )}
      </div>

      {visibleDocuments.length === 0 ? (
        <div className="flex flex-1 items-center gap-3 rounded-xl border border-green-100 bg-green-50/70 p-3 dark:border-green-900/40 dark:bg-green-900/10">
          <div className="rounded-lg bg-white/70 p-2 dark:bg-slate-900/50">
            <ShieldCheck className="h-4 w-4 text-green-600 dark:text-green-400" />
          </div>
          <div>
            <p className="text-sm font-medium text-gray-900 dark:text-gray-100">
              {t('employeeDashboard.expiringDocuments.emptyTitle')}
            </p>
            <p className="text-xs text-gray-500 dark:text-gray-400">
              {t('employeeDashboard.expiringDocuments.emptyDescription')}
            </p>
          </div>
        </div>
      ) : (
        <div className="space-y-2">
          {visibleDocuments.map((document) => {
            const cfg = statusConfig(document.status)
            const Icon = cfg.icon

            return (
              <div
                key={document.id}
                className={`relative overflow-hidden rounded-xl border p-3 ${cfg.rowClass}`}
              >
                {document.status === 'critical' && (
                  <div className={`absolute inset-y-0 left-0 w-1 ${cfg.accentClass}`} />
                )}

                <div className="flex items-start gap-2 pl-1">
                  <Icon className={`mt-0.5 h-4 w-4 flex-shrink-0 ${cfg.iconClass}`} />
                  <div className="min-w-0 flex-1">
                    <p className={`truncate text-sm leading-tight ${cfg.titleClass}`}>
                      {document.documentName}
                    </p>
                    <p className="mt-1 text-xs text-gray-500 dark:text-gray-400">
                      {t('employeeDashboard.expiringDocuments.validTo')}: {' '}
                      <span className="tabular-nums">{formatDate(document.validTo)}</span>
                    </p>
                  </div>
                  <span className={`max-w-[7.5rem] rounded-full px-2 py-0.5 text-center text-xs font-medium leading-tight ${cfg.badgeClass}`}>
                    {statusLabel(document, t)}
                  </span>
                </div>
              </div>
            )
          })}
        </div>
      )}

    </div>
  )
}
