import { useState } from 'react'
import type { FormEvent } from 'react'
import { Paperclip } from 'lucide-react'
import { Button } from '@/shared/components/ui/Button'
import { Input } from '@/shared/components/ui/Input'
import { Modal } from '@/shared/components/ui/Modal'
import { Textarea } from '@/shared/components/ui/Textarea'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { ChangeRequestType } from '../../types/personalData'

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

export function ChangeRequestModal({ isOpen, onClose, onSubmitted }: ChangeRequestModalProps) {
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

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setIsSubmitting(true)
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
