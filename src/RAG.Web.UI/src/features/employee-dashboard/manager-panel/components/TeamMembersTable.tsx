import React, { useEffect, useMemo, useState } from 'react'
import {
  createColumnHelper,
  flexRender,
  getCoreRowModel,
  getFilteredRowModel,
  getSortedRowModel,
  useReactTable,
  type ColumnFiltersState,
  type FilterFn,
  type SortingState,
} from '@tanstack/react-table'
import { Mail, Phone, Search, UserRound } from 'lucide-react'
import removeAccents from 'remove-accents'
import type { TeamMember, TeamMemberPresenceStatus } from '../types/managerTypes'
import { presenceStatusLabel } from './managerPanelUtils'
import { ManagerStatusBadge } from './ManagerStatusBadge'
import { useManagerT } from './managerTranslations'

interface TeamMembersTableProps {
  members: TeamMember[]
  initialStatusFilter?: TeamMemberPresenceStatus
}

const columnHelper = createColumnHelper<TeamMember>()

const diacriticsInsensitiveFilter: FilterFn<TeamMember> = (row, columnId, filterValue) => {
  const value = row.getValue(columnId)
  if (value == null) return false
  return removeAccents(String(value)).toLowerCase().includes(
    removeAccents(String(filterValue)).toLowerCase()
  )
}

const globalFilter: FilterFn<TeamMember> = (row, _columnId, filterValue) => {
  const searchable = [
    row.original.fullName,
    row.original.position,
    row.original.department,
    row.original.seniority,
    row.original.currentProject,
  ].join(' ')
  return removeAccents(searchable).toLowerCase().includes(
    removeAccents(String(filterValue)).toLowerCase()
  )
}

