import React, { useState, useMemo, useRef } from 'react'
import {
  CalendarDays,
  TrendingDown,
  Send,
  X,
  CheckCircle,
  XCircle,
  AlertCircle,
  Ban,
  FileText,
  Paperclip,
  ChevronRight,
  Info,
  Clock,
} from 'lucide-react'
import { Button } from '@/shared/components/ui/Button'
import { Input } from '@/shared/components/ui/Input'
import { Textarea } from '@/shared/components/ui/Textarea'
import { Modal } from '@/shared/components/ui/Modal'
import { useI18n } from '@/shared/contexts/I18nContext'
import { useLeaveRequest } from '../hooks/useLeaveRequest'
import type {
  LeaveType,
  LeaveRequestStatus,
  LeaveRequestRecord,
  CreateLeaveRequestPayload,
} from '../types/leaveRequest'

// ---------------------------------------------------------------------------
// DatePickerInput – native date input with custom calendar icon on the right
// ---------------------------------------------------------------------------

interface DatePickerInputProps {
  id: string
  value: string
  min?: string
  onChange: (value: string) => void
  error?: boolean
}

function DatePickerInput({ id, value, min, onChange, error }: DatePickerInputProps) {
  const inputRef = useRef<HTMLInputElement>(null)

  return (
    <div className="relative">
      <input
        ref={inputRef}
        id={id}
        type="date"
        value={value}
        min={min}
        onChange={(e) => onChange(e.target.value)}
        className={`flex h-10 w-full rounded-md border bg-white px-3 py-2 pr-10 text-sm ring-offset-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-gray-800 dark:text-gray-100 dark:ring-offset-gray-900 [color-scheme:light] dark:[color-scheme:dark] [&::-webkit-calendar-picker-indicator]:absolute [&::-webkit-calendar-picker-indicator]:right-0 [&::-webkit-calendar-picker-indicator]:h-full [&::-webkit-calendar-picker-indicator]:w-10 [&::-webkit-calendar-picker-indicator]:cursor-pointer [&::-webkit-calendar-picker-indicator]:opacity-0 ${
          error
            ? 'border-red-500 focus-visible:ring-red-500 dark:border-red-600'
            : 'border-gray-300 focus-visible:ring-blue-600 dark:border-gray-600 dark:focus-visible:ring-blue-500'
        }`}
      />
      <CalendarDays className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-gray-400 dark:text-gray-500" />
    </div>
  )
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('pl-PL', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  })
}

function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleDateString('pl-PL', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  })
}

/** Count working days between two ISO date strings (Mon–Fri, no holidays). */
function countWorkingDays(from: string, to: string): number {
  if (!from || !to) return 0
  const start = new Date(from)
  const end = new Date(to)
  if (end < start) return 0
  let count = 0
  const current = new Date(start)
  while (current <= end) {
    const day = current.getDay()
    if (day !== 0 && day !== 6) count++
    current.setDate(current.getDate() + 1)
  }
  return count
}

// ---------------------------------------------------------------------------
// StatusBadge
// ---------------------------------------------------------------------------

