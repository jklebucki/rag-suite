import { Download, FileText, Info } from 'lucide-react'
import { Button } from '@/shared/components/ui'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { TranslationKeys } from '@/shared/types/i18n'
import type { SalaryPayment } from '../types/salaryTypes'
import { formatSalaryMoney } from '../services/salaryUtils'

interface PayslipDownloadCardProps {
  payment: SalaryPayment
  isDownloading: boolean
  canDownload: boolean
  message: string | null
  onDownload: (paymentId: string) => void
  onClearMessage: () => void
}

export function PayslipDownloadCard({
  payment,
  isDownloading,
  canDownload,
  message,
  onDownload,
  onClearMessage,
}: PayslipDownloadCardProps) {
  const { language, t } = useI18n()

  return (
    <div className="surface p-5 flex flex-col gap-4">
      <div className="flex items-center gap-3">
        <div className="p-2 bg-emerald-50 dark:bg-emerald-900/20 rounded-lg">
          <FileText className="h-5 w-5 text-emerald-600 dark:text-emerald-400" />
        </div>
        <div>
          <h2 className="font-semibold text-gray-900 dark:text-gray-100">
            {t('employeeDashboard.salary.pdf.title')}
          </h2>
          <p className="text-xs text-gray-500 dark:text-gray-400">
            {payment.periodLabel} - {formatSalaryMoney(payment.netAmount, payment.currency, language)} {t('employeeDashboard.salary.netShort')}
          </p>
        </div>
      </div>

      <Button
        variant="outline"
        size="sm"
        className="w-full gap-2"
        disabled={!canDownload || isDownloading || payment.status === 'pending'}
        onClick={() => onDownload(payment.id)}
      >
        <Download className="h-4 w-4" />
        {t('employeeDashboard.salary.pdf.download')}
      </Button>

      {message && (
        <div className="surface-muted p-3 border-blue-200 dark:border-blue-900/40">
          <div className="flex items-start gap-2">
            <Info className="h-4 w-4 mt-0.5 text-blue-600 dark:text-blue-400" />
            <div className="min-w-0">
              <p className="text-xs text-blue-700 dark:text-blue-300">
                {t(message as keyof TranslationKeys)}
              </p>
              <button
                type="button"
                onClick={onClearMessage}
                className="mt-1 text-xs font-medium text-blue-700 dark:text-blue-300 hover:underline"
              >
                {t('employeeDashboard.salary.pdf.closeMessage')}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
