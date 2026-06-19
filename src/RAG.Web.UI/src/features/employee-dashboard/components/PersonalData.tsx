import React, { useState } from 'react'
import {
  User,
  Mail,
  Phone,
  MapPin,
  Briefcase,
  Building2,
  UserCheck,
  Calendar,
  Hash,
  Heart,
  Clock,
  FileEdit,
  Paperclip,
  CheckCircle,
  XCircle,
  AlertCircle,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import { Modal } from '@/shared/components/ui/Modal'
import { Button } from '@/shared/components/ui/Button'
import { Input } from '@/shared/components/ui/Input'
import { Textarea } from '@/shared/components/ui/Textarea'
import { usePersonalData } from '../hooks/usePersonalData'
import type {
  Address,
  ChangeRequestType,
  ChangeRequestStatus,
  DataChangeRequest,
  EmployeePersonalData,
} from '../types/personalData'

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('pl-PL', {
    day: '2-digit',
    month: 'long',
    year: 'numeric',
  })
}

function formatAddress(addr: Address): string {
  const apt = addr.apartmentNumber ? `/${addr.apartmentNumber}` : ''
  return `${addr.street} ${addr.buildingNumber}${apt}, ${addr.postalCode} ${addr.city}`
}

function computeSeniority(hireDateIso: string): string {
  const hire = new Date(hireDateIso)
  const now = new Date()
  let years = now.getFullYear() - hire.getFullYear()
  let months = now.getMonth() - hire.getMonth()
  if (months < 0) {
    years -= 1
    months += 12
  }
  const yearsPart = years > 0 ? `${years} ${years === 1 ? 'rok' : years < 5 ? 'lata' : 'lat'}` : ''
  const monthsPart = months > 0 ? `${months} ${months === 1 ? 'miesiąc' : months < 5 ? 'miesiące' : 'miesięcy'}` : ''
  return [yearsPart, monthsPart].filter(Boolean).join(' ') || '< 1 miesiąc'
}

// ---------------------------------------------------------------------------
// DataRow – reusable read-only field row (matches EmployeeProfileCard style)
// ---------------------------------------------------------------------------

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
        <p className="text-sm font-medium text-gray-900 dark:text-gray-100 break-words">{value}</p>
      </div>
    </div>
  )
}

// ---------------------------------------------------------------------------
// SectionCard
// ---------------------------------------------------------------------------

interface SectionCardProps {
  icon: React.ComponentType<{ className?: string }>
  title: string
  children: React.ReactNode
  iconBg?: string
  iconColor?: string
}

function SectionCard({ icon: Icon, title, children, iconBg = 'bg-primary-50 dark:bg-primary-900/20', iconColor = 'text-primary-600 dark:text-primary-400' }: SectionCardProps) {
  return (
    <div className="surface p-5 flex flex-col gap-1">
      <div className="flex items-center gap-2 mb-3">
        <div className={`p-2 rounded-lg ${iconBg}`}>
          <Icon className={`h-5 w-5 ${iconColor}`} />
        </div>
        <h2 className="font-semibold text-gray-900 dark:text-gray-100">{title}</h2>
      </div>
      {children}
    </div>
  )
}

// ---------------------------------------------------------------------------
// StatusBadge – for request history
// ---------------------------------------------------------------------------

function StatusBadge({ status }: { status: ChangeRequestStatus }) {
  const { t } = useI18n()

  if (status === 'approved') {
    return (
      <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400">
        <CheckCircle className="h-3 w-3" />
        {t('employeeDashboard.personal.history.status.approved')}
      </span>
    )
  }
  if (status === 'rejected') {
    return (
      <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400">
        <XCircle className="h-3 w-3" />
        {t('employeeDashboard.personal.history.status.rejected')}
      </span>
    )
  }
  return (
    <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400">
      <AlertCircle className="h-3 w-3" />
      {t('employeeDashboard.personal.history.status.pending')}
    </span>
  )
}

// ---------------------------------------------------------------------------
// ChangeRequestModal
// ---------------------------------------------------------------------------

const CHANGE_TYPES: ChangeRequestType[] = [
  'residenceAddress',
  'correspondenceAddress',
  'privatePhone',
  'privateEmail',
  'lastName',
  'emergencyContact',
  'other',
]

interface ChangeRequestModalProps {
  isOpen: boolean
  onClose: () => void
  onSubmitted: () => void
}

interface AddressFormFields {
  street: string
  buildingNumber: string
  apartmentNumber: string
  postalCode: string
  city: string
}

interface EmergencyContactFormFields {
  fullName: string
  relationship: string
  phone: string
}

interface ChangeFormState {
  changeType: ChangeRequestType | ''
  justification: string
  // dynamic fields
  newPhone: string
  newEmail: string
  newLastName: string
  address: AddressFormFields
  emergencyContact: EmergencyContactFormFields
  description: string
}

