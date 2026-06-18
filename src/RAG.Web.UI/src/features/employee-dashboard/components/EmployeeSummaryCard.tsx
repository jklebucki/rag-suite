import React from 'react'
import { User, Clock, Building2, Briefcase } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { EmployeeProfile } from '../types/employeeDashboard'

interface Props {
  profile: EmployeeProfile
}

function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString(undefined, {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function AvatarPlaceholder({ fullName }: { fullName: string }) {
  const initials = fullName
    .split(' ')
    .slice(0, 2)
    .map((w) => w[0])
    .join('')
    .toUpperCase()

  return (
    <div className="h-16 w-16 rounded-full bg-primary-100 dark:bg-primary-900/40 flex items-center justify-center flex-shrink-0 ring-2 ring-primary-200 dark:ring-primary-700">
      <span className="text-xl font-bold text-primary-700 dark:text-primary-300">
        {initials}
      </span>
    </div>
  )
}

export function EmployeeSummaryCard({ profile }: Props) {
  const { t } = useI18n()

  return (
    <div className="surface p-5 flex flex-col sm:flex-row items-start sm:items-center gap-4">
      {profile.avatarUrl ? (
        <img
          src={profile.avatarUrl}
          alt={profile.fullName}
          className="h-16 w-16 rounded-full object-cover flex-shrink-0 ring-2 ring-primary-200 dark:ring-primary-700"
        />
      ) : (
        <AvatarPlaceholder fullName={profile.fullName} />
      )}

      <div className="flex-1 min-w-0">
        <h1 className="text-xl font-bold text-gray-900 dark:text-gray-100 truncate">
          {profile.fullName}
        </h1>

        <div className="mt-1.5 flex flex-wrap gap-x-4 gap-y-1 text-sm text-gray-600 dark:text-gray-400">
          <span className="flex items-center gap-1.5">
            <Briefcase className="h-4 w-4 text-primary-500 flex-shrink-0" />
            {profile.position}
          </span>
          <span className="flex items-center gap-1.5">
            <Building2 className="h-4 w-4 text-primary-500 flex-shrink-0" />
            {profile.department}
          </span>
        </div>
      </div>

      {profile.lastLoginAt && (
        <div className="text-xs text-gray-500 dark:text-gray-500 flex items-center gap-1.5 flex-shrink-0 sm:text-right">
          <Clock className="h-3.5 w-3.5" />
          <span>
            {t('employeeDashboard.overview.lastLogin')}:{' '}
            {formatDateTime(profile.lastLoginAt)}
          </span>
        </div>
      )}
    </div>
  )
}
