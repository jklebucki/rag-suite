import { useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { CheckCircle, Clock, FileText, Info, Paperclip, Send, X } from 'lucide-react'
import { Button } from '@/shared/components/ui/Button'
import { Input } from '@/shared/components/ui/Input'
import { Textarea } from '@/shared/components/ui/Textarea'
import { useI18n } from '@/shared/contexts/I18nContext'
import type {
  CreateLeaveRequestPayload,
  LeaveCompany,
  LeaveSubstitute,
  LeaveType,
} from '../types/leaveRequest'
import { DatePickerInput } from './DatePickerInput'
import { countWorkingDays, leaveTypeLabel } from './leaveRequestUtils'

const LEAVE_TYPES: LeaveType[] = [
  'annual',
  'onDemand',
  'occasional',
  'childCare',
  'homeOffice',
  'delegation',
]

interface FormState {
  companyId: string
  leaveType: LeaveType | ''
  dateFrom: string
  dateTo: string
  substituteId: string
  comment: string
}

const INITIAL_FORM: FormState = {
  companyId: '',
  leaveType: '',
  dateFrom: '',
  dateTo: '',
  substituteId: '',
  comment: '',
}

interface LeaveRequestFormProps {
  companies: LeaveCompany[]
  substitutes: LeaveSubstitute[]
  isSubmitting: boolean
  onSubmit: (payload: CreateLeaveRequestPayload) => Promise<void>
}

export function LeaveRequestForm({
  companies,
  substitutes,
  isSubmitting,
  onSubmit,
}: LeaveRequestFormProps) {
  const { t } = useI18n()
  const defaultCompanyId = companies.length === 1 ? companies[0].id : ''
  const createInitialForm = () => ({ ...INITIAL_FORM, companyId: defaultCompanyId })

  const [form, setForm] = useState<FormState>(() => createInitialForm())
  const [submitted, setSubmitted] = useState(false)
  const [errors, setErrors] = useState<Partial<Record<keyof FormState, string>>>({})
  const selectedCompany = companies.find((company) => company.id === form.companyId)
  const shouldSelectCompany = companies.length > 1

  const daysCount = useMemo(
    () => countWorkingDays(form.dateFrom, form.dateTo),
    [form.dateFrom, form.dateTo]
  )

  function setField<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm((prev) => ({ ...prev, [key]: value }))
    if (errors[key]) setErrors((prev) => ({ ...prev, [key]: undefined }))
  }

  function validate(): boolean {
    const next: Partial<Record<keyof FormState, string>> = {}
    if (companies.length > 0 && !form.companyId)
      next.companyId = t('employeeDashboard.leave.form.errors.companyRequired')
    if (!form.leaveType)
      next.leaveType = t('employeeDashboard.leave.form.errors.leaveTypeRequired')
    if (!form.dateFrom)
      next.dateFrom = t('employeeDashboard.leave.form.errors.dateFromRequired')
    if (!form.dateTo)
      next.dateTo = t('employeeDashboard.leave.form.errors.dateToRequired')
    if (form.dateFrom && form.dateTo && form.dateTo < form.dateFrom)
      next.dateTo = t('employeeDashboard.leave.form.errors.dateToBeforeDateFrom')
    setErrors(next)
    return Object.keys(next).length === 0
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!validate()) return
    await onSubmit({
      companyId: form.companyId,
      leaveType: form.leaveType as LeaveType,
      dateFrom: form.dateFrom,
      dateTo: form.dateTo,
      daysCount,
      substituteId: form.substituteId || undefined,
      comment: form.comment || undefined,
    })
    setForm(createInitialForm())
    setErrors({})
    setSubmitted(true)
    setTimeout(() => setSubmitted(false), 4000)
  }

  function handleCancel() {
    setForm(createInitialForm())
    setErrors({})
    setSubmitted(false)
  }

  const labelClass = 'block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5'
  const errorClass = 'mt-1 text-xs text-red-600 dark:text-red-400'

  return (
    <div className="surface p-5">
      <div className="flex items-center gap-2 mb-5">
        <div className="p-2 bg-primary-50 dark:bg-primary-900/20 rounded-lg">
          <FileText className="h-5 w-5 text-primary-600 dark:text-primary-400" />
        </div>
        <h2 className="font-semibold text-gray-900 dark:text-gray-100">
          {t('employeeDashboard.leave.form.title')}
        </h2>
      </div>

      {submitted && (
        <div className="mb-4 flex items-center gap-2 rounded-xl bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 px-4 py-3">
          <CheckCircle className="h-4 w-4 text-green-600 dark:text-green-400 flex-shrink-0" />
          <span className="text-sm text-green-700 dark:text-green-300">
            {t('employeeDashboard.leave.form.submitSuccess')}
          </span>
        </div>
      )}

      <form onSubmit={handleSubmit} noValidate>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
          {companies.length > 0 && (
            <div className="md:col-span-2">
              <label htmlFor="companyId" className={labelClass}>
                {t('employeeDashboard.leave.form.company')}
                <span className="text-red-500 ml-1">*</span>
              </label>
              {shouldSelectCompany ? (
                <select
                  id="companyId"
                  value={form.companyId}
                  onChange={(e) => setField('companyId', e.target.value)}
                  className={`form-select ${errors.companyId ? 'border-red-400 dark:border-red-500' : ''}`}
                >
                  <option value="">{t('employeeDashboard.leave.form.companyPlaceholder')}</option>
                  {companies.map((company) => (
                    <option key={company.id} value={company.id}>
                      {company.name}
                    </option>
                  ))}
                </select>
              ) : (
                <Input
                  id="companyId"
                  value={selectedCompany?.name ?? ''}
                  readOnly
                  aria-readonly="true"
                  className="bg-gray-50 dark:bg-slate-800"
                />
              )}
              {errors.companyId && <p className={errorClass}>{errors.companyId}</p>}
            </div>
          )}

          <div className="md:col-span-2">
            <label htmlFor="leaveType" className={labelClass}>
              {t('employeeDashboard.leave.form.leaveType')}
              <span className="text-red-500 ml-1">*</span>
            </label>
            <select
              id="leaveType"
              value={form.leaveType}
              onChange={(e) => setField('leaveType', e.target.value as LeaveType)}
              className={`form-select ${errors.leaveType ? 'border-red-400 dark:border-red-500' : ''}`}
            >
              <option value="">{t('employeeDashboard.leave.form.leaveTypePlaceholder')}</option>
              {LEAVE_TYPES.map((type) => (
                <option key={type} value={type}>
                  {leaveTypeLabel(type, t)}
                </option>
              ))}
            </select>
            {errors.leaveType && <p className={errorClass}>{errors.leaveType}</p>}
          </div>

          <div>
            <label htmlFor="dateFrom" className={labelClass}>
              {t('employeeDashboard.leave.form.dateFrom')}
              <span className="text-red-500 ml-1">*</span>
            </label>
            <DatePickerInput
              id="dateFrom"
              value={form.dateFrom}
              onChange={(v) => setField('dateFrom', v)}
              error={!!errors.dateFrom}
            />
            {errors.dateFrom && <p className={errorClass}>{errors.dateFrom}</p>}
          </div>

          <div>
            <label htmlFor="dateTo" className={labelClass}>
              {t('employeeDashboard.leave.form.dateTo')}
              <span className="text-red-500 ml-1">*</span>
            </label>
            <DatePickerInput
              id="dateTo"
              value={form.dateTo}
              min={form.dateFrom || undefined}
              onChange={(v) => setField('dateTo', v)}
              error={!!errors.dateTo}
            />
            {errors.dateTo && <p className={errorClass}>{errors.dateTo}</p>}
          </div>

          <div>
            <label className={labelClass}>
              {t('employeeDashboard.leave.form.daysCount')}
            </label>
            <div className="flex h-10 items-center gap-2 rounded-md border border-gray-300 dark:border-gray-600 bg-gray-50 dark:bg-slate-800 px-3 text-sm text-gray-700 dark:text-gray-300">
              <Clock className="h-4 w-4 text-gray-400 dark:text-gray-500 flex-shrink-0" />
              <span className="font-semibold tabular-nums">{daysCount}</span>
              <span className="text-gray-500 dark:text-gray-400">
                {t('employeeDashboard.leave.balance.daysUnit')}
              </span>
              {form.dateFrom && form.dateTo && daysCount > 0 && (
                <span className="ml-auto text-xs text-gray-400 dark:text-gray-500">
                  {t('employeeDashboard.leave.form.workingDays')}
                </span>
              )}
            </div>
            <p className="mt-1 text-xs text-gray-400 dark:text-gray-500 flex items-center gap-1">
              <Info className="h-3 w-3" />
              {t('employeeDashboard.leave.form.daysCountHint')}
            </p>
          </div>

          <div>
            <label htmlFor="substituteId" className={labelClass}>
              {t('employeeDashboard.leave.form.substitute')}
            </label>
            <select
              id="substituteId"
              value={form.substituteId}
              onChange={(e) => setField('substituteId', e.target.value)}
              className="form-select"
            >
              <option value="">{t('employeeDashboard.leave.form.substitutePlaceholder')}</option>
              {substitutes.map((substitute) => (
                <option key={substitute.id} value={substitute.id}>
                  {substitute.fullName} - {substitute.position}
                </option>
              ))}
            </select>
          </div>

          <div className="md:col-span-2">
            <label htmlFor="comment" className={labelClass}>
              {t('employeeDashboard.leave.form.comment')}
              <span className="ml-2 text-xs font-normal text-gray-400 dark:text-gray-500">
                ({t('employeeDashboard.leave.form.optional')})
              </span>
            </label>
            <Textarea
              id="comment"
              rows={3}
              value={form.comment}
              onChange={(e) => setField('comment', e.target.value)}
              placeholder={t('employeeDashboard.leave.form.commentPlaceholder')}
              className="resize-none"
            />
          </div>

          <div className="md:col-span-2">
            <label className={labelClass}>
              {t('employeeDashboard.leave.form.attachment')}
              <span className="ml-2 text-xs font-normal text-gray-400 dark:text-gray-500">
                ({t('employeeDashboard.leave.form.optional')})
              </span>
            </label>
            <div
              className="flex items-center gap-3 rounded-xl border-2 border-dashed border-gray-200 dark:border-slate-700 bg-gray-50 dark:bg-slate-900/50 px-4 py-3 cursor-not-allowed opacity-60"
              aria-disabled="true"
              title={t('employeeDashboard.leave.form.attachmentNotAvailable')}
            >
              <Paperclip className="h-5 w-5 text-gray-400 dark:text-gray-500 flex-shrink-0" />
              <div>
                <p className="text-sm text-gray-500 dark:text-gray-400">
                  {t('employeeDashboard.leave.form.attachmentPlaceholder')}
                </p>
                <p className="text-xs text-gray-400 dark:text-gray-500 mt-0.5">
                  {t('employeeDashboard.leave.form.attachmentComingSoon')}
                </p>
              </div>
            </div>
          </div>
        </div>

        <div className="mt-6 flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
          <Button
            type="button"
            variant="outline"
            onClick={handleCancel}
            disabled={isSubmitting}
          >
            <X className="h-4 w-4 mr-2" />
            {t('employeeDashboard.leave.form.cancel')}
          </Button>
          <Button type="submit" variant="primary" disabled={isSubmitting}>
            <Send className="h-4 w-4 mr-2" />
            {isSubmitting
              ? t('employeeDashboard.leave.form.submitting')
              : t('employeeDashboard.leave.form.submit')}
          </Button>
        </div>
      </form>
    </div>
  )
}
