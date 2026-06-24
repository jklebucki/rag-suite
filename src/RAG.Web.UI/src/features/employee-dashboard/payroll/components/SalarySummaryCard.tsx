import { Banknote, CalendarDays, Download } from 'lucide-react'
import { Button } from '@/shared/components/ui'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { SalaryPayment } from '../types/salaryTypes'
import { formatSalaryDate, formatSalaryMoney, salaryPaymentTypeLabel } from '../services/salaryUtils'
import { SalaryStatusBadge } from './SalaryStatusBadge'

interface SalarySummaryCardProps {
  payment: SalaryPayment
  isDownloading: boolean
  canDownload: boolean
  onDownload: (paymentId: string) => void
}

export function SalarySummaryCard({
  payment,
  isDownloading,
  canDownload,
  onDownload,
}: SalarySummaryCardProps) {
  const { language, t } = useI18n()

  return (
    <div className="surface p-5 flex flex-col gap-5">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-primary-50 dark:bg-primary-900/20 rounded-lg">
            <Banknote className="h-5 w-5 text-primary-600 dark:text-primary-400" />
          </div>
          <div>
            <h2 className="font-semibold text-gray-900 dark:text-gray-100">
              {t('employeeDashboard.salary.summary.title')}
            </h2>
            <p className="text-xs text-gray-500 dark:text-gray-400">
              {salaryPaymentTypeLabel(payment.paymentType, t)}
            </p>
          </div>
        </div>
        <SalaryStatusBadge status={payment.status} />
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <div className="surface-muted p-4">
          <span className="text-xs font-medium text-gray-500 dark:text-gray-400">
            {t('employeeDashboard.salary.grossAmount')}
          </span>
          <p className="mt-1 text-xl font-bold text-gray-900 dark:text-gray-100 tabular-nums">
            {formatSalaryMoney(payment.grossAmount, payment.currency, language)}
          </p>
        </div>
        <div className="surface-muted p-4 border-primary-200 dark:border-primary-800">
          <span className="text-xs font-medium text-gray-500 dark:text-gray-400">
            {t('employeeDashboard.salary.netAmount')}
          </span>
          <p className="mt-1 text-xl font-bold text-primary-700 dark:text-primary-300 tabular-nums">
            {formatSalaryMoney(payment.netAmount, payment.currency, language)}
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-sm">
        <div className="flex items-center gap-2 text-gray-600 dark:text-gray-300">
          <CalendarDays className="h-4 w-4 text-gray-400" />
          <span>{t('employeeDashboard.salary.paymentDate')}: </span>
          <span className="font-medium tabular-nums text-gray-900 dark:text-gray-100">
            {formatSalaryDate(payment.paymentDate, language)}
          </span>
        </div>
        <div className="flex items-center gap-2 text-gray-600 dark:text-gray-300">
          <CalendarDays className="h-4 w-4 text-gray-400" />
          <span>{t('employeeDashboard.salary.period')}: </span>
          <span className="font-medium text-gray-900 dark:text-gray-100">
            {payment.periodLabel}
          </span>
        </div>
      </div>

      <Button
        variant="primary"
        size="sm"
        className="w-full sm:w-fit gap-2"
        disabled={!canDownload || isDownloading}
        onClick={() => onDownload(payment.id)}
      >
        <Download className="h-4 w-4" />
        {t('employeeDashboard.salary.downloadPayslip')}
      </Button>
    </div>
  )
}
