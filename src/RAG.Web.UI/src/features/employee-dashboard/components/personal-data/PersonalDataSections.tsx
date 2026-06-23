import { useState } from 'react'
import {
  Briefcase,
  Building2,
  Calendar,
  CheckCircle,
  ChevronLeft,
  ChevronRight,
  Clock,
  Hash,
  Heart,
  Mail,
  MapPin,
  Phone,
  User,
  UserCheck,
} from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { EmployeePersonalData } from '../../types/personalData'
import { DataRow } from './DataRow'
import { SectionCard } from './SectionCard'
import { computeSeniority, formatAddress, formatDate } from './personalDataUtils'

export function BasicInfoSection({ data }: { data: EmployeePersonalData['basicInfo'] }) {
  const { t } = useI18n()

  return (
    <SectionCard icon={User} title={t('employeeDashboard.personal.basicInfo.title')}>
      <DataRow icon={User} label={t('employeeDashboard.personal.basicInfo.firstName')} value={data.firstName} />
      <DataRow icon={User} label={t('employeeDashboard.personal.basicInfo.lastName')} value={data.lastName} />
      <DataRow icon={Hash} label={t('employeeDashboard.personal.basicInfo.employeeCode')} value={data.employeeCode} />
      <DataRow icon={Calendar} label={t('employeeDashboard.personal.basicInfo.birthDate')} value={formatDate(data.birthDate)} />
      <DataRow icon={Briefcase} label={t('employeeDashboard.personal.basicInfo.position')} value={data.position} />
      <DataRow icon={Building2} label={t('employeeDashboard.personal.basicInfo.department')} value={data.department} />
      <DataRow icon={UserCheck} label={t('employeeDashboard.personal.basicInfo.supervisor')} value={data.supervisor} />
      <DataRow icon={Calendar} label={t('employeeDashboard.personal.basicInfo.hireDate')} value={formatDate(data.hireDate)} />
    </SectionCard>
  )
}

export function ContactInfoSection({ data }: { data: EmployeePersonalData['contactInfo'] }) {
  const { t } = useI18n()

  return (
    <SectionCard
      icon={Mail}
      title={t('employeeDashboard.personal.contactInfo.title')}
      iconBg="bg-blue-50 dark:bg-blue-900/20"
      iconColor="text-blue-600 dark:text-blue-400"
    >
      <DataRow icon={Mail} label={t('employeeDashboard.personal.contactInfo.workEmail')} value={data.workEmail} />
      <DataRow icon={Mail} label={t('employeeDashboard.personal.contactInfo.privateEmail')} value={data.privateEmail} />
      <DataRow icon={Phone} label={t('employeeDashboard.personal.contactInfo.workPhone')} value={data.workPhone} />
      <DataRow icon={Phone} label={t('employeeDashboard.personal.contactInfo.privatePhone')} value={data.privatePhone} />
      <DataRow icon={MapPin} label={t('employeeDashboard.personal.contactInfo.residenceAddress')} value={formatAddress(data.residenceAddress)} />
      <DataRow icon={MapPin} label={t('employeeDashboard.personal.contactInfo.correspondenceAddress')} value={formatAddress(data.correspondenceAddress)} />
    </SectionCard>
  )
}

