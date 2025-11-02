// ContactsTable - Table with smart diacritics-insensitive search and expandable details
import React, { useMemo, useState, useEffect } from 'react'
import {
  useReactTable,
  getCoreRowModel,
  getFilteredRowModel,
  getSortedRowModel,
  getPaginationRowModel,
  flexRender,
  createColumnHelper,
  type ColumnFiltersState,
  type SortingState,
  type FilterFn,
  type ExpandedState,
  type VisibilityState
} from '@tanstack/react-table'
import removeAccents from 'remove-accents'
import type { ContactListItem } from '@/types/addressbook'
import { useI18n } from '@/contexts/I18nContext'
import {
  PencilIcon,
  TrashIcon,
  PlusCircleIcon,
  ChevronDownIcon,
  ChevronRightIcon,
  AdjustmentsHorizontalIcon
} from '@heroicons/react/24/outline'

// Custom filter function for diacritics-insensitive search
const diacriticsInsensitiveFilter: FilterFn<ContactListItem> = (row, columnId, filterValue) => {
  const value = row.getValue(columnId)
  if (value == null) return false
  
  const normalizedValue = removeAccents(String(value)).toLowerCase()
  const normalizedFilter = removeAccents(String(filterValue)).toLowerCase()
  
  return normalizedValue.includes(normalizedFilter)
}

// Global filter that searches across all columns with diacritics normalization
const globalDiacriticsFilter: FilterFn<ContactListItem> = (row, columnId, filterValue) => {
  const searchableValues = [
    row.original.firstName,
    row.original.lastName,
    row.original.displayName,
    row.original.email,
    row.original.department,
    row.original.position,
    row.original.location,
    row.original.mobilePhone
  ]
    .filter(Boolean)
    .join(' ')
  
  const normalizedSearchable = removeAccents(searchableValues).toLowerCase()
  const normalizedFilter = removeAccents(String(filterValue)).toLowerCase()
  
  return normalizedSearchable.includes(normalizedFilter)
}

interface ContactsTableProps {
  contacts: ContactListItem[]
  onEdit?: (contact: ContactListItem) => void
  onDelete?: (contact: ContactListItem) => void
  onProposeChange?: (contact: ContactListItem) => void
  canModify: boolean
  isAuthenticated: boolean
  loading?: boolean
}

const columnHelper = createColumnHelper<ContactListItem>()

