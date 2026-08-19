import React, { useMemo, useState } from 'react'
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
import { AlertTriangle, ChevronRight, ClipboardCheck } from 'lucide-react'
import removeAccents from 'remove-accents'
import { Button } from '@/shared/components/ui/Button'
import type { ApprovalRequest, ManagerApprovalStatus } from '../types/managerTypes'
import { ManagerStatusBadge } from './ManagerStatusBadge'
import {
  approvalStatusLabel,
  formatDate,
  formatDateTime,
  leaveTypeLabel,
} from './managerPanelUtils'
import { useManagerT } from './managerTranslations'

interface PendingRequestsTableProps {
  requests: ApprovalRequest[]
  isMutating: boolean
  onViewDetails: (request: ApprovalRequest) => void
  onApprove: (requestId: string) => void
  onReject: (request: ApprovalRequest) => void
  initialStatusFilter?: ManagerApprovalStatus
  leaveOnly?: boolean
  initialConflictsOnly?: boolean
}

const leaveRequestTypes = new Set(['annual', 'onDemand', 'occasional', 'childCare'])
const columnHelper = createColumnHelper<ApprovalRequest>()

const diacriticsInsensitiveFilter: FilterFn<ApprovalRequest> = (row, columnId, filterValue) => {
  const value = row.getValue(columnId)
  if (value == null) return false
  return removeAccents(String(value)).toLowerCase().includes(
    removeAccents(String(filterValue)).toLowerCase()
  )
}

