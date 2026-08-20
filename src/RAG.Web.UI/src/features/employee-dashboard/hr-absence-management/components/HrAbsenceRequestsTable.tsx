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
import { AlertTriangle, CalendarClock, ClipboardList } from 'lucide-react'
import removeAccents from 'remove-accents'
import { ManagerStatusBadge } from '../../manager-panel/components/ManagerStatusBadge'
import type { HrAbsenceRequest, HrAbsenceRequestFilters } from '../types/hrAbsenceTypes'
import {
  formatHrAbsenceDate,
  formatHrAbsenceDateTime,
  hrAbsenceActionLabel,
  hrAbsenceLeaveTypeLabel,
  hrAbsenceStatusLabel,
  isHrAbsenceOverdue,
} from './hrAbsenceUtils'
import { useHrAbsenceT } from './hrAbsenceTranslations'

interface HrAbsenceRequestsTableProps {
  requests: HrAbsenceRequest[]
  initialFilters: HrAbsenceRequestFilters
}

const columnHelper = createColumnHelper<HrAbsenceRequest>()

const diacriticsInsensitiveFilter: FilterFn<HrAbsenceRequest> = (row, columnId, filterValue) => {
  const value = row.getValue(columnId)
  if (value == null) return false

  return removeAccents(String(value)).toLowerCase().includes(
    removeAccents(String(filterValue)).toLowerCase()
  )
}

