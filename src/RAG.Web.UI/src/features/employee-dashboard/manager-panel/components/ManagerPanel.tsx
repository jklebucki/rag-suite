import React from 'react'
import { useI18n } from '@/shared/contexts/I18nContext'
import { Briefcase } from 'lucide-react'

export function ManagerPanel() {
  const { t } = useI18n()

  return (
    <div className="space-y-6 text-gray-900 dark:text-gray-100">
      <div className="flex items-center gap-3">
        <Briefcase className="h-8 w-8 text-primary-600 dark:text-primary-400" />
        <h1 className="text-2xl font-bold">{t('employeeDashboard.managerPanel')}</h1>
      </div>
    </div>
  )
}
