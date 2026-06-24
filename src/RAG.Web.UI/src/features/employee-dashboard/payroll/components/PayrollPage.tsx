import { useMemo, useState } from 'react'
import { Banknote, XCircle } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { TranslationKeys } from '@/shared/types/i18n'
import type { SalaryPayment } from '../types/salaryTypes'
import { useSalaryData } from '../hooks/useSalaryData'
import { BankAccountCard } from './BankAccountCard'
import { PayslipDownloadCard } from './PayslipDownloadCard'
import { SalaryDetailsCard } from './SalaryDetailsCard'
import { SalaryHistoryTable } from './SalaryHistoryTable'
import { SalarySummaryCard } from './SalarySummaryCard'

export function PayrollPage() {
  const { t } = useI18n()
  const {
    data,
    isLoading,
    isDownloading,
    error,
    downloadMessage,
    downloadPayslip,
    clearDownloadMessage,
  } = useSalaryData()
  const [selectedPaymentId, setSelectedPaymentId] = useState<string | null>(null)

  const selectedPayment = useMemo<SalaryPayment | null>(() => {
    if (!data) return null

    return (
      data.paymentHistory.find((payment) => payment.id === selectedPaymentId) ??
      data.latestPayment
    )
  }, [data, selectedPaymentId])

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-48">
        <div className="h-8 w-8 rounded-full border-4 border-primary-200 border-t-primary-600 animate-spin" />
      </div>
    )
  }

  if (error || !data || !selectedPayment) {
    return (
      <div className="surface p-6 text-center text-red-600 dark:text-red-400">
        <XCircle className="h-8 w-8 mx-auto mb-2" />
        <p className="text-sm">
          {error ? t(error as keyof TranslationKeys) : t('employeeDashboard.salary.error.noData')}
        </p>
      </div>
    )
  }

  if (!data.canViewSalaryData) {
    return (
      <div className="surface p-6 text-center text-gray-600 dark:text-gray-300">
        <Banknote className="h-8 w-8 mx-auto mb-2 text-gray-400" />
        <p className="text-sm">
          {t('employeeDashboard.salary.error.noAccess')}
        </p>
      </div>
    )
  }

  return (
    <div className="space-y-5 text-gray-900 dark:text-gray-100">
      <div className="flex items-center gap-3">
        <Banknote className="h-8 w-8 text-primary-600 dark:text-primary-400" />
        <h1 className="text-2xl font-bold">{t('employeeDashboard.salary')}</h1>
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-[minmax(0,2fr)_minmax(320px,1fr)] gap-5">
        <SalarySummaryCard
          payment={data.latestPayment}
          isDownloading={isDownloading}
          canDownload={data.canDownloadPayslip}
          onDownload={downloadPayslip}
        />
        <BankAccountCard account={data.bankAccount} />
      </div>

      <div className="grid grid-cols-1 2xl:grid-cols-[minmax(0,1.7fr)_minmax(360px,0.9fr)] gap-5 items-start">
        <SalaryHistoryTable
          payments={data.paymentHistory}
          selectedPaymentId={selectedPayment.id}
          isDownloading={isDownloading}
          canDownload={data.canDownloadPayslip}
          onSelect={(payment) => setSelectedPaymentId(payment.id)}
          onDownload={downloadPayslip}
        />

        <div className="space-y-5">
          <SalaryDetailsCard payment={selectedPayment} />
          <PayslipDownloadCard
            payment={selectedPayment}
            isDownloading={isDownloading}
            canDownload={data.canDownloadPayslip}
            message={downloadMessage}
            onDownload={downloadPayslip}
            onClearMessage={clearDownloadMessage}
          />
        </div>
      </div>
    </div>
  )
}