export function EmploymentInfoSection({
  employments,
}: {
  employments: EmployeePersonalData['employments']
}) {
  const { t } = useI18n()
  const [activeIndex, setActiveIndex] = useState(0)

  const total = employments.length
  const data = employments[activeIndex]

  return (
    <div className="surface p-5 flex flex-col gap-1">
      <div className="flex items-center gap-2 mb-3">
        <div className="p-2 bg-purple-50 dark:bg-purple-900/20 rounded-lg">
          <Briefcase className="h-5 w-5 text-purple-600 dark:text-purple-400" />
        </div>
        <h2 className="font-semibold text-gray-900 dark:text-gray-100 flex-1">
          {t('employeeDashboard.personal.employmentInfo.title')}
        </h2>

        {total > 1 && (
          <div className="flex items-center gap-1.5 ml-auto">
            <button
              onClick={() => setActiveIndex((i) => Math.max(0, i - 1))}
              disabled={activeIndex === 0}
              className="p-1 rounded-lg hover:bg-gray-100 dark:hover:bg-slate-800 disabled:opacity-30 disabled:pointer-events-none transition-colors"
              aria-label="Poprzednie zatrudnienie"
            >
              <ChevronLeft className="h-4 w-4 text-gray-600 dark:text-gray-400" />
            </button>

            <span className="text-xs font-medium text-gray-500 dark:text-gray-400 tabular-nums min-w-[2.5rem] text-center">
              {activeIndex + 1} / {total}
            </span>

            <button
              onClick={() => setActiveIndex((i) => Math.min(total - 1, i + 1))}
              disabled={activeIndex === total - 1}
              className="p-1 rounded-lg hover:bg-gray-100 dark:hover:bg-slate-800 disabled:opacity-30 disabled:pointer-events-none transition-colors"
              aria-label="Nastepne zatrudnienie"
            >
              <ChevronRight className="h-4 w-4 text-gray-600 dark:text-gray-400" />
            </button>
          </div>
        )}
      </div>

      {total > 1 && (
        <div className="flex flex-wrap gap-2 mb-3">
          {employments.map((employment, index) => (
            <button
              key={employment.id}
              onClick={() => setActiveIndex(index)}
              className={`px-3 py-1 rounded-full text-xs font-medium border transition-colors ${
                index === activeIndex
                  ? 'bg-purple-100 text-purple-700 border-purple-300 dark:bg-purple-900/40 dark:text-purple-300 dark:border-purple-600'
                  : 'bg-gray-100 text-gray-600 border-gray-200 hover:bg-gray-200 dark:bg-slate-800 dark:text-gray-400 dark:border-slate-700 dark:hover:bg-slate-700'
              }`}
            >
              {employment.company}
            </button>
          ))}
        </div>
      )}

      <DataRow icon={Building2} label={t('employeeDashboard.personal.employmentInfo.company')} value={data.company} />
      <DataRow icon={Building2} label={t('employeeDashboard.personal.employmentInfo.organizationalUnit')} value={data.organizationalUnit} />
      <DataRow icon={Hash} label={t('employeeDashboard.personal.employmentInfo.costCenter')} value={data.costCenter} />
      <DataRow icon={Briefcase} label={t('employeeDashboard.personal.employmentInfo.contractType')} value={data.contractType} />
      <DataRow icon={Clock} label={t('employeeDashboard.personal.employmentInfo.workTimeFraction')} value={data.workTimeFraction} />
      <DataRow icon={CheckCircle} label={t('employeeDashboard.personal.employmentInfo.employmentStatus')} value={data.employmentStatus} />
      <DataRow icon={Calendar} label={t('employeeDashboard.personal.employmentInfo.hireDate')} value={formatDate(data.hireDate)} />
      <DataRow icon={Clock} label={t('employeeDashboard.personal.employmentInfo.seniority')} value={computeSeniority(data.hireDate)} />
    </div>
  )
}

export function EmergencyContactSection({
  data,
}: {
  data: EmployeePersonalData['emergencyContact']
}) {
  const { t } = useI18n()

  return (
    <SectionCard
      icon={Heart}
      title={t('employeeDashboard.personal.emergencyContact.title')}
      iconBg="bg-rose-50 dark:bg-rose-900/20"
      iconColor="text-rose-600 dark:text-rose-400"
    >
      <DataRow icon={User} label={t('employeeDashboard.personal.emergencyContact.fullName')} value={data.fullName} />
      <DataRow icon={Heart} label={t('employeeDashboard.personal.emergencyContact.relationship')} value={data.relationship} />
      <DataRow icon={Phone} label={t('employeeDashboard.personal.emergencyContact.phone')} value={data.phone} />
    </SectionCard>
  )
}
