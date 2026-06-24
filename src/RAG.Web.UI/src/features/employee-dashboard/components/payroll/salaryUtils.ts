import type { SalaryPaymentType, SalaryStatus } from './salaryTypes'
import type { TranslationKeys } from '@/shared/types/i18n'

type Translate = (key: keyof TranslationKeys) => string

export function formatSalaryMoney(
  amount: number,
  currency: string,
  locale?: string
): string {
  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amount)
}

export function formatSalaryDate(iso: string, locale?: string): string {
  return new Date(iso).toLocaleDateString(locale, {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  })
}

export function salaryStatusLabel(status: SalaryStatus, t: Translate): string {
  const labels: Record<SalaryStatus, keyof TranslationKeys> = {
    paid: 'employeeDashboard.salary.status.paid',
    pending: 'employeeDashboard.salary.status.pending',
    cancelled: 'employeeDashboard.salary.status.cancelled',
    correction: 'employeeDashboard.salary.status.correction',
  }

  return t(labels[status])
}

export function salaryPaymentTypeLabel(type: SalaryPaymentType, t: Translate): string {
  const labels: Record<SalaryPaymentType, keyof TranslationKeys> = {
    base_salary: 'employeeDashboard.salary.type.baseSalary',
    bonus: 'employeeDashboard.salary.type.bonus',
    correction: 'employeeDashboard.salary.type.correction',
    annual_bonus: 'employeeDashboard.salary.type.annualBonus',
  }

  return t(labels[type])
}