function StatusBadge({ status }: { status: LeaveRequestStatus }) {
  const { t } = useI18n()

  const configs: Record<
    LeaveRequestStatus,
    { icon: React.ComponentType<{ className?: string }>; label: string; className: string }
  > = {
    pending: {
      icon: AlertCircle,
      label: t('employeeDashboard.leave.status.pending'),
      className:
        'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
    },
    approved: {
      icon: CheckCircle,
      label: t('employeeDashboard.leave.status.approved'),
      className:
        'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400',
    },
    rejected: {
      icon: XCircle,
      label: t('employeeDashboard.leave.status.rejected'),
      className: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',
    },
    cancelled: {
      icon: Ban,
      label: t('employeeDashboard.leave.status.cancelled'),
      className:
        'bg-gray-100 text-gray-600 dark:bg-slate-800 dark:text-gray-400',
    },
  }

  const { icon: Icon, label, className } = configs[status]

  return (
    <span
      className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium ${className}`}
    >
      <Icon className="h-3 w-3" />
      {label}
    </span>
  )
}

// ---------------------------------------------------------------------------
// LeaveTypeLabel helper
// ---------------------------------------------------------------------------

function leaveTypeLabel(type: LeaveType, t: (key: string) => string): string {
  const map: Record<LeaveType, string> = {
    annual: t('employeeDashboard.leave.type.annual'),
    onDemand: t('employeeDashboard.leave.type.onDemand'),
    occasional: t('employeeDashboard.leave.type.occasional'),
    childCare: t('employeeDashboard.leave.type.childCare'),
    homeOffice: t('employeeDashboard.leave.type.homeOffice'),
    delegation: t('employeeDashboard.leave.type.delegation'),
  }
  return map[type] ?? type
}

// ---------------------------------------------------------------------------
// LeaveBalanceCard
// ---------------------------------------------------------------------------

interface LeaveBalanceCardProps {
  annual: number
  carryover: number
  onDemand: number
  total: number
}

function LeaveBalanceCard({ annual, carryover, onDemand, total }: LeaveBalanceCardProps) {
  const { t } = useI18n()

  return (
    <div className="surface p-5">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <div className="p-2 bg-green-50 dark:bg-green-900/20 rounded-lg">
            <CalendarDays className="h-5 w-5 text-green-600 dark:text-green-400" />
          </div>
          <h2 className="font-semibold text-gray-900 dark:text-gray-100">
            {t('employeeDashboard.leave.balance.title')}
          </h2>
        </div>
        <div className="text-right">
          <span className="text-3xl font-bold text-green-600 dark:text-green-400 tabular-nums">
            {total}
          </span>
          <span className="text-sm font-normal text-gray-500 dark:text-gray-400 ml-1">
            {t('employeeDashboard.leave.balance.days')}
          </span>
        </div>
      </div>

      <div className="grid grid-cols-3 gap-3">
        <BalancePill
          label={t('employeeDashboard.leave.balance.annual')}
          value={annual}
          colorClass="bg-blue-50 dark:bg-blue-900/20 text-blue-700 dark:text-blue-400"
        />
        <BalancePill
          label={t('employeeDashboard.leave.balance.carryover')}
          value={carryover}
          colorClass="bg-purple-50 dark:bg-purple-900/20 text-purple-700 dark:text-purple-400"
        />
        <BalancePill
          label={t('employeeDashboard.leave.balance.onDemand')}
          value={onDemand}
          colorClass="bg-orange-50 dark:bg-orange-900/20 text-orange-700 dark:text-orange-400"
        />
      </div>

      <div className="mt-3 flex items-center gap-1.5 text-xs text-gray-500 dark:text-gray-500">
        <TrendingDown className="h-3.5 w-3.5" />
        <span>{t('employeeDashboard.leave.balance.totalAvailable', { total: String(total) })}</span>
      </div>
    </div>
  )
}

interface BalancePillProps {
  label: string
  value: number
  colorClass: string
}

function BalancePill({ label, value, colorClass }: BalancePillProps) {
  const { t } = useI18n()

  return (
    <div className={`rounded-xl p-3 text-center ${colorClass}`}>
      <div className="text-2xl font-bold tabular-nums">{value}</div>
      <div className="text-xs mt-0.5 font-medium opacity-80">{label}</div>
      <div className="text-xs opacity-60">{t('employeeDashboard.leave.balance.daysUnit')}</div>
    </div>
  )
}

// ---------------------------------------------------------------------------
// LeaveRequestForm
// ---------------------------------------------------------------------------

const LEAVE_TYPES: LeaveType[] = [
  'annual',
  'onDemand',
  'occasional',
  'childCare',
  'homeOffice',
  'delegation',
]

interface FormState {
  leaveType: LeaveType | ''
  dateFrom: string
  dateTo: string
  substituteId: string
  comment: string
}

const INITIAL_FORM: FormState = {
  leaveType: '',
  dateFrom: '',
  dateTo: '',
  substituteId: '',
  comment: '',
}

interface LeaveRequestFormProps {
  substitutes: Array<{ id: string; fullName: string; position: string }>
  isSubmitting: boolean
  onSubmit: (payload: CreateLeaveRequestPayload) => Promise<void>
}

function LeaveRequestForm({ substitutes, isSubmitting, onSubmit }: LeaveRequestFormProps) {
  const { t } = useI18n()
  const [form, setForm] = useState<FormState>(INITIAL_FORM)
  const [submitted, setSubmitted] = useState(false)
  const [errors, setErrors] = useState<Partial<Record<keyof FormState, string>>>({})

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

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!validate()) return
    await onSubmit({
      leaveType: form.leaveType as LeaveType,
      dateFrom: form.dateFrom,
      dateTo: form.dateTo,
      daysCount,
      substituteId: form.substituteId || undefined,
      comment: form.comment || undefined,
    })
    setForm(INITIAL_FORM)
    setErrors({})
    setSubmitted(true)
    setTimeout(() => setSubmitted(false), 4000)
  }

  function handleCancel() {
    setForm(INITIAL_FORM)
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
          {/* Leave type */}
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

          {/* Date from */}
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

          {/* Date to */}
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

          {/* Days count (read-only) */}
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

          {/* Substitute */}
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
              {substitutes.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.fullName} – {s.position}
                </option>
              ))}
            </select>
          </div>

          {/* Comment */}
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

          {/* Attachment placeholder */}
          <div className="md:col-span-2">
            <label className={labelClass}>
              {t('employeeDashboard.leave.form.attachment')}
              <span className="ml-2 text-xs font-normal text-gray-400 dark:text-gray-500">
                ({t('employeeDashboard.leave.form.optional')})
              </span>
            </label>
            {/* Placeholder – real file upload to be implemented with backend */}
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

        {/* Actions */}
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

// ---------------------------------------------------------------------------
// RequestDetailModal
// ---------------------------------------------------------------------------

interface RequestDetailModalProps {
  request: LeaveRequestRecord | null
  onClose: () => void
}

function RequestDetailModal({ request, onClose }: RequestDetailModalProps) {
  const { t } = useI18n()
  if (!request) return null

  return (
    <Modal
      isOpen={!!request}
      onClose={onClose}
      title={t('employeeDashboard.leave.detail.title')}
      size="md"
    >
      <div className="p-6 space-y-4">
        <div className="flex items-center justify-between">
          <span className="text-sm text-gray-500 dark:text-gray-400">
            {t('employeeDashboard.leave.detail.status')}
          </span>
          <StatusBadge status={request.status} />
        </div>

        <DetailRow
          label={t('employeeDashboard.leave.detail.leaveType')}
          value={leaveTypeLabel(request.leaveType, t)}
        />
        <DetailRow
          label={t('employeeDashboard.leave.detail.dateFrom')}
          value={formatDate(request.dateFrom)}
        />
        <DetailRow
          label={t('employeeDashboard.leave.detail.dateTo')}
          value={formatDate(request.dateTo)}
        />
        <DetailRow
          label={t('employeeDashboard.leave.detail.daysCount')}
          value={`${request.daysCount} ${t('employeeDashboard.leave.balance.daysUnit')}`}
        />
        {request.substituteName && (
          <DetailRow
            label={t('employeeDashboard.leave.detail.substitute')}
            value={request.substituteName}
          />
        )}
        {request.comment && (
          <DetailRow
            label={t('employeeDashboard.leave.detail.comment')}
            value={request.comment}
          />
        )}
        <DetailRow
          label={t('employeeDashboard.leave.detail.createdAt')}
          value={formatDateTime(request.createdAt)}
        />

        {(request.reviewedAt || request.managerComment) && (
          <div className="pt-3 border-t border-gray-100 dark:border-slate-800 space-y-3">
            <p className="text-xs font-semibold text-gray-500 dark:text-gray-500 uppercase tracking-wider">
              {t('employeeDashboard.leave.detail.managerSection')}
            </p>
            {request.reviewedBy && (
              <DetailRow
                label={t('employeeDashboard.leave.detail.reviewedBy')}
                value={request.reviewedBy}
              />
            )}
            {request.reviewedAt && (
              <DetailRow
                label={t('employeeDashboard.leave.detail.reviewedAt')}
                value={formatDateTime(request.reviewedAt)}
              />
            )}
            {request.managerComment && (
              <DetailRow
                label={t('employeeDashboard.leave.detail.managerComment')}
                value={request.managerComment}
              />
            )}
          </div>
        )}
      </div>
    </Modal>
  )
}

interface DetailRowProps {
  label: string
  value: string
}

function DetailRow({ label, value }: DetailRowProps) {
  return (
    <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-1">
      <span className="text-sm text-gray-500 dark:text-gray-400 flex-shrink-0">{label}</span>
      <span className="text-sm font-medium text-gray-900 dark:text-gray-100 sm:text-right">
        {value}
      </span>
    </div>
  )
}

// ---------------------------------------------------------------------------
// LeaveHistoryTable
// ---------------------------------------------------------------------------

interface LeaveHistoryTableProps {
  requests: LeaveRequestRecord[]
  isCancelling: boolean
  onViewDetail: (request: LeaveRequestRecord) => void
  onCancel: (requestId: string) => void
}

function LeaveHistoryTable({
  requests,
  isCancelling,
  onViewDetail,
  onCancel,
}: LeaveHistoryTableProps) {
  const { t } = useI18n()

  if (requests.length === 0) {
    return (
      <div className="surface p-10 text-center">
        <CalendarDays className="h-10 w-10 text-gray-300 dark:text-slate-600 mx-auto mb-3" />
        <p className="text-sm text-gray-500 dark:text-gray-400">
          {t('employeeDashboard.leave.history.empty')}
        </p>
      </div>
    )
  }

  return (
    <div className="surface overflow-hidden">
      <div className="px-5 py-4 border-b border-gray-100 dark:border-slate-800 flex items-center gap-2">
        <div className="p-2 bg-primary-50 dark:bg-primary-900/20 rounded-lg">
          <CalendarDays className="h-5 w-5 text-primary-600 dark:text-primary-400" />
        </div>
        <h2 className="font-semibold text-gray-900 dark:text-gray-100">
          {t('employeeDashboard.leave.history.title')}
        </h2>
        <span className="ml-auto text-xs text-gray-400 dark:text-gray-500 tabular-nums">
          {requests.length} {t('employeeDashboard.leave.history.recordsCount')}
        </span>
      </div>

      {/* Desktop table */}
      <div className="hidden md:block overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-gray-100 dark:border-slate-800 bg-gray-50 dark:bg-slate-900/50">
              <th className="text-left px-5 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                {t('employeeDashboard.leave.history.col.leaveType')}
              </th>
              <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                {t('employeeDashboard.leave.history.col.dateFrom')}
              </th>
              <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                {t('employeeDashboard.leave.history.col.dateTo')}
              </th>
              <th className="text-center px-4 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                {t('employeeDashboard.leave.history.col.daysCount')}
              </th>
              <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                {t('employeeDashboard.leave.history.col.status')}
              </th>
              <th className="text-left px-4 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                {t('employeeDashboard.leave.history.col.createdAt')}
              </th>
              <th className="text-right px-5 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                {t('employeeDashboard.leave.history.col.actions')}
              </th>
            </tr>
          </thead>
          <tbody>
            {requests.map((req) => (
              <tr
                key={req.id}
                className="border-b border-gray-50 dark:border-slate-800 hover:bg-gray-50 dark:hover:bg-slate-800/50 transition-colors last:border-0"
              >
                <td className="px-5 py-3.5 font-medium text-gray-900 dark:text-gray-100">
                  {leaveTypeLabel(req.leaveType, t)}
                </td>
                <td className="px-4 py-3.5 text-gray-600 dark:text-gray-300 tabular-nums">
                  {formatDate(req.dateFrom)}
                </td>
                <td className="px-4 py-3.5 text-gray-600 dark:text-gray-300 tabular-nums">
                  {formatDate(req.dateTo)}
                </td>
                <td className="px-4 py-3.5 text-center">
                  <span className="font-semibold tabular-nums text-gray-900 dark:text-gray-100">
                    {req.daysCount}
                  </span>
                </td>
                <td className="px-4 py-3.5">
                  <StatusBadge status={req.status} />
                </td>
                <td className="px-4 py-3.5 text-gray-500 dark:text-gray-400 tabular-nums text-xs">
                  {formatDateTime(req.createdAt)}
                </td>
                <td className="px-5 py-3.5">
                  <div className="flex items-center justify-end gap-2">
                    <button
                      onClick={() => onViewDetail(req)}
                      className="inline-flex items-center gap-1 text-xs font-medium text-primary-600 dark:text-primary-400 hover:text-primary-700 dark:hover:text-primary-300 transition-colors"
                    >
                      <ChevronRight className="h-3.5 w-3.5" />
                      {t('employeeDashboard.leave.history.actions.details')}
                    </button>
                    {req.status === 'pending' && (
                      <button
                        onClick={() => onCancel(req.id)}
                        disabled={isCancelling}
                        className="inline-flex items-center gap-1 text-xs font-medium text-red-500 dark:text-red-400 hover:text-red-600 dark:hover:text-red-300 transition-colors disabled:opacity-50 disabled:pointer-events-none"
                      >
                        <X className="h-3.5 w-3.5" />
                        {t('employeeDashboard.leave.history.actions.cancel')}
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Mobile card list */}
      <div className="md:hidden divide-y divide-gray-100 dark:divide-slate-800">
        {requests.map((req) => (
          <div key={req.id} className="px-4 py-4 space-y-2">
            <div className="flex items-start justify-between gap-2">
              <span className="text-sm font-medium text-gray-900 dark:text-gray-100">
                {leaveTypeLabel(req.leaveType, t)}
              </span>
              <StatusBadge status={req.status} />
            </div>
            <div className="flex items-center gap-3 text-xs text-gray-500 dark:text-gray-400">
              <span className="tabular-nums">{formatDate(req.dateFrom)}</span>
              <span>→</span>
              <span className="tabular-nums">{formatDate(req.dateTo)}</span>
              <span className="font-semibold text-gray-700 dark:text-gray-300">
                {req.daysCount} {t('employeeDashboard.leave.balance.daysUnit')}
              </span>
            </div>
            <div className="flex items-center gap-3 pt-1">
              <button
                onClick={() => onViewDetail(req)}
                className="text-xs font-medium text-primary-600 dark:text-primary-400 hover:underline"
              >
                {t('employeeDashboard.leave.history.actions.details')}
              </button>
              {req.status === 'pending' && (
                <button
                  onClick={() => onCancel(req.id)}
                  disabled={isCancelling}
                  className="text-xs font-medium text-red-500 dark:text-red-400 hover:underline disabled:opacity-50"
                >
                  {t('employeeDashboard.leave.history.actions.cancel')}
                </button>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

// ---------------------------------------------------------------------------
// Tab bar
// ---------------------------------------------------------------------------

type Tab = 'new' | 'history'

interface TabBarProps {
  active: Tab
  onChange: (tab: Tab) => void
}

function TabBar({ active, onChange }: TabBarProps) {
  const { t } = useI18n()

  const tabs: Array<{ id: Tab; label: string }> = [
    { id: 'new', label: t('employeeDashboard.leave.tabs.new') },
    { id: 'history', label: t('employeeDashboard.leave.tabs.history') },
  ]

  return (
    <div className="flex gap-1 p-1 bg-gray-100 dark:bg-slate-800 rounded-xl w-fit">
      {tabs.map((tab) => (
        <button
          key={tab.id}
          onClick={() => onChange(tab.id)}
          className={`px-4 py-2 text-sm font-medium rounded-lg transition-colors ${
            active === tab.id
              ? 'bg-white dark:bg-slate-900 text-gray-900 dark:text-gray-100 shadow-sm'
              : 'text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200'
          }`}
        >
          {tab.label}
        </button>
      ))}
    </div>
  )
}

// ---------------------------------------------------------------------------
// LeaveRequest (main export)
// ---------------------------------------------------------------------------

export function LeaveRequest() {
  const { t } = useI18n()
  const { data, isLoading, isSubmitting, isCancelling, error, submitRequest, cancelRequest } =
    useLeaveRequest()
  const [activeTab, setActiveTab] = useState<Tab>('new')
  const [detailRequest, setDetailRequest] = useState<LeaveRequestRecord | null>(null)

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-48">
        <div className="h-8 w-8 rounded-full border-4 border-primary-200 border-t-primary-600 animate-spin" />
      </div>
    )
  }

  if (error) {
    return (
      <div className="surface p-6 text-center text-red-600 dark:text-red-400">
        <XCircle className="h-8 w-8 mx-auto mb-2" />
        <p className="text-sm">{error}</p>
      </div>
    )
  }

  const balance = data?.leaveBalance ?? { annual: 0, carryover: 0, onDemand: 0, total: 0 }
  const requests = data?.requests ?? []
  const substitutes = data?.substitutes ?? []

  return (
    <div className="space-y-5 text-gray-900 dark:text-gray-100">
      {/* Page header */}
      <div className="flex items-center gap-3">
        <CalendarDays className="h-8 w-8 text-primary-600 dark:text-primary-400" />
        <h1 className="text-2xl font-bold">{t('employeeDashboard.leaveRequest')}</h1>
      </div>

      {/* Tab navigation */}
      <TabBar active={activeTab} onChange={setActiveTab} />

      {/* Tab: Nowy wniosek */}
      {activeTab === 'new' && (
        <div className="space-y-5">
          <LeaveBalanceCard
            annual={balance.annual}
            carryover={balance.carryover}
            onDemand={balance.onDemand}
            total={balance.total}
          />
          <LeaveRequestForm
            substitutes={substitutes}
            isSubmitting={isSubmitting}
            onSubmit={async (payload) => {
              await submitRequest(payload)
            }}
          />
        </div>
      )}

      {/* Tab: Historia wniosków */}
      {activeTab === 'history' && (
        <LeaveHistoryTable
          requests={requests}
          isCancelling={isCancelling}
          onViewDetail={setDetailRequest}
          onCancel={cancelRequest}
        />
      )}

      {/* Detail modal */}
      <RequestDetailModal
        request={detailRequest}
        onClose={() => setDetailRequest(null)}
      />
    </div>
  )
}