export function TeamMembersTable({ members, initialStatusFilter }: TeamMembersTableProps) {
  const t = useManagerT()
  const [selectedMemberId, setSelectedMemberId] = useState(
    () => members.find((member) => member.presenceStatus === initialStatusFilter)?.id ?? members[0]?.id ?? ''
  )
  const [sorting, setSorting] = useState<SortingState>([{ id: 'employee', desc: false }])
  const [columnFilters, setColumnFilters] = useState<ColumnFiltersState>(() =>
    initialStatusFilter
      ? [{ id: 'status', value: presenceStatusLabel(initialStatusFilter, t) }]
      : []
  )
  const [query, setQuery] = useState('')

  const columns = useMemo(
    () => [
      columnHelper.accessor('fullName', {
        id: 'employee',
        header: t('team.col.employee'),
        filterFn: diacriticsInsensitiveFilter,
        cell: (info) => (
          <span className="font-medium text-gray-900 dark:text-gray-100">{info.getValue()}</span>
        ),
      }),
      columnHelper.accessor('position', {
        header: t('team.col.position'),
        filterFn: diacriticsInsensitiveFilter,
      }),
      columnHelper.accessor('seniority', {
        header: t('team.col.seniority'),
        filterFn: diacriticsInsensitiveFilter,
      }),
      columnHelper.accessor((member) => presenceStatusLabel(member.presenceStatus, t), {
        id: 'status',
        header: t('team.col.status'),
        filterFn: diacriticsInsensitiveFilter,
        cell: ({ row }) => (
          <ManagerStatusBadge type="presence" status={row.original.presenceStatus} />
        ),
      }),
      columnHelper.accessor('remainingLeaveDays', {
        id: 'leave',
        header: t('team.col.leave'),
        filterFn: diacriticsInsensitiveFilter,
        cell: (info) => (
          <span className="font-semibold tabular-nums text-gray-900 dark:text-gray-100">
            {info.getValue()}
          </span>
        ),
      }),
      columnHelper.accessor('absenceDaysThisYear', {
        id: 'absences',
        header: t('team.col.absences'),
        filterFn: diacriticsInsensitiveFilter,
        cell: (info) => <span className="tabular-nums">{info.getValue()}</span>,
      }),
    ],
    [t]
  )

  const table = useReactTable({
    data: members,
    columns,
    state: { sorting, columnFilters, globalFilter: query },
    onSortingChange: setSorting,
    onColumnFiltersChange: setColumnFilters,
    onGlobalFilterChange: setQuery,
    getCoreRowModel: getCoreRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    getSortedRowModel: getSortedRowModel(),
    globalFilterFn: globalFilter,
  })

  const visibleMembers = table.getRowModel().rows.map((row) => row.original)
  const selectedMember = visibleMembers.find((member) => member.id === selectedMemberId) ?? visibleMembers[0]

  useEffect(() => {
    if (selectedMember && selectedMember.id !== selectedMemberId) {
      setSelectedMemberId(selectedMember.id)
    }
  }, [selectedMember, selectedMemberId])

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
                {t('team.count', { count: visibleMembers.length })}
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
              {table.getHeaderGroups().map((headerGroup) => (
                <React.Fragment key={headerGroup.id}>
                  <tr className="border-b border-gray-100 bg-gray-50 dark:border-slate-800 dark:bg-slate-900/50">
                    {headerGroup.headers.map((header) => (
                      <th
                        key={header.id}
                        onClick={header.column.getToggleSortingHandler()}
                        className={`px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 transition-colors dark:text-gray-400 ${
                          header.column.getCanSort()
                            ? 'cursor-pointer select-none hover:bg-gray-100 dark:hover:bg-slate-700/60'
                            : ''
                        }`}
                      >
                        <div className="flex items-center gap-2">
                          {flexRender(header.column.columnDef.header, header.getContext())}
                          {header.column.getCanSort() && (
                            <span className="text-gray-400 dark:text-gray-500">
                              {{ asc: '↑', desc: '↓' }[header.column.getIsSorted() as string] ?? '↕'}
                            </span>
                          )}
                        </div>
                      </th>
                    ))}
                  </tr>
                  <tr>
                    {headerGroup.headers.map((header) => (
                      <th key={`filter-${header.id}`} className="bg-gray-100 px-3 py-2 dark:bg-slate-800/60">
                        {header.column.getCanFilter() && (
                          <input
                            type="text"
                            value={(header.column.getFilterValue() ?? '') as string}
                            onChange={(event) => header.column.setFilterValue(event.target.value)}
                            placeholder={`${t('table.searchIn')} ${String(header.column.columnDef.header).toLowerCase()}...`}
                            className="form-input w-full py-1 text-xs"
                            onClick={(event) => event.stopPropagation()}
                          />
                        )}
                      </th>
                    ))}
                  </tr>
                </React.Fragment>
              ))}
            </thead>
            <tbody>
              {table.getRowModel().rows.length === 0 ? (
                <tr>
                  <td colSpan={columns.length} className="px-6 py-8 text-center text-gray-500 dark:text-gray-300">
                    {t('table.noResults')}
                  </td>
                </tr>
              ) : (
                table.getRowModel().rows.map((row) => (
                  <tr
                    key={row.id}
                    onClick={() => setSelectedMemberId(row.original.id)}
                    className={`cursor-pointer border-b border-gray-50 transition-colors last:border-0 dark:border-slate-800 ${
                      selectedMember?.id === row.original.id
                        ? 'bg-primary-50/80 dark:bg-primary-900/20'
                        : 'hover:bg-gray-50 dark:hover:bg-slate-800/50'
                    }`}
                  >
                    {row.getVisibleCells().map((cell) => (
                      <td key={cell.id} className="px-4 py-3.5 text-gray-600 dark:text-gray-300">
                        {flexRender(cell.column.columnDef.cell, cell.getContext())}
                      </td>
                    ))}
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        <div className="divide-y divide-gray-100 dark:divide-slate-800 lg:hidden">
          {visibleMembers.map((member) => (
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
          {visibleMembers.length === 0 && (
            <p className="px-6 py-8 text-center text-sm text-gray-500 dark:text-gray-300">
              {t('table.noResults')}
            </p>
          )}
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
