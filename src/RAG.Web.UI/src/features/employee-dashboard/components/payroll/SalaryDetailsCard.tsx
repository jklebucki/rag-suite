import { ListChecks } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { SalaryPayment } from './salaryTypes'
import { formatSalaryDate, formatSalaryMoney, salaryPaymentTypeLabel } from './salaryUtils'
import { SalaryStatusBadge } from './SalaryStatusBadge'

interface SalaryDetailsCardProps {
  payment: SalaryPayment
}

export function SalaryDetailsCard({ payment }: SalaryDetailsCardProps) {
  const { language, t } = useI18n()
  const details = [
    [t('employeeDashboard.salary.details.baseSalary'), payment.details.baseSalary],
    [t('employeeDashboard.salary.details.bonus'), payment.details.bonus],
    [t('employeeDashboard.salary.details.allowances'), payment.details.allowances],
    [t('employeeDashboard.salary.details.overtime'), payment.details.overtime],
    [t('employeeDashboard.salary.details.deductions'), -payment.details.deductions],
    [t('employeeDashboard.salary.details.socialContributions'), -payment.details.socialContributions],
    [t('employeeDashboard.salary.details.healthContribution'), -payment.details.healthContribution],
    [t('employeeDashboard.salary.details.tax'), -payment.details.tax],
  ] as const

  return (
    <div className="surface p-5 flex flex-col gap-4">
      <div className="flex items-start justify-between gap-3">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-slate-100 dark:bg-slate-800 rounded-lg">
            <ListChecks className="h-5 w-5 text-slate-600 dark:text-slate-300" />
          </div>
          <div>
            <h2 className="font-semibold text-gray-900 dark:text-gray-100">
              {t('employeeDashboard.salary.details.title')}
            </h2>
            <p className="text-xs text-gray-500 dark:text-gray-400">
              {payment.periodLabel} - {salaryPaymentTypeLabel(payment.paymentType, t)}
            </p>
          </div>
        </div>
        <SalaryStatusBadge status={payment.status} />
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-sm">
        <div className="surface-muted p-3">
          <span className="text-xs text-gray-500 dark:text-gray-400">{t('employeeDashboard.salary.paymentDate')}</span>
          <p className="font-medium text-gray-900 dark:text-gray-100 tabular-nums">
            {formatSalaryDate(payment.paymentDate, language)}
          </p>
        </div>
        <div className="surface-muted p-3">
          <span className="text-xs text-gray-500 dark:text-gray-400">{t('employeeDashboard.salary.settlementPeriod')}</span>
          <p className="font-medium text-gray-900 dark:text-gray-100">
            {payment.periodLabel}
          </p>
        </div>
      </div>

      <div className="divide-y divide-gray-100 dark:divide-slate-800">
        {details.map(([label, amount]) => (
          <div key={label} className="flex items-center justify-between gap-4 py-2.5">
            <span className="text-sm text-gray-600 dark:text-gray-300">{label}</span>
            <span
              className={`text-sm font-medium tabular-nums ${
                amount < 0
                  ? 'text-red-600 dark:text-red-400'
                  : 'text-gray-900 dark:text-gray-100'
              }`}
            >
              {formatSalaryMoney(amount, payment.currency, language)}
            </span>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-2">
        <div className="surface-muted p-4">
          <span className="text-xs font-medium text-gray-500 dark:text-gray-400">
            {t('employeeDashboard.salary.grossAmount')}
          </span>
          <p className="mt-1 text-lg font-bold text-gray-900 dark:text-gray-100 tabular-nums">
            {formatSalaryMoney(payment.grossAmount, payment.currency, language)}
          </p>
        </div>
        <div className="surface-muted p-4 border-primary-200 dark:border-primary-800">
          <span className="text-xs font-medium text-gray-500 dark:text-gray-400">
            {t('employeeDashboard.salary.netAmount')}
          </span>
          <p className="mt-1 text-lg font-bold text-primary-700 dark:text-primary-300 tabular-nums">
            {formatSalaryMoney(payment.netAmount, payment.currency, language)}
          </p>
        </div>
      </div>
    </div>
  )
}