export function PendingRequestsTable({
  requests,
  isMutating,
  onViewDetails,
  onApprove,
  onReject,
  initialStatusFilter,
  leaveOnly = false,
  initialConflictsOnly = false,
}: PendingRequestsTableProps) {
  const t = useManagerT()
  const [sorting, setSorting] = useState<SortingState>([{ id: 'submittedAt', desc: true }])
  const [columnFilters, setColumnFilters] = useState<ColumnFiltersState>(() =>
    initialStatusFilter
      ? [{ id: 'status', value: approvalStatusLabel(initialStatusFilter, t) }]
      : []
  )
  const [conflictsOnly, setConflictsOnly] = useState(initialConflictsOnly)

  const scopedRequests = useMemo(() => {
    const byType = leaveOnly
      ? requests.filter((request) => leaveRequestTypes.has(request.leaveType))
      : requests
    return conflictsOnly ? byType.filter((request) => request.hasConflict) : byType
  }, [conflictsOnly, leaveOnly, requests])

  const columns = useMemo(
    () => [
      columnHelper.accessor('employeeName', {
        id: 'employee',
        header: t('requests.col.employee'),
        filterFn: diacriticsInsensitiveFilter,
        cell: ({ row }) => (
          <div className="flex items-center gap-2 font-medium text-gray-900 dark:text-gray-100">
            {row.original.hasConflict && <AlertTriangle className="h-4 w-4 text-amber-500" />}
            {row.original.employeeName}
          </div>
        ),
      }),
      columnHelper.accessor((request) => leaveTypeLabel(request.leaveType, t), {
        id: 'type',
        header: t('requests.col.type'),
        filterFn: diacriticsInsensitiveFilter,
      }),
      columnHelper.accessor((request) => formatDate(request.dateFrom), {
        id: 'dateFrom',
        header: t('requests.col.dateFrom'),
        filterFn: diacriticsInsensitiveFilter,
        sortingFn: (rowA, rowB) => rowA.original.dateFrom.localeCompare(rowB.original.dateFrom),
      }),
      columnHelper.accessor((request) => formatDate(request.dateTo), {
        id: 'dateTo',
        header: t('requests.col.dateTo'),
        filterFn: diacriticsInsensitiveFilter,
        sortingFn: (rowA, rowB) => rowA.original.dateTo.localeCompare(rowB.original.dateTo),
      }),
      columnHelper.accessor('daysCount', {
        id: 'days',
        header: t('requests.col.days'),
        filterFn: diacriticsInsensitiveFilter,
        cell: (info) => <span className="font-semibold tabular-nums">{info.getValue()}</span>,
      }),
      columnHelper.accessor((request) => formatDateTime(request.submittedAt), {
        id: 'submittedAt',
        header: t('requests.col.submittedAt'),
        filterFn: diacriticsInsensitiveFilter,
        sortingFn: (rowA, rowB) =>
          rowA.original.submittedAt.localeCompare(rowB.original.submittedAt),
        cell: (info) => <span className="text-xs tabular-nums">{info.getValue()}</span>,
      }),
      columnHelper.accessor((request) => approvalStatusLabel(request.status, t), {
        id: 'status',
        header: t('requests.col.status'),
        filterFn: diacriticsInsensitiveFilter,
        cell: ({ row }) => <ManagerStatusBadge type="approval" status={row.original.status} />,
      }),
      columnHelper.display({
        id: 'actions',
        header: t('requests.col.actions'),
        enableSorting: false,
        enableColumnFilter: false,
        cell: ({ row }) => {
          const request = row.original
          return (
            <div className="flex items-center justify-end gap-2">
              <button
                type="button"
                onClick={() => onViewDetails(request)}
                className="inline-flex items-center gap-1 text-xs font-medium text-primary-600 transition-colors hover:text-primary-700 dark:text-primary-400 dark:hover:text-primary-300"
              >
                <ChevronRight className="h-3.5 w-3.5" />
                {t('requests.details')}
              </button>
              {request.status === 'pending' && (
                <>
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    disabled={isMutating}
                    onClick={() => onReject(request)}
                  >
                    {t('requests.reject')}
                  </Button>
                  <Button
                    type="button"
                    size="sm"
                    variant="primary"
                    disabled={isMutating}
                    onClick={() => onApprove(request.id)}
                  >
                    {t('requests.approve')}
                  </Button>
                </>
              )}
            </div>
          )
        },
      }),
    ],
    [isMutating, onApprove, onReject, onViewDetails, t]
  )

  const table = useReactTable({
    data: scopedRequests,
    columns,
    state: { sorting, columnFilters },
    onSortingChange: setSorting,
    onColumnFiltersChange: setColumnFilters,
    getCoreRowModel: getCoreRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    getSortedRowModel: getSortedRowModel(),
  })

  const visibleRequests = table.getRowModel().rows.map((row) => row.original)
  const pendingCount = scopedRequests.filter((request) => request.status === 'pending').length

  return (
    <div className="surface overflow-hidden">
      <div className="flex flex-col gap-3 border-b border-gray-100 px-5 py-4 dark:border-slate-800 sm:flex-row sm:items-center">
        <div className="flex items-center gap-2">
          <div className="rounded-lg bg-primary-50 p-2 dark:bg-primary-900/20">
            <ClipboardCheck className="h-5 w-5 text-primary-600 dark:text-primary-400" />
          </div>
          <div>
            <h2 className="font-semibold text-gray-900 dark:text-gray-100">
              {t(leaveOnly ? 'requests.leaveTitle' : 'requests.title')}
            </h2>
            <p className="text-xs text-gray-500 dark:text-gray-400">
              {t('requests.summary', { pending: pendingCount, total: scopedRequests.length })}
            </p>
          </div>
        </div>
        {leaveOnly && (
          <label className="flex cursor-pointer items-center gap-2 sm:ml-auto">
            <input
              type="checkbox"
              checked={conflictsOnly}
              onChange={(event) => setConflictsOnly(event.target.checked)}
              className="form-checkbox"
            />
            <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
              {t('requests.onlyConflicts')}
            </span>
          </label>
        )}
      </div>

      <div className="hidden overflow-x-auto xl:block">
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
                      } ${header.id === 'actions' ? 'text-right' : ''}`}
                    >
                      <div className={`flex items-center gap-2 ${header.id === 'actions' ? 'justify-end' : ''}`}>
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
                  className="border-b border-gray-50 transition-colors last:border-0 hover:bg-gray-50 dark:border-slate-800 dark:hover:bg-slate-800/50"
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

      <div className="divide-y divide-gray-100 dark:divide-slate-800 xl:hidden">
        {visibleRequests.map((request) => (
          <div key={request.id} className="space-y-3 px-4 py-4">
            <div className="flex items-start justify-between gap-3">
              <div>
                <div className="flex items-center gap-2 font-medium text-gray-900 dark:text-gray-100">
                  {request.hasConflict && <AlertTriangle className="h-4 w-4 text-amber-500" />}
                  {request.employeeName}
                </div>
                <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
                  {leaveTypeLabel(request.leaveType, t)}
                </p>
              </div>
              <ManagerStatusBadge type="approval" status={request.status} />
            </div>
            <div className="flex flex-wrap gap-x-4 gap-y-1 text-xs text-gray-500 dark:text-gray-400">
              <span>{formatDate(request.dateFrom)} - {formatDate(request.dateTo)}</span>
              <span>{request.daysCount} {t('common.days')}</span>
              <span>{t('requests.submittedShort')}: {formatDate(request.submittedAt)}</span>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              <Button type="button" size="sm" variant="ghost" onClick={() => onViewDetails(request)}>
                {t('requests.details')}
              </Button>
              {request.status === 'pending' && (
                <>
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    disabled={isMutating}
                    onClick={() => onReject(request)}
                  >
                    {t('requests.reject')}
                  </Button>
                  <Button
                    type="button"
                    size="sm"
                    variant="primary"
                    disabled={isMutating}
                    onClick={() => onApprove(request.id)}
                  >
                    {t('requests.approve')}
                  </Button>
                </>
              )}
            </div>
          </div>
        ))}
        {visibleRequests.length === 0 && (
          <p className="px-6 py-8 text-center text-sm text-gray-500 dark:text-gray-300">
            {t('table.noResults')}
          </p>
        )}
      </div>
    </div>
  )
}
