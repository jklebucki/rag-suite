import { useState } from 'react'
import { FileEdit, User } from 'lucide-react'
import { Button } from '@/shared/components/ui/Button'
import { useI18n } from '@/shared/contexts/I18nContext'
import { usePersonalData } from '../hooks/usePersonalData'
import { ChangeRequestModal } from './ChangeRequestModal'
import {
  BasicInfoSection,
  ContactInfoSection,
  EmergencyContactSection,
  EmploymentInfoSection,
} from './PersonalDataSections'
import { RequestHistoryTable } from './RequestHistoryTable'
import { SuccessBanner } from './SuccessBanner'

export function PersonalData() {
  const { t } = useI18n()
  const { data, isLoading, error } = usePersonalData()
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [showSuccess, setShowSuccess] = useState(false)

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

  const { personalData, changeRequests } = data

  return (
    <div className="space-y-4 text-gray-900 dark:text-gray-100">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div className="flex items-center gap-3">
          <User className="h-7 w-7 text-primary-600 dark:text-primary-400" />
          <h1 className="text-xl font-bold text-gray-900 dark:text-gray-100">
            {t('employeeDashboard.personalData')}
          </h1>
        </div>
        <Button
          variant="primary"
          size="md"
          onClick={() => setIsModalOpen(true)}
          className="flex items-center gap-2 sm:ml-auto"
        >
          <FileEdit className="h-4 w-4" />
          {t('employeeDashboard.personal.changeRequest.buttonLabel')}
        </Button>
      </div>

      {showSuccess && (
        <SuccessBanner
          message={t('employeeDashboard.personal.changeRequest.successMessage')}
          onDismiss={() => setShowSuccess(false)}
        />
      )}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <BasicInfoSection data={personalData.basicInfo} />
        <ContactInfoSection data={personalData.contactInfo} />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <EmploymentInfoSection employments={personalData.employments} />
        <EmergencyContactSection data={personalData.emergencyContact} />
      </div>

      <RequestHistoryTable requests={changeRequests} />

      <ChangeRequestModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSubmitted={() => {
          setIsModalOpen(false)
          setShowSuccess(true)
        }}
      />
    </div>
  )
}
