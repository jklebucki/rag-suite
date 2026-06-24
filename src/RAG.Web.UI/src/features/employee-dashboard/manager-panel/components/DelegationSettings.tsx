import { CalendarSync, Save } from 'lucide-react'
import { useState } from 'react'
import { Button } from '@/shared/components/ui/Button'
import { DatePickerInput } from '../../leave-request/components/DatePickerInput'
import type {
  ApprovalDelegation,
  DelegationPayload,
  TeamMember,
} from '../types/managerTypes'
import { ManagerStatusBadge } from './ManagerStatusBadge'
import { formatDate, formatDateTime } from './managerPanelUtils'
import { useManagerT } from './managerTranslations'

interface DelegationSettingsProps {
  teamMembers: TeamMember[]
  activeDelegation: ApprovalDelegation | null
  delegations: ApprovalDelegation[]
  isMutating: boolean
  onSave: (payload: DelegationPayload) => Promise<void>
}

export function DelegationSettings({
  teamMembers,
  activeDelegation,
  delegations,
  isMutating,
  onSave,
}: DelegationSettingsProps) {
  const t = useManagerT()
  const [substituteId, setSubstituteId] = useState(activeDelegation?.substituteId ?? '')
  const [dateFrom, setDateFrom] = useState(activeDelegation?.dateFrom ?? '')
  const [dateTo, setDateTo] = useState(activeDelegation?.dateTo ?? '')
  const [error, setError] = useState('')

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    if (!substituteId || !dateFrom || !dateTo) {
      setError(t('delegation.required'))
      return
    }
    if (dateTo < dateFrom) {
      setError(t('delegation.dateOrder'))
      return
    }
    setError('')
    await onSave({ substituteId, dateFrom, dateTo })
  }

  return (
    <div className="space-y-5">
      <div className="grid grid-cols-1 gap-5 xl:grid-cols-[minmax(0,1fr)_360px]">
        <form onSubmit={handleSubmit} className="surface p-5">
          <div className="mb-5 flex items-center gap-2">
            <div className="rounded-lg bg-primary-50 p-2 dark:bg-primary-900/20">
              <CalendarSync className="h-5 w-5 text-primary-600 dark:text-primary-400" />
            </div>
            <div>
              <h2 className="font-semibold text-gray-900 dark:text-gray-100">
                {t('delegation.formTitle')}
              </h2>
              <p className="text-xs text-gray-500 dark:text-gray-400">
                {t('delegation.formSubtitle')}
              </p>
            </div>
          </div>

          <div className="grid grid-cols-1 gap-5 md:grid-cols-2">
            <div className="md:col-span-2">
              <label
                htmlFor="delegationSubstitute"
                className="mb-1.5 block text-sm font-medium text-gray-700 dark:text-gray-300"
              >
                {t('delegation.substitute')}
              </label>
              <select
                id="delegationSubstitute"
                value={substituteId}
                onChange={(event) => setSubstituteId(event.target.value)}
                className="form-select"
              >
                <option value="">{t('delegation.substitutePlaceholder')}</option>
                {teamMembers.map((member) => (
                  <option key={member.id} value={member.id}>
                    {member.fullName} - {member.position}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label
                htmlFor="delegationDateFrom"
                className="mb-1.5 block text-sm font-medium text-gray-700 dark:text-gray-300"
              >
                {t('delegation.dateFrom')}
              </label>
              <DatePickerInput
                id="delegationDateFrom"
                value={dateFrom}
                onChange={setDateFrom}
              />
            </div>

            <div>
              <label
                htmlFor="delegationDateTo"
                className="mb-1.5 block text-sm font-medium text-gray-700 dark:text-gray-300"
              >
                {t('delegation.dateTo')}
              </label>
              <DatePickerInput
                id="delegationDateTo"
                min={dateFrom || undefined}
                value={dateTo}
                onChange={setDateTo}
              />
            </div>
          </div>

          {error && <p className="mt-3 text-sm text-red-600 dark:text-red-400">{error}</p>}

          <div className="mt-6 flex justify-end">
            <Button type="submit" variant="primary" disabled={isMutating}>
              <Save className="mr-2 h-4 w-4" />
              {t('delegation.save')}
            </Button>
          </div>
        </form>

        <aside className="surface p-5">
          <p className="text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
            {t('delegation.activeTitle')}
          </p>
          {activeDelegation ? (
            <div className="mt-3 space-y-3">
              <div>
                <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
                  {activeDelegation.substituteName}
                </h3>
                <p className="text-sm text-gray-500 dark:text-gray-400">
                  {formatDate(activeDelegation.dateFrom)} - {formatDate(activeDelegation.dateTo)}
                </p>
              </div>
              <ManagerStatusBadge type="delegation" status={activeDelegation.status} />
            </div>
          ) : (
            <p className="mt-3 text-sm text-gray-500 dark:text-gray-400">
              {t('delegation.noActive')}
            </p>
          )}
        </aside>
      </div>

      <div className="surface overflow-hidden">
        <div className="border-b border-gray-100 px-5 py-4 dark:border-slate-800">
          <h2 className="font-semibold text-gray-900 dark:text-gray-100">{t('delegation.logTitle')}</h2>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-100 bg-gray-50 dark:border-slate-800 dark:bg-slate-900/50">
                <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                  {t('delegation.col.createdAt')}
                </th>
                <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                  {t('delegation.col.substitute')}
                </th>
                <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                  {t('delegation.col.dateFrom')}
                </th>
                <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                  {t('delegation.col.dateTo')}
                </th>
                <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                  {t('delegation.col.status')}
                </th>
              </tr>
            </thead>
            <tbody>
              {delegations.map((delegation) => (
                <tr
                  key={delegation.id}
                  className="border-b border-gray-50 last:border-0 hover:bg-gray-50 dark:border-slate-800 dark:hover:bg-slate-800/50"
                >
                  <td className="whitespace-nowrap px-5 py-3.5 text-xs tabular-nums text-gray-500 dark:text-gray-400">
                    {formatDateTime(delegation.createdAt)}
                  </td>
                  <td className="px-4 py-3.5 font-medium text-gray-900 dark:text-gray-100">
                    {delegation.substituteName}
                  </td>
                  <td className="whitespace-nowrap px-4 py-3.5 tabular-nums text-gray-600 dark:text-gray-300">
                    {formatDate(delegation.dateFrom)}
                  </td>
                  <td className="whitespace-nowrap px-4 py-3.5 tabular-nums text-gray-600 dark:text-gray-300">
                    {formatDate(delegation.dateTo)}
                  </td>
                  <td className="px-4 py-3.5">
                    <ManagerStatusBadge type="delegation" status={delegation.status} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