const INITIAL_FORM: ChangeFormState = {
  changeType: '',
  justification: '',
  newPhone: '',
  newEmail: '',
  newLastName: '',
  address: { street: '', buildingNumber: '', apartmentNumber: '', postalCode: '', city: '' },
  emergencyContact: { fullName: '', relationship: '', phone: '' },
  description: '',
}

function ChangeRequestModal({ isOpen, onClose, onSubmitted }: ChangeRequestModalProps) {
  const { t } = useI18n()
  const [form, setForm] = useState<ChangeFormState>(INITIAL_FORM)
  const [isSubmitting, setIsSubmitting] = useState(false)

  function handleClose() {
    setForm(INITIAL_FORM)
    onClose()
  }

  function setField<K extends keyof ChangeFormState>(key: K, value: ChangeFormState[K]) {
    setForm((prev) => ({ ...prev, [key]: value }))
  }

  function setAddressField(key: keyof AddressFormFields, value: string) {
    setForm((prev) => ({ ...prev, address: { ...prev.address, [key]: value } }))
  }

  function setEmergencyField(key: keyof EmergencyContactFormFields, value: string) {
    setForm((prev) => ({ ...prev, emergencyContact: { ...prev.emergencyContact, [key]: value } }))
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setIsSubmitting(true)
    // Simulates async submit; replace with real API call
    await new Promise((resolve) => setTimeout(resolve, 800))
    setIsSubmitting(false)
    setForm(INITIAL_FORM)
    onSubmitted()
  }

  const labelClass = 'block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1'

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title={t('employeeDashboard.personal.changeRequest.modalTitle')}
      size="md"
    >
      <form onSubmit={handleSubmit} className="p-6 space-y-5">
        {/* Change type */}
        <div>
          <label className={labelClass}>
            {t('employeeDashboard.personal.changeRequest.changeType')}
          </label>
          <select
            className="form-select"
            value={form.changeType}
            onChange={(e) => setField('changeType', e.target.value as ChangeRequestType | '')}
            required
          >
            <option value="" disabled>
              {t('employeeDashboard.personal.changeRequest.changeTypePlaceholder')}
            </option>
            {CHANGE_TYPES.map((type) => (
              <option key={type} value={type}>
                {t(`employeeDashboard.personal.changeType.${type}` as Parameters<typeof t>[0])}
              </option>
            ))}
          </select>
        </div>

        {/* Dynamic fields based on change type */}
        {form.changeType === 'privatePhone' && (
          <div>
            <label className={labelClass}>
              {t('employeeDashboard.personal.changeForm.newPhone')}
            </label>
            <Input
              type="tel"
              value={form.newPhone}
              onChange={(e) => setField('newPhone', e.target.value)}
              placeholder="+48 000 000 000"
              required
            />
          </div>
        )}

        {form.changeType === 'privateEmail' && (
          <div>
            <label className={labelClass}>
              {t('employeeDashboard.personal.changeForm.newEmail')}
            </label>
            <Input
              type="email"
              value={form.newEmail}
              onChange={(e) => setField('newEmail', e.target.value)}
              placeholder="email@example.com"
              required
            />
          </div>
        )}

        {form.changeType === 'lastName' && (
          <div>
            <label className={labelClass}>
              {t('employeeDashboard.personal.changeForm.newLastName')}
            </label>
            <Input
              type="text"
              value={form.newLastName}
              onChange={(e) => setField('newLastName', e.target.value)}
              required
            />
          </div>
        )}

        {(form.changeType === 'residenceAddress' || form.changeType === 'correspondenceAddress') && (
          <div className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div className="col-span-2">
                <label className={labelClass}>
                  {t('employeeDashboard.personal.changeForm.street')}
                </label>
                <Input
                  type="text"
                  value={form.address.street}
                  onChange={(e) => setAddressField('street', e.target.value)}
                  required
                />
              </div>
              <div>
                <label className={labelClass}>
                  {t('employeeDashboard.personal.changeForm.buildingNumber')}
                </label>
                <Input
                  type="text"
                  value={form.address.buildingNumber}
                  onChange={(e) => setAddressField('buildingNumber', e.target.value)}
                  required
                />
              </div>
              <div>
                <label className={labelClass}>
                  {t('employeeDashboard.personal.changeForm.apartmentNumber')}
                </label>
                <Input
                  type="text"
                  value={form.address.apartmentNumber}
                  onChange={(e) => setAddressField('apartmentNumber', e.target.value)}
                />
              </div>
              <div>
                <label className={labelClass}>
                  {t('employeeDashboard.personal.changeForm.postalCode')}
                </label>
                <Input
                  type="text"
                  value={form.address.postalCode}
                  onChange={(e) => setAddressField('postalCode', e.target.value)}
                  placeholder="00-000"
                  required
                />
              </div>
              <div>
                <label className={labelClass}>
                  {t('employeeDashboard.personal.changeForm.city')}
                </label>
                <Input
                  type="text"
                  value={form.address.city}
                  onChange={(e) => setAddressField('city', e.target.value)}
                  required
                />
              </div>
            </div>
          </div>
        )}

        {form.changeType === 'emergencyContact' && (
          <div className="space-y-3">
            <div>
              <label className={labelClass}>
                {t('employeeDashboard.personal.changeForm.contactFullName')}
              </label>
              <Input
                type="text"
                value={form.emergencyContact.fullName}
                onChange={(e) => setEmergencyField('fullName', e.target.value)}
                required
              />
            </div>
            <div>
              <label className={labelClass}>
                {t('employeeDashboard.personal.changeForm.contactRelationship')}
              </label>
              <Input
                type="text"
                value={form.emergencyContact.relationship}
                onChange={(e) => setEmergencyField('relationship', e.target.value)}
                required
              />
            </div>
            <div>
              <label className={labelClass}>
                {t('employeeDashboard.personal.changeForm.contactPhone')}
              </label>
              <Input
                type="tel"
                value={form.emergencyContact.phone}
                onChange={(e) => setEmergencyField('phone', e.target.value)}
                placeholder="+48 000 000 000"
                required
              />
            </div>
          </div>
        )}

        {form.changeType === 'other' && (
          <div>
            <label className={labelClass}>
              {t('employeeDashboard.personal.changeForm.description')}
            </label>
            <Textarea
              value={form.description}
              onChange={(e) => setField('description', e.target.value)}
              placeholder={t('employeeDashboard.personal.changeForm.descriptionPlaceholder')}
              rows={4}
              required
            />
          </div>
        )}

        {/* Justification – always shown when type is selected */}
        {form.changeType !== '' && (
          <div>
            <label className={labelClass}>
              {t('employeeDashboard.personal.changeRequest.justification')}
            </label>
            <Textarea
              value={form.justification}
              onChange={(e) => setField('justification', e.target.value)}
              placeholder={t('employeeDashboard.personal.changeRequest.justificationPlaceholder')}
              rows={3}
            />
          </div>
        )}

        {/* Attachment placeholder */}
        {form.changeType !== '' && (
          <div>
            <label className={labelClass}>
              {t('employeeDashboard.personal.changeRequest.attachment')}
            </label>
            <div className="flex items-center gap-3 px-4 py-3 rounded-xl border border-dashed border-gray-300 dark:border-slate-700 bg-gray-50 dark:bg-slate-800/50 text-sm text-gray-400 dark:text-gray-500 cursor-not-allowed select-none">
              <Paperclip className="h-4 w-4 flex-shrink-0" />
              <span>{t('employeeDashboard.personal.changeRequest.attachmentPlaceholder')}</span>
            </div>
          </div>
        )}

        {/* Actions */}
        <div className="flex justify-end gap-3 pt-2">
          <Button type="button" variant="outline" onClick={handleClose} disabled={isSubmitting}>
            {t('employeeDashboard.personal.changeRequest.cancel')}
          </Button>
          <Button type="submit" disabled={isSubmitting || form.changeType === ''}>
            {isSubmitting
              ? t('employeeDashboard.personal.changeRequest.submitting')
              : t('employeeDashboard.personal.changeRequest.submit')}
          </Button>
        </div>
      </form>
    </Modal>
  )
}

