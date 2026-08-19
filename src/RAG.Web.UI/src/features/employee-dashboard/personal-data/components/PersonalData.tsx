import { User } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import { usePersonalData } from '../hooks/usePersonalData'
import {
  BasicInfoSection,
  ContactInfoSection,
  EmergencyContactSection,
  EmploymentInfoSection,
} from './PersonalDataSections'

export function PersonalData() {
  const { t } = useI18n()
  const { data, isLoading, error } = usePersonalData()

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64 text-gray-600 dark:text-gray-400">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600" />
      </div>
    )
  }

  if (error || !data) {
    return (
      <div className="text-center py-12 text-gray-600 dark:text-gray-400">
        <p>{t('common.error')}</p>
      </div>
    )
  }

  const { personalData } = data

  return (
    <div className="space-y-4 text-gray-900 dark:text-gray-100">
      <div className="flex items-center gap-3">
        <User className="h-7 w-7 text-primary-600 dark:text-primary-400" />
        <h1 className="text-xl font-bold text-gray-900 dark:text-gray-100">
          {t('employeeDashboard.personalData')}
        </h1>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <BasicInfoSection data={personalData.basicInfo} />
        <ContactInfoSection data={personalData.contactInfo} />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <EmploymentInfoSection employments={personalData.employments} />
        <EmergencyContactSection data={personalData.emergencyContact} />
      </div>
    </div>
  )
}
