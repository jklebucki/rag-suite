import { Mail, Phone, Search, UserRound } from 'lucide-react'
import { useMemo, useState } from 'react'
import type { TeamMember } from '../types/managerTypes'
import { ManagerStatusBadge } from './ManagerStatusBadge'
import { useManagerT } from './managerTranslations'

interface TeamMembersTableProps {
  members: TeamMember[]
}

export function TeamMembersTable({ members }: TeamMembersTableProps) {
  const t = useManagerT()
  const [selectedMemberId, setSelectedMemberId] = useState(members[0]?.id ?? '')
  const [query, setQuery] = useState('')
  const selectedMember = members.find((member) => member.id === selectedMemberId) ?? members[0]

  const filteredMembers = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase()
    if (!normalizedQuery) return members
    return members.filter((member) =>
      [member.fullName, member.position, member.department]
        .join(' ')
        .toLowerCase()
        .includes(normalizedQuery)
    )
  }, [members, query])

  return (
    <div className="grid grid-cols-1 gap-5 xl:grid-cols-[minmax(0,1fr)_360px]">
      <div className="surface overflow-hidden">
        <div className="flex flex-col gap-3 border-b border-gray-100 px-5 py-4 dark:border-slate-800 sm:flex-row sm:items-center">
          <div className="flex items-center gap-2">
            <div className="rounded-lg bg-primary-50 p-2 dark:bg-primary-900/20">
              <UserRound className="h-5 w-5 text-primary-600 dark:text-primary-400" />
            </div>
            <div>
              <h2 className="font-semibold text-gray-900 dark:text-gray-100">{t('team.title')}</h2>
              <p className="text-xs text-gray-500 dark:text-gray-400">
                {t('team.count', { count: members.length })}
              </p>
            </div>
          </div>
          <label className="relative sm:ml-auto sm:w-72">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-gray-400" />
            <input
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder={t('team.searchPlaceholder')}
              className="form-input h-10 py-2 pl-9"
            />
          </label>
        </div>

        <div className="hidden overflow-x-auto lg:block">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-100 bg-gray-50 dark:border-slate-800 dark:bg-slate-900/50">
                <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                  {t('team.col.employee')}
                </th>
                <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                  {t('team.col.position')}
                </th>
                <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                  {t('team.col.seniority')}
                </th>
                <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                  {t('team.col.status')}
                </th>
                <th className="px-4 py-3 text-center text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                  {t('team.col.leave')}
                </th>
                <th className="px-4 py-3 text-center text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                  {t('team.col.absences')}
                </th>
              </tr>
            </thead>
            <tbody>
              {filteredMembers.map((member) => (
                <tr
                  key={member.id}
                  onClick={() => setSelectedMemberId(member.id)}
                  className={`cursor-pointer border-b border-gray-50 transition-colors last:border-0 dark:border-slate-800 ${
                    selectedMember?.id === member.id
                      ? 'bg-primary-50/80 dark:bg-primary-900/20'
                      : 'hover:bg-gray-50 dark:hover:bg-slate-800/50'
                  }`}
                >
                  <td className="px-5 py-3.5 font-medium text-gray-900 dark:text-gray-100">
                    {member.fullName}
                  </td>
                  <td className="px-4 py-3.5 text-gray-600 dark:text-gray-300">{member.position}</td>
                  <td className="whitespace-nowrap px-4 py-3.5 text-gray-600 dark:text-gray-300">
                    {member.seniority}
                  </td>
                  <td className="px-4 py-3.5">
                    <ManagerStatusBadge type="presence" status={member.presenceStatus} />
                  </td>
                  <td className="px-4 py-3.5 text-center font-semibold tabular-nums text-gray-900 dark:text-gray-100">
                    {member.remainingLeaveDays}
                  </td>
                  <td className="px-4 py-3.5 text-center tabular-nums text-gray-600 dark:text-gray-300">
                    {member.absenceDaysThisYear}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <div className="divide-y divide-gray-100 dark:divide-slate-800 lg:hidden">
          {filteredMembers.map((member) => (
            <button
              key={member.id}
              type="button"
              onClick={() => setSelectedMemberId(member.id)}
              className="block w-full px-4 py-4 text-left transition-colors hover:bg-gray-50 dark:hover:bg-slate-800/50"
            >
              <div className="flex items-start justify-between gap-3">
                <div>
                  <p className="font-medium text-gray-900 dark:text-gray-100">{member.fullName}</p>
                  <p className="mt-0.5 text-sm text-gray-500 dark:text-gray-400">{member.position}</p>
                </div>
                <ManagerStatusBadge type="presence" status={member.presenceStatus} />
              </div>
              <div className="mt-3 flex gap-4 text-xs text-gray-500 dark:text-gray-400">
                <span>{t('team.mobile.seniority')}: {member.seniority}</span>
                <span>{t('team.mobile.leave')}: {member.remainingLeaveDays} {t('common.days')}</span>
                <span>{t('team.mobile.absences')}: {member.absenceDaysThisYear}</span>
              </div>
            </button>
          ))}
        </div>
      </div>

      {selectedMember && (
        <aside className="surface p-5">
          <div className="flex items-start justify-between gap-3">
            <div>
              <p className="text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                {t('team.employeeCard')}
              </p>
              <h3 className="mt-1 text-lg font-semibold text-gray-900 dark:text-gray-100">
                {selectedMember.fullName}
              </h3>
              <p className="text-sm text-gray-500 dark:text-gray-400">{selectedMember.position}</p>
            </div>
            <ManagerStatusBadge type="presence" status={selectedMember.presenceStatus} />
          </div>

          <div className="mt-5 space-y-4 text-sm">
            <DetailRow label={t('team.detail.department')} value={selectedMember.department} />
            <DetailRow label={t('team.detail.seniority')} value={selectedMember.seniority} />
            <DetailRow
              label={t('team.detail.remainingLeave')}
              value={`${selectedMember.remainingLeaveDays} ${t('common.days')}`}
            />
            <DetailRow
              label={t('team.detail.absences')}
              value={`${selectedMember.absenceDaysThisYear} ${t('common.days')}`}
            />
            <DetailRow label={t('team.detail.project')} value={selectedMember.currentProject} />
          </div>

          <div className="mt-5 space-y-2 border-t border-gray-100 pt-4 dark:border-slate-800">
            <a
              href={`mailto:${selectedMember.email}`}
              className="flex items-center gap-2 text-sm text-primary-600 hover:text-primary-700 dark:text-primary-400 dark:hover:text-primary-300"
            >
              <Mail className="h-4 w-4" />
              {selectedMember.email}
            </a>
            <a
              href={`tel:${selectedMember.phone}`}
              className="flex items-center gap-2 text-sm text-gray-600 hover:text-gray-900 dark:text-gray-300 dark:hover:text-gray-100"
            >
              <Phone className="h-4 w-4" />
              {selectedMember.phone}
            </a>
          </div>
        </aside>
      )}
    </div>
  )
}

function DetailRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-start justify-between gap-4">
      <span className="text-gray-500 dark:text-gray-400">{label}</span>
      <span className="text-right font-medium text-gray-900 dark:text-gray-100">{value}</span>
    </div>
  )
}
