import { ChevronRight, Download, ReceiptText } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { SalaryPayment } from './salaryTypes'
import { formatSalaryDate, formatSalaryMoney, salaryPaymentTypeLabel } from './salaryUtils'
import { SalaryStatusBadge } from './SalaryStatusBadge'

interface SalaryHistoryTableProps {
  payments: SalaryPayment[]
  selectedPaymentId: string
  isDownloading: boolean
  canDownload: boolean
  onSelect: (payment: SalaryPayment) => void
  onDownload: (paymentId: string) => void
}

export function SalaryHistoryTable({
  payments,
  selectedPaymentId,
  isDownloading,
  canDownload,
  onSelect,
  onDownload,
}: SalaryHistoryTableProps) {
  const { language, t } = useI18n()

  return (
    <div className="surface overflow-hidden">
      <div className="px-5 py-4 border-b border-gray-100 dark:border-slate-800 flex items-center gap-2">
        <div className="p-2 bg-primary-50 dark:bg-primary-900/20 rounded-lg">
          <ReceiptText className="h-5 w-5 text-primary-600 dark:text-primary-400" />
        </div>
        <h2 className="font-semibold text-gray-900 dark:text-gray-100">
          {t('employeeDashboard.salary.history.title')}
        </h2>
        <span className="ml-auto text-xs text-gray-400 dark:text-gray-500 tabular-nums">
          {t('employeeDashboard.salary.history.recordsCount', {
            count: String(payments.length),
          })}
        </span>
      </div>

      <div className="hidden lg:block overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-gray-100 dark:border-slate-800 bg-gray-50 dark:bg-slate-900/50">
              <th className="text-left px-5 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                {t('employeeDashboard.salary.history.col.paymentDate')}
              </th>
              <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                {t('employeeDashboard.salary.history.col.period')}
              </th>
              <th className="text-right px-4 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                {t('employeeDashboard.salary.history.col.gross')}
              </th>
              <th className="text-right px-4 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                {t('employeeDashboard.salary.history.col.net')}
              </th>
              <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                {t('employeeDashboard.salary.history.col.type')}
              </th>
              <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                {t('employeeDashboard.salary.history.col.status')}
              </th>
              <th className="text-right px-5 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                {t('employeeDashboard.salary.history.col.actions')}
              </th>
            </tr>
          </thead>
          <tbody>
            {payments.map((payment) => {
              const isSelected = payment.id === selectedPaymentId
              return (
                <tr
                  key={payment.id}
                  className={`border-b border-gray-50 dark:border-slate-800 transition-colors last:border-0 ${
                    isSelected
                      ? 'bg-primary-50/70 dark:bg-primary-900/10'
                      : 'hover:bg-gray-50 dark:hover:bg-slate-800/50'
                  }`}
                >
                  <td className="px-5 py-3.5 text-gray-600 dark:text-gray-300 tabular-nums">
                    {formatSalaryDate(payment.paymentDate, language)}
                  </td>
                  <td className="px-4 py-3.5 font-medium text-gray-900 dark:text-gray-100">
                    {payment.periodLabel}
                  </td>
                  <td className="px-4 py-3.5 text-right text-gray-600 dark:text-gray-300 tabular-nums">
                    {formatSalaryMoney(payment.grossAmount, payment.currency, language)}
                  </td>
                  <td className="px-4 py-3.5 text-right font-semibold text-gray-900 dark:text-gray-100 tabular-nums">
                    {formatSalaryMoney(payment.netAmount, payment.currency, language)}
                  </td>
                  <td className="px-4 py-3.5 text-gray-600 dark:text-gray-300">
                    {salaryPaymentTypeLabel(payment.paymentType, t)}
                  </td>
                  <td className="px-4 py-3.5">
                    <SalaryStatusBadge status={payment.status} />
                  </td>
                  <td className="px-5 py-3.5">
                    <div className="flex items-center justify-end gap-3">
                      <button
                        type="button"
                        onClick={() => onSelect(payment)}
                        className="inline-flex items-center gap-1 text-xs font-medium text-primary-600 dark:text-primary-400 hover:text-primary-700 dark:hover:text-primary-300 transition-colors"
                      >
                        <ChevronRight className="h-3.5 w-3.5" />
                        {t('employeeDashboard.salary.actions.details')}
                      </button>
                      <button
                        type="button"
                        disabled={!canDownload || isDownloading || payment.status === 'pending'}
                        onClick={() => onDownload(payment.id)}
                        className="inline-flex items-center gap-1 text-xs font-medium text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-gray-100 transition-colors disabled:opacity-50 disabled:pointer-events-none"
                      >
                        <Download className="h-3.5 w-3.5" />
                        PDF
                      </button>
                    </div>
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>

      <div className="lg:hidden divide-y divide-gray-100 dark:divide-slate-800">
        {payments.map((payment) => (
          <div key={payment.id} className="px-4 py-4 space-y-3">
            <div className="flex items-start justify-between gap-3">
              <div>
                <p className="text-sm font-semibold text-gray-900 dark:text-gray-100">
                  {payment.periodLabel}
                </p>
                <p className="text-xs text-gray-500 dark:text-gray-400 tabular-nums">
                  {formatSalaryDate(payment.paymentDate, language)}
                </p>
              </div>
              <SalaryStatusBadge status={payment.status} />
            </div>
            <div className="grid grid-cols-2 gap-3 text-xs">
              <div>
              <span className="text-gray-500 dark:text-gray-400">{t('employeeDashboard.salary.grossAmount')}</span>
                <p className="font-medium text-gray-900 dark:text-gray-100 tabular-nums">
                  {formatSalaryMoney(payment.grossAmount, payment.currency, language)}
                </p>
              </div>
              <div>
              <span className="text-gray-500 dark:text-gray-400">{t('employeeDashboard.salary.netAmount')}</span>
                <p className="font-semibold text-primary-700 dark:text-primary-300 tabular-nums">
                  {formatSalaryMoney(payment.netAmount, payment.currency, language)}
                </p>
              </div>
            </div>
            <p className="text-xs text-gray-600 dark:text-gray-300">
              {salaryPaymentTypeLabel(payment.paymentType, t)}
            </p>
            <div className="flex items-center gap-4 pt-1">
              <button
                type="button"
                onClick={() => onSelect(payment)}
                className="text-xs font-medium text-primary-600 dark:text-primary-400 hover:underline"
              >
                {t('employeeDashboard.salary.actions.details')}
              </button>
              <button
                type="button"
                disabled={!canDownload || isDownloading || payment.status === 'pending'}
                onClick={() => onDownload(payment.id)}
                className="text-xs font-medium text-gray-600 dark:text-gray-300 hover:underline disabled:opacity-50"
              >
                {t('employeeDashboard.salary.actions.downloadPdf')}
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
