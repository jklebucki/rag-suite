import React from 'react'
import { User, Mail, Phone, Calendar, Building2, Briefcase, UserCheck } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { EmployeeProfile } from '../../types/employeeDashboard'

interface Props {
  profile: EmployeeProfile
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    day: '2-digit',
    month: 'long',
    year: 'numeric',
  })
}

interface DataRowProps {
  icon: React.ComponentType<{ className?: string }>
  label: string
  value: string
}

function DataRow({ icon: Icon, label, value }: DataRowProps) {
  return (
    <div className="flex items-start gap-3 py-2.5 border-b border-gray-100 dark:border-slate-800 last:border-0">
      <div className="p-1.5 bg-primary-50 dark:bg-primary-900/20 rounded-lg flex-shrink-0 mt-0.5">
        <Icon className="h-4 w-4 text-primary-600 dark:text-primary-400" />
      </div>
      <div className="flex-1 min-w-0">
        <p className="text-xs text-gray-500 dark:text-gray-500">{label}</p>
        <p className="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">{value}</p>
      </div>
    </div>
  )
}

export function EmployeeProfileCard({ profile }: Props) {
  const { t } = useI18n()

  return (
    <div className="surface p-5 flex flex-col gap-1">
      <div className="flex items-center gap-2 mb-3">
        <div className="p-2 bg-primary-50 dark:bg-primary-900/20 rounded-lg">
          <User className="h-5 w-5 text-primary-600 dark:text-primary-400" />
        </div>
        <h2 className="font-semibold text-gray-900 dark:text-gray-100">
          {t('employeeDashboard.profile.title')}
        </h2>
      </div>

      <DataRow
        icon={User}
        label={t('employeeDashboard.profile.fullName')}
        value={profile.fullName}
      />
      <DataRow
        icon={Briefcase}
        label={t('employeeDashboard.profile.position')}
        value={profile.position}
      />
      <DataRow
        icon={Building2}
        label={t('employeeDashboard.profile.department')}
        value={profile.department}
      />
      <DataRow
        icon={UserCheck}
        label={t('employeeDashboard.profile.supervisor')}
        value={profile.supervisor}
      />
      <DataRow
        icon={Calendar}
        label={t('employeeDashboard.profile.hireDate')}
        value={formatDate(profile.hireDate)}
      />
      <DataRow
        icon={Phone}
        label={t('employeeDashboard.profile.phone')}
        value={profile.phone}
      />
      <DataRow
        icon={Mail}
        label={t('employeeDashboard.profile.email')}
        value={profile.email}
      />
    </div>
  )
}
