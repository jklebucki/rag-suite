import type { TranslationKeys } from '@/shared/types/i18n'
import type { EmploymentContextStatus } from '../types/employmentContextTypes'

type Translate = (key: keyof TranslationKeys, params?: Record<string, string> | string[]) => string

export function formatEmploymentDate(value: string, locale = 'pl-PL'): string {
  return new Intl.DateTimeFormat(locale, {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(new Date(value))
}

export function employmentStatusLabel(status: EmploymentContextStatus, t: Translate): string {
  const labels: Record<EmploymentContextStatus, keyof TranslationKeys> = {
    active: 'employeeDashboard.employmentContext.status.active',
    ended: 'employeeDashboard.employmentContext.status.ended',
    suspended: 'employeeDashboard.employmentContext.status.suspended',
  }

  return t(labels[status])
}
