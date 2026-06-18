import React from 'react'
import { useI18n } from '@/shared/contexts/I18nContext'
import { useAuth } from '@/shared/contexts/AuthContext'
import { BarChart3 } from 'lucide-react'

export function EmployeeDashboard() {
  const { t } = useI18n()
  const { user } = useAuth()

  return (
    <div className="space-y-6 text-gray-900 dark:text-gray-100">
      <div className="flex items-center gap-3">
        <BarChart3 className="h-8 w-8 text-primary-600 dark:text-primary-400" />
        <div>
          <h1 className="text-2xl font-bold">{t('employeeDashboard.dashboard')}</h1>
          {user && (
            <p className="text-gray-600 dark:text-gray-400 mt-0.5 text-sm">
              {user.firstName} {user.lastName}
            </p>
          )}
        </div>
      </div>
    </div>
  )
}

