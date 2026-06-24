import { Building2, CalendarClock, CreditCard, ShieldCheck } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { SalaryBankAccount } from '../types/salaryTypes'
import { formatSalaryDate } from '../services/salaryUtils'

interface BankAccountCardProps {
  account: SalaryBankAccount
}

export function BankAccountCard({ account }: BankAccountCardProps) {
  const { language, t } = useI18n()
  const rows = [
    {
      icon: Building2,
      label: t('employeeDashboard.salary.bank.bankName'),
      value: account.bankName,
    },
    {
      icon: CreditCard,
      label: t('employeeDashboard.salary.bank.accountNumber'),
      value: account.maskedAccountNumber,
      mono: true,
    },
    {
      icon: ShieldCheck,
      label: t('employeeDashboard.salary.bank.payoutMethod'),
      value: account.payoutMethod,
    },
    {
      icon: CalendarClock,
      label: t('employeeDashboard.salary.bank.lastUpdated'),
      value: formatSalaryDate(account.lastUpdatedAt, language),
      mono: true,
    },
  ]

  return (
    <div className="surface p-5 flex flex-col gap-4">
      <div className="flex items-center gap-3">
        <div className="p-2 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
          <CreditCard className="h-5 w-5 text-blue-600 dark:text-blue-400" />
        </div>
        <div>
          <h2 className="font-semibold text-gray-900 dark:text-gray-100">
            {t('employeeDashboard.salary.bank.title')}
          </h2>
          <p className="text-xs text-gray-500 dark:text-gray-400">
            {t('employeeDashboard.salary.readOnly')}
          </p>
        </div>
      </div>

      <div className="space-y-3">
        {rows.map((row) => {
          const Icon = row.icon
          return (
            <div key={row.label} className="flex items-start gap-3">
              <Icon className="h-4 w-4 mt-0.5 text-gray-400" />
              <div className="min-w-0">
                <p className="text-xs text-gray-500 dark:text-gray-400">{row.label}</p>
                <p
                  className={`text-sm font-medium text-gray-900 dark:text-gray-100 break-words ${
                    row.mono ? 'tabular-nums' : ''
                  }`}
                >
                  {row.value}
                </p>
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