export function HrAbsenceRequestsTable({
  requests,
  initialFilters,
}: HrAbsenceRequestsTableProps) {
  const t = useHrAbsenceT()
  const [sorting, setSorting] = useState<SortingState>([{ id: 'submittedAt', desc: true }])
  const [columnFilters, setColumnFilters] = useState<ColumnFiltersState>(() =>
    initialFilters.status
      ? [{ id: 'status', value: hrAbsenceStatusLabel(initialFilters.status, t) }]
      : []
  )
  const [payrollClosureOnly, setPayrollClosureOnly] = useState(
    initialFilters.category === 'payrollClosure'
  )
  const [overdueOnly, setOverdueOnly] = useState(initialFilters.category === 'overdue')

  const categoryScopedRequests = useMemo(
    () =>
      requests.filter(
        (request) =>
          (!payrollClosureOnly || request.requiresPayrollClosure) &&
          (!overdueOnly || isHrAbsenceOverdue(request))
      ),
    [overdueOnly, payrollClosureOnly, requests]
  )

  const columns = useMemo(
    () => [
      columnHelper.accessor('employeeName', {
        id: 'employee',
        header: t('requests.col.employee'),
        filterFn: diacriticsInsensitiveFilter,
        cell: (info) => (
          <span className="font-medium text-gray-900 dark:text-gray-100">{info.getValue()}</span>
        ),
      }),
      columnHelper.accessor((request) => hrAbsenceLeaveTypeLabel(request.leaveType, t), {
        id: 'leaveType',
        header: t('requests.col.leaveType'),
        filterFn: diacriticsInsensitiveFilter,
      }),
      columnHelper.accessor((request) => formatHrAbsenceDate(request.dateFrom), {
        id: 'dateFrom',
        header: t('requests.col.dateFrom'),
        filterFn: diacriticsInsensitiveFilter,
        sortingFn: (rowA, rowB) => rowA.original.dateFrom.localeCompare(rowB.original.dateFrom),
      }),
      columnHelper.accessor((request) => formatHrAbsenceDate(request.dateTo), {
        id: 'dateTo',
        header: t('requests.col.dateTo'),
        filterFn: diacriticsInsensitiveFilter,
        sortingFn: (rowA, rowB) => rowA.original.dateTo.localeCompare(rowB.original.dateTo),
      }),
      columnHelper.accessor('days', {
        header: t('requests.col.days'),
        filterFn: diacriticsInsensitiveFilter,
        cell: (info) => <span className="font-semibold tabular-nums">{info.getValue()}</span>,
      }),
      columnHelper.accessor((request) => formatHrAbsenceDateTime(request.submittedAt), {
        id: 'submittedAt',
        header: t('requests.col.submittedAt'),
        filterFn: diacriticsInsensitiveFilter,
        sortingFn: (rowA, rowB) =>
          rowA.original.submittedAt.localeCompare(rowB.original.submittedAt),
        cell: (info) => <span className="text-xs tabular-nums">{info.getValue()}</span>,
      }),
      columnHelper.accessor('managerName', {
        id: 'manager',
        header: t('requests.col.manager'),
        filterFn: diacriticsInsensitiveFilter,
      }),
      columnHelper.accessor((request) => hrAbsenceStatusLabel(request.status, t), {
        id: 'status',
        header: t('requests.col.status'),
        filterFn: diacriticsInsensitiveFilter,
        cell: ({ row }) => <ManagerStatusBadge type="approval" status={row.original.status} />,
      }),
      columnHelper.accessor((request) => hrAbsenceActionLabel(request, t), {
        id: 'action',
        header: t('requests.col.action'),
        filterFn: diacriticsInsensitiveFilter,
        cell: ({ row }) => {
          const overdue = isHrAbsenceOverdue(row.original)
          const payrollClosure = row.original.requiresPayrollClosure

          return (
            <span
              className={`inline-flex items-center gap-1 text-xs font-medium ${
                overdue
                  ? 'text-red-600 dark:text-red-400'
                  : payrollClosure
                    ? 'text-amber-600 dark:text-amber-400'
                    : 'text-gray-500 dark:text-gray-400'
              }`}
            >
              {overdue && <AlertTriangle className="h-3.5 w-3.5" />}
              {!overdue && payrollClosure && <CalendarClock className="h-3.5 w-3.5" />}
              {hrAbsenceActionLabel(row.original, t)}
            </span>
          )
        },
      }),
    ],
    [t]
  )

  const table = useReactTable({
    data: categoryScopedRequests,
    columns,
    state: { sorting, columnFilters },
    onSortingChange: setSorting,
    onColumnFiltersChange: setColumnFilters,
    getCoreRowModel: getCoreRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    getSortedRowModel: getSortedRowModel(),
  })

  const visibleRequests = table.getRowModel().rows.map((row) => row.original)

  return (
    <div className="surface overflow-hidden">
      <div className="flex flex-col gap-3 border-b border-gray-100 px-5 py-4 dark:border-slate-800 lg:flex-row lg:items-center">
        <div className="flex items-center gap-2">
          <div className="rounded-lg bg-primary-50 p-2 dark:bg-primary-900/20">
            <ClipboardList className="h-5 w-5 text-primary-600 dark:text-primary-400" />
          </div>
          <div>
            <h2 className="font-semibold text-gray-900 dark:text-gray-100">
              {t('requests.title')}
            </h2>
            <p className="text-xs text-gray-500 dark:text-gray-400">
              {t('requests.summary', {
                visible: visibleRequests.length,
                total: requests.length,
              })}
            </p>
          </div>
        </div>
        <div className="flex flex-col gap-2 lg:ml-auto sm:flex-row sm:flex-wrap">
          <label className="flex cursor-pointer items-center gap-2">
            <input
              type="checkbox"
              checked={payrollClosureOnly}
              onChange={(event) => setPayrollClosureOnly(event.target.checked)}
              className="form-checkbox"
            />
            <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
              {t('requests.onlyPayrollClosure')}
            </span>
          </label>
          <label className="flex cursor-pointer items-center gap-2">
            <input
              type="checkbox"
              checked={overdueOnly}
              onChange={(event) => setOverdueOnly(event.target.checked)}
              className="form-checkbox"
            />
            <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
              {t('requests.onlyOverdue')}
            </span>
          </label>
        </div>
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
                      }`}
                    >
                      <div className="flex items-center gap-2">
                        {flexRender(header.column.columnDef.header, header.getContext())}
                        {header.column.getCanSort() && (
                          <span className="text-gray-400 dark:text-gray-500">
                            {{ asc: '↑', desc: '↓' }[
                              header.column.getIsSorted() as string
                            ] ?? '↕'}
                          </span>
                        )}
                      </div>
                    </th>
                  ))}
                </tr>
                <tr>
                  {headerGroup.headers.map((header) => (
                    <th
                      key={`filter-${header.id}`}
                      className="bg-gray-100 px-3 py-2 dark:bg-slate-800/60"
                    >
                      {header.column.getCanFilter() && (
                        <input
                          type="text"
                          value={(header.column.getFilterValue() ?? '') as string}
                          onChange={(event) => header.column.setFilterValue(event.target.value)}
                          placeholder={`${t('table.searchIn')} ${String(
                            header.column.columnDef.header
                          ).toLowerCase()}...`}
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
                <td
                  colSpan={columns.length}
                  className="px-6 py-8 text-center text-gray-500 dark:text-gray-300"
                >
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
                <p className="font-medium text-gray-900 dark:text-gray-100">
                  {request.employeeName}
                </p>
                <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
                  {hrAbsenceLeaveTypeLabel(request.leaveType, t)} · {request.managerName}
                </p>
              </div>
              <ManagerStatusBadge type="approval" status={request.status} />
            </div>
            <div className="flex flex-wrap gap-x-4 gap-y-1 text-xs text-gray-500 dark:text-gray-400">
              <span>
                {formatHrAbsenceDate(request.dateFrom)} - {formatHrAbsenceDate(request.dateTo)}
              </span>
              <span>
                {request.days} {t('common.days')}
              </span>
              <span>
                {t('requests.submittedShort')}: {formatHrAbsenceDate(request.submittedAt)}
              </span>
            </div>
            <p className="text-xs font-medium text-gray-600 dark:text-gray-300">
              {hrAbsenceActionLabel(request, t)}
            </p>
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
