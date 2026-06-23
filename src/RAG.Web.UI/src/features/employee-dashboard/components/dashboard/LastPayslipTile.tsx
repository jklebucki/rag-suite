import React from 'react'
import { Banknote, Download } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import { Button } from '@/shared/components/ui'
import type { LastPayslip } from '../../types/employeeDashboard'

interface Props {
  payslip: LastPayslip
}

function formatMoney(amount: number, currency: string): string {
  return new Intl.NumberFormat(undefined, {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amount)
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  })
}

export function LastPayslipTile({ payslip }: Props) {
  const { t } = useI18n()

  const handleDownload = () => {
    if (payslip.downloadUrl) {
      window.open(payslip.downloadUrl, '_blank', 'noopener,noreferrer')
    }
  }

  return (
    <div className="surface p-5 flex flex-col gap-3">
      <div className="flex items-center gap-2">
        <div className="p-2 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
          <Banknote className="h-5 w-5 text-blue-600 dark:text-blue-400" />
        </div>
        <h2 className="font-semibold text-gray-900 dark:text-gray-100 text-sm">
          {t('employeeDashboard.payslip.title')}
        </h2>
        <span className="ml-auto text-xs text-gray-500 dark:text-gray-400 font-medium">
          {payslip.periodLabel}
        </span>
      </div>

      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <span className="text-sm text-gray-600 dark:text-gray-400">
            {t('employeeDashboard.payslip.gross')}
          </span>
          <span className="text-sm font-medium text-gray-900 dark:text-gray-100 tabular-nums">
            {formatMoney(payslip.grossAmount, payslip.currency)}
          </span>
        </div>

        <div className="flex items-center justify-between">
          <span className="text-sm text-gray-600 dark:text-gray-400">
            {t('employeeDashboard.payslip.net')}
          </span>
          <span className="text-base font-bold text-primary-700 dark:text-primary-300 tabular-nums">
            {formatMoney(payslip.netAmount, payslip.currency)}
          </span>
        </div>

        <div className="flex items-center justify-between border-t border-gray-100 dark:border-slate-800 pt-2">
          <span className="text-xs text-gray-500 dark:text-gray-500">
            {t('employeeDashboard.payslip.paymentDate')}
          </span>
          <span className="text-xs text-gray-700 dark:text-gray-300 tabular-nums">
            {formatDate(payslip.paymentDate)}
          </span>
        </div>
      </div>

      <Button
        variant="outline"
        size="sm"
        className="mt-auto w-full gap-1.5 text-xs"
        disabled={!payslip.downloadUrl}
        onClick={handleDownload}
      >
        <Download className="h-3.5 w-3.5" />
        {t('employeeDashboard.payslip.download')}
      </Button>
    </div>
  )
}