// ---------------------------------------------------------------------------
// RequestHistoryTable
// ---------------------------------------------------------------------------

interface RequestHistoryTableProps {
  requests: DataChangeRequest[]
}

function RequestHistoryTable({ requests }: RequestHistoryTableProps) {
  const { t } = useI18n()

  return (
    <div className="surface p-5">
      <div className="flex items-center gap-2 mb-4">
        <div className="p-2 bg-gray-100 dark:bg-slate-800 rounded-lg">
          <Clock className="h-5 w-5 text-gray-600 dark:text-gray-400" />
        </div>
        <h2 className="font-semibold text-gray-900 dark:text-gray-100">
          {t('employeeDashboard.personal.history.title')}
        </h2>
      </div>

      {requests.length === 0 ? (
        <p className="text-sm text-gray-500 dark:text-gray-400 py-4 text-center">
          {t('employeeDashboard.personal.history.empty')}
        </p>
      ) : (
        <div className="overflow-x-auto -mx-5">
          <table className="w-full text-sm min-w-[560px]">
            <thead>
              <tr className="border-b border-gray-100 dark:border-slate-800">
                <th className="px-5 py-2.5 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                  {t('employeeDashboard.personal.history.date')}
                </th>
                <th className="px-5 py-2.5 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                  {t('employeeDashboard.personal.history.changeType')}
                </th>
                <th className="px-5 py-2.5 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                  {t('employeeDashboard.personal.history.status')}
                </th>
                <th className="px-5 py-2.5 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                  {t('employeeDashboard.personal.history.comment')}
                </th>
              </tr>
            </thead>
            <tbody>
              {requests.map((req) => (
                <tr
                  key={req.id}
                  className="border-b border-gray-50 dark:border-slate-800/60 last:border-0 hover:bg-gray-50/60 dark:hover:bg-slate-800/30 transition-colors"
                >
                  <td className="px-5 py-3 text-gray-700 dark:text-gray-300 whitespace-nowrap">
                    {new Date(req.date).toLocaleDateString('pl-PL', {
                      day: '2-digit',
                      month: '2-digit',
                      year: 'numeric',
                    })}
                  </td>
                  <td className="px-5 py-3 text-gray-700 dark:text-gray-300">
                    {t(`employeeDashboard.personal.changeType.${req.changeType}` as Parameters<typeof t>[0])}
                  </td>
                  <td className="px-5 py-3">
                    <StatusBadge status={req.status} />
                  </td>
                  <td className="px-5 py-3 text-gray-600 dark:text-gray-400 max-w-xs">
                    {req.comment ?? '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

// ---------------------------------------------------------------------------
// Section components
// ---------------------------------------------------------------------------

function BasicInfoSection({ data }: { data: EmployeePersonalData['basicInfo'] }) {
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

function ContactInfoSection({ data }: { data: EmployeePersonalData['contactInfo'] }) {
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

function EmploymentInfoSection({ employments }: { employments: EmployeePersonalData['employments'] }) {
  const { t } = useI18n()
  const [activeIndex, setActiveIndex] = useState(0)

  const total = employments.length
  const data = employments[activeIndex]

  return (
    <div className="surface p-5 flex flex-col gap-1">
      {/* Header row with switcher */}
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
              aria-label="Następne zatrudnienie"
            >
              <ChevronRight className="h-4 w-4 text-gray-600 dark:text-gray-400" />
            </button>
          </div>
        )}
      </div>

      {/* Company tab pills – clickable when more than one */}
      {total > 1 && (
        <div className="flex flex-wrap gap-2 mb-3">
          {employments.map((emp, idx) => (
            <button
              key={emp.id}
              onClick={() => setActiveIndex(idx)}
              className={`px-3 py-1 rounded-full text-xs font-medium border transition-colors ${
                idx === activeIndex
                  ? 'bg-purple-100 text-purple-700 border-purple-300 dark:bg-purple-900/40 dark:text-purple-300 dark:border-purple-600'
                  : 'bg-gray-100 text-gray-600 border-gray-200 hover:bg-gray-200 dark:bg-slate-800 dark:text-gray-400 dark:border-slate-700 dark:hover:bg-slate-700'
              }`}
            >
              {emp.company}
            </button>
          ))}
        </div>
      )}

      {/* Data rows for active employment */}
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

function EmergencyContactSection({ data }: { data: EmployeePersonalData['emergencyContact'] }) {
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

// ---------------------------------------------------------------------------
// SuccessBanner
// ---------------------------------------------------------------------------

function SuccessBanner({ message, onDismiss }: { message: string; onDismiss: () => void }) {
  return (
    <div className="flex items-start gap-3 px-4 py-3 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-xl text-sm text-green-800 dark:text-green-300">
      <CheckCircle className="h-5 w-5 flex-shrink-0 mt-0.5" />
      <span className="flex-1">{message}</span>
      <button
        onClick={onDismiss}
        className="text-green-600 dark:text-green-400 hover:text-green-800 dark:hover:text-green-200 transition-colors flex-shrink-0"
        aria-label="Dismiss"
      >
        <XCircle className="h-4 w-4" />
      </button>
    </div>
  )
}

// ---------------------------------------------------------------------------
// PersonalData – page root
// ---------------------------------------------------------------------------

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
      {/* Page header */}
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

      {/* Success banner */}
      {showSuccess && (
        <SuccessBanner
          message={t('employeeDashboard.personal.changeRequest.successMessage')}
          onDismiss={() => setShowSuccess(false)}
        />
      )}

      {/* Section 1 + 2 – Basic and Contact */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <BasicInfoSection data={personalData.basicInfo} />
        <ContactInfoSection data={personalData.contactInfo} />
      </div>

      {/* Section 3 + 4 – Employment and Emergency */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <EmploymentInfoSection employments={personalData.employments} />
        <EmergencyContactSection data={personalData.emergencyContact} />
      </div>

      {/* Request history */}
      <RequestHistoryTable requests={changeRequests} />

      {/* Change request modal */}
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