export const ContactsTable: React.FC<ContactsTableProps> = ({
  contacts,
  onEdit,
  onDelete,
  onProposeChange,
  canModify,
  isAuthenticated,
  loading = false
}) => {
  const { t } = useI18n()
  const [sorting, setSorting] = useState<SortingState>([])
  const [columnFilters, setColumnFilters] = useState<ColumnFiltersState>([])
  const [globalFilter, setGlobalFilter] = useState('')
  const [expanded, setExpanded] = useState<ExpandedState>({})
  const [showColumnConfig, setShowColumnConfig] = useState(false)
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>(() => {
    const saved = localStorage.getItem('addressbook-column-visibility')
    return saved ? JSON.parse(saved) : {
      firstName: true,
      lastName: true,
      email: true,
      mobilePhone: true,
      department: true,
      position: true
    }
  })

  // Persist column visibility to localStorage
  useEffect(() => {
    localStorage.setItem('addressbook-column-visibility', JSON.stringify(columnVisibility))
  }, [columnVisibility])

  const columns = useMemo(
    () => [
      columnHelper.display({
        id: 'expand',
        header: '',
        size: 50,
        cell: ({ row }) => (
          <button
            onClick={() => row.toggleExpanded()}
            className="p-1 hover:bg-gray-100 rounded transition-colors"
            aria-label={t('addressBook.table.details')}
          >
            {row.getIsExpanded() ? (
              <ChevronDownIcon className="w-5 h-5 text-gray-600" />
            ) : (
              <ChevronRightIcon className="w-5 h-5 text-gray-600" />
            )}
          </button>
        )
      }),
      columnHelper.accessor('firstName', {
        header: t('addressBook.table.firstName'),
        cell: (info) => (
          <span className="font-medium text-gray-900">{info.getValue()}</span>
        ),
        filterFn: diacriticsInsensitiveFilter
      }),
      columnHelper.accessor('lastName', {
        header: t('addressBook.table.lastName'),
        cell: (info) => (
          <span className="font-medium text-gray-900">{info.getValue()}</span>
        ),
        filterFn: diacriticsInsensitiveFilter
      }),
      columnHelper.accessor('email', {
        header: t('addressBook.table.email'),
        cell: (info) => (
          info.getValue() ? (
            <a
              href={`mailto:${info.getValue()}`}
              className="text-blue-600 hover:underline truncate block max-w-xs"
            >
              {info.getValue()}
            </a>
          ) : (
            <span className="text-gray-400">-</span>
          )
        ),
        filterFn: diacriticsInsensitiveFilter
      }),
      columnHelper.accessor('mobilePhone', {
        header: t('addressBook.table.mobilePhone'),
        cell: (info) => (
          info.getValue() ? (
            <a
              href={`tel:${info.getValue()}`}
              className="text-blue-600 hover:underline whitespace-nowrap"
            >
              {info.getValue()}
            </a>
          ) : (
            <span className="text-gray-400">-</span>
          )
        ),
        filterFn: diacriticsInsensitiveFilter
      }),
      columnHelper.accessor('department', {
        header: t('addressBook.table.department'),
        cell: (info) => (
          <span className="text-gray-700">{info.getValue() || '-'}</span>
        ),
        filterFn: diacriticsInsensitiveFilter
      }),
      columnHelper.accessor('position', {
        header: t('addressBook.table.position'),
        cell: (info) => (
          <span className="text-gray-700">{info.getValue() || '-'}</span>
        ),
        filterFn: diacriticsInsensitiveFilter
      }),
      columnHelper.display({
        id: 'actions',
        header: t('addressBook.table.actions'),
        size: 120,
        cell: ({ row }) => (
          <div className="flex items-center gap-1">
            {canModify ? (
              <>
                <button
                  onClick={() => onEdit?.(row.original)}
                  className="p-2 text-blue-600 hover:bg-blue-50 rounded transition-colors"
                  title={t('addressBook.actions.edit')}
                  aria-label={t('addressBook.actions.edit')}
                >
                  <PencilIcon className="w-5 h-5" />
                </button>
                <button
                  onClick={() => {
                    if (confirm(t('addressBook.deleteConfirm'))) {
                      onDelete?.(row.original)
                    }
                  }}
                  className="p-2 text-red-600 hover:bg-red-50 rounded transition-colors"
                  title={t('addressBook.actions.delete')}
                  aria-label={t('addressBook.actions.delete')}
                >
                  <TrashIcon className="w-5 h-5" />
                </button>
              </>
            ) : (
              isAuthenticated && (
                <button
                  onClick={() => onProposeChange?.(row.original)}
                  className="p-2 text-green-600 hover:bg-green-50 rounded transition-colors"
                  title={t('addressBook.actions.propose')}
                  aria-label={t('addressBook.actions.propose')}
                >
                  <PlusCircleIcon className="w-5 h-5" />
                </button>
              )
            )}
          </div>
        )
      })
    ],
    [canModify, isAuthenticated, onEdit, onDelete, onProposeChange, t]
  )

  const table = useReactTable({
    data: contacts,
    columns,
    state: {
      sorting,
      columnFilters,
      globalFilter,
      expanded,
      columnVisibility
    },
    onSortingChange: setSorting,
    onColumnFiltersChange: setColumnFilters,
    onGlobalFilterChange: setGlobalFilter,
    onExpandedChange: setExpanded,
    onColumnVisibilityChange: setColumnVisibility,
    getCoreRowModel: getCoreRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    globalFilterFn: globalDiacriticsFilter,
    getRowCanExpand: () => true,
    initialState: {
      pagination: {
        pageSize: 20
      }
    }
  })

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" />
        <span className="ml-3 text-gray-600">{t('addressBook.loading')}</span>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-4">
        <input
          type="text"
          value={globalFilter ?? ''}
          onChange={(e) => setGlobalFilter(e.target.value)}
          placeholder={t('addressBook.search')}
          className="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
        />
        <div className="relative">
          <button
            onClick={() => setShowColumnConfig(!showColumnConfig)}
            className="flex items-center gap-2 px-4 py-2 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
            title={t('addressBook.table.configureColumns')}
          >
            <AdjustmentsHorizontalIcon className="w-5 h-5 text-gray-600" />
            <span className="hidden sm:inline">{t('addressBook.table.configureColumns')}</span>
          </button>
          {showColumnConfig && (
            <div className="absolute right-0 mt-2 w-64 bg-white rounded-lg shadow-lg border border-gray-200 z-10 p-4">
              <div className="space-y-2">
                {['firstName', 'lastName', 'email', 'mobilePhone', 'department', 'position'].map((columnId) => (
                  <label key={columnId} className="flex items-center gap-2 cursor-pointer hover:bg-gray-50 p-2 rounded">
                    <input
                      type="checkbox"
                      checked={columnVisibility[columnId] !== false}
                      onChange={(e) => {
                        setColumnVisibility(prev => ({
                          ...prev,
                          [columnId]: e.target.checked
                        }))
                      }}
                      className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                    />
                    <span className="text-sm text-gray-700">
                      {t(`addressBook.table.${columnId}` as keyof typeof t)}
                    </span>
                  </label>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>

      <div className="overflow-x-auto bg-white rounded-lg shadow address-book-table">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            {table.getHeaderGroups().map((headerGroup) => (
              <React.Fragment key={headerGroup.id}>
                <tr>
                  {headerGroup.headers.map((header) => (
                    <th
                      key={header.id}
                      onClick={header.column.getToggleSortingHandler()}
                      className={`px-3 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider ${header.column.getCanSort() ? 'cursor-pointer select-none hover:bg-gray-100' : ''} ${header.id === 'expand' ? 'w-12' : ''} ${header.id === 'actions' ? 'w-32' : ''}`}
                    >
                      <div className="flex items-center gap-2">
                        {flexRender(header.column.columnDef.header, header.getContext())}
                        {header.column.getCanSort() && (
                          <span className="text-gray-400">
                            {{
                              asc: ' ↑',
                              desc: ' ↓'
                            }[header.column.getIsSorted() as string] ?? '↕'}
                          </span>
                        )}
                      </div>
                    </th>
                  ))}
                </tr>
                <tr>
                  {headerGroup.headers.map((header) => (
                    <th key={`filter-${header.id}`} className="px-3 py-2 bg-gray-100">
                      {header.column.getCanFilter() ? (
                        <input
                          type="text"
                          value={(header.column.getFilterValue() ?? '') as string}
                          onChange={(e) => header.column.setFilterValue(e.target.value)}
                          placeholder={`${t('addressBook.table.searchIn')} ${typeof header.column.columnDef.header === 'string' ? header.column.columnDef.header.toLowerCase() : ''}...`}
                          className="w-full px-2 py-1 text-xs border border-gray-300 rounded focus:ring-1 focus:ring-blue-500 focus:border-blue-500"
                          onClick={(e) => e.stopPropagation()}
                        />
                      ) : null}
                    </th>
                  ))}
                </tr>
              </React.Fragment>
            ))}
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {table.getRowModel().rows.length === 0 ? (
              <tr>
                <td colSpan={columns.length} className="px-6 py-8 text-center text-gray-500">
                  {globalFilter ? t('addressBook.table.noResults') : t('addressBook.noContacts')}
                </td>
              </tr>
            ) : (
              table.getRowModel().rows.map((row) => (
                <React.Fragment key={row.id}>
                  <tr className="hover:bg-gray-50 transition-colors">
                    {row.getVisibleCells().map((cell) => (
                      <td key={cell.id} className="px-3 py-3 whitespace-nowrap text-sm">
                        {flexRender(cell.column.columnDef.cell, cell.getContext())}
                      </td>
                    ))}
                  </tr>
                  {row.getIsExpanded() && (
                    <tr className="bg-gray-50">
                      <td colSpan={columns.length} className="px-6 py-4">
                        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 text-sm">
                          {row.original.displayName && (
                            <div>
                              <span className="font-medium text-gray-700">{t('addressBook.table.displayName')}:</span>
                              <span className="ml-2 text-gray-900">{row.original.displayName}</span>
                            </div>
                          )}
                          {row.original.mobilePhone && (
                            <div>
                              <span className="font-medium text-gray-700">{t('addressBook.table.mobile')}:</span>
                              <a href={`tel:${row.original.mobilePhone}`} className="ml-2 text-blue-600 hover:underline">
                                {row.original.mobilePhone}
                              </a>
                            </div>
                          )}
                          {row.original.location && (
                            <div>
                              <span className="font-medium text-gray-700">{t('addressBook.table.location')}:</span>
                              <span className="ml-2 text-gray-900">{row.original.location}</span>
                            </div>
                          )}
                          <div>
                            <span className="font-medium text-gray-700">{t('addressBook.table.status')}:</span>
                            <span className={`ml-2 px-2 py-1 rounded text-xs font-medium ${row.original.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'}`}>
                              {row.original.isActive ? t('addressBook.table.active') : t('addressBook.table.inactive')}
                            </span>
                          </div>
                        </div>
                      </td>
                    </tr>
                  )}
                </React.Fragment>
              ))
            )}
          </tbody>
        </table>
      </div>

      <div className="flex flex-col sm:flex-row items-center justify-between gap-4 px-2">
        <div className="flex items-center gap-2 text-sm text-gray-700">
          <span>{t('addressBook.table.rowsPerPage')}:</span>
          <select
            value={table.getState().pagination.pageSize}
            onChange={(e) => table.setPageSize(Number(e.target.value))}
            className="px-2 py-1 border border-gray-300 rounded focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            aria-label={t('addressBook.table.rowsPerPage')}
          >
            {[10, 20, 50, 100].map((pageSize) => (
              <option key={pageSize} value={pageSize}>{pageSize}</option>
            ))}
          </select>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={() => table.setPageIndex(0)}
            disabled={!table.getCanPreviousPage()}
            className="px-3 py-1 text-sm border border-gray-300 rounded hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {'<<'}
          </button>
          <button
            onClick={() => table.previousPage()}
            disabled={!table.getCanPreviousPage()}
            className="px-3 py-1 text-sm border border-gray-300 rounded hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {'<'}
          </button>
          <span className="px-3 py-1 text-sm text-gray-700">
            {table.getState().pagination.pageIndex + 1} {t('addressBook.table.of')} {table.getPageCount()}
          </span>
          <button
            onClick={() => table.nextPage()}
            disabled={!table.getCanNextPage()}
            className="px-3 py-1 text-sm border border-gray-300 rounded hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {'>'}
          </button>
          <button
            onClick={() => table.setPageIndex(table.getPageCount() - 1)}
            disabled={!table.getCanNextPage()}
            className="px-3 py-1 text-sm border border-gray-300 rounded hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {'>>'}
          </button>
        </div>

        <div className="text-sm text-gray-700">
          {table.getFilteredRowModel().rows.length} {t('addressBook.table.totalContacts')}
        </div>
      </div>

      <style>{`
        @media (max-width: 768px) {
          .address-book-table table {
            font-size: 0.875rem;
          }
          
          .address-book-table th,
          .address-book-table td {
            padding: 0.5rem !important;
          }
          
          /* Hide department and position on tablets */
          .address-book-table th:nth-child(6),
          .address-book-table td:nth-child(6),
          .address-book-table th:nth-child(7),
          .address-book-table td:nth-child(7) {
            display: none;
          }
        }
        
        @media (max-width: 640px) {
          /* Hide email and mobilePhone on phones */
          .address-book-table th:nth-child(4),
          .address-book-table td:nth-child(4),
          .address-book-table th:nth-child(5),
          .address-book-table td:nth-child(5) {
            display: none;
          }
        }
      `}</style>
    </div>
  )
}
