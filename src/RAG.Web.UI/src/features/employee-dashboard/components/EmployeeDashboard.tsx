import React from 'react'
import { useI18n } from '@/shared/contexts/I18nContext'
import { useAuth } from '@/shared/contexts/AuthContext'

export function EmployeeDashboard() {
  const { t } = useI18n()
  const { user } = useAuth()

  return (
    <div className="space-y-6 text-gray-900 dark:text-gray-100">
      <div>
        <h1 className="text-3xl font-bold">{t('nav.employeeDashboard')}</h1>
        {user && (
          <p className="text-gray-600 dark:text-gray-400 mt-1">
            {user.firstName} {user.lastName}
          </p>
        )}
      </div>
    </div>
  )
}
