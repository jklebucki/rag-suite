import { useEffect, useRef, useState } from 'react'
import { Building2, Check, ChevronDown } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { TranslationKeys } from '@/shared/types/i18n'
import { useEmploymentContext } from '../hooks/useEmploymentContext'
import { employmentStatusLabel, formatEmploymentDate } from '../services/employmentContextUtils'

export function EmploymentContextDropdown() {
  const { t } = useI18n()
  const {
    contexts,
    activeContext,
    activeContextId,
    isLoading,
    error,
    setActiveContextId,
  } = useEmploymentContext()
  const [isOpen, setIsOpen] = useState(false)
  const dropdownRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside)
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [isOpen])

  const selectContext = (contextId: string) => {
    setActiveContextId(contextId)
    setIsOpen(false)
  }

  if (isLoading) {
    return (
      <div className="inline-flex min-h-9 items-center gap-2 rounded-lg border border-gray-200 bg-white px-2.5 py-1.5 text-xs text-gray-500 dark:border-slate-800 dark:bg-slate-900 dark:text-gray-400">
        <div className="h-3.5 w-3.5 animate-spin rounded-full border-2 border-primary-200 border-t-primary-600" />
        {t('employeeDashboard.employmentContext.loading')}
      </div>
    )
  }

  if (error || !activeContext) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 px-2.5 py-1.5 text-xs text-red-700 dark:border-red-900/40 dark:bg-red-900/10 dark:text-red-300">
        {error ? t(error as keyof TranslationKeys) : t('employeeDashboard.employmentContext.error.noData')}
      </div>
    )
  }

  return (
    <div className="relative w-full sm:w-auto" ref={dropdownRef}>
      <button
        type="button"
        onClick={() => setIsOpen((value) => !value)}
        className="flex min-h-9 w-full items-center justify-between gap-2 rounded-lg border border-gray-200 bg-white px-2.5 py-1.5 text-left shadow-sm transition-colors hover:bg-gray-50 dark:border-slate-800 dark:bg-slate-900 dark:hover:bg-slate-800 sm:min-w-[320px]"
        aria-expanded={isOpen}
        aria-haspopup="listbox"
      >
        <span className="flex min-w-0 items-center gap-2">
          <span className="rounded-md bg-primary-50 p-1.5 dark:bg-primary-900/20">
            <Building2 className="h-3.5 w-3.5 text-primary-600 dark:text-primary-400" />
          </span>
          <span className="min-w-0">
            <span className="block text-[10px] font-medium leading-3 text-gray-500 dark:text-gray-400">
              {t('employeeDashboard.employmentContext.label')}
            </span>
            <span className="block truncate text-sm font-semibold leading-5 text-gray-900 dark:text-gray-100">
              {activeContext.companyName} - {activeContext.position}
            </span>
          </span>
        </span>
        <ChevronDown className={`h-3.5 w-3.5 flex-shrink-0 text-gray-500 transition-transform ${isOpen ? 'rotate-180' : ''}`} />
      </button>

      {isOpen && (
        <div
          role="listbox"
          className="absolute right-0 z-40 mt-2 w-full min-w-[min(26rem,calc(100vw-2rem))] overflow-hidden rounded-xl border border-gray-200 bg-white shadow-lg dark:border-slate-800 dark:bg-slate-900 sm:w-[28rem]"
        >
          <div className="border-b border-gray-100 px-4 py-3 dark:border-slate-800">
            <p className="text-sm font-semibold text-gray-900 dark:text-gray-100">
              {t('employeeDashboard.employmentContext.menuTitle')}
            </p>
            <p className="mt-0.5 text-xs text-gray-500 dark:text-gray-400">
              {t('employeeDashboard.employmentContext.mockNotice')}
            </p>
          </div>

          <div className="max-h-80 overflow-y-auto py-1">
            {contexts.map((context) => {
              const selected = context.id === activeContextId

              return (
                <button
                  key={context.id}
                  type="button"
                  role="option"
                  aria-selected={selected}
                  onClick={() => selectContext(context.id)}
                  className={`flex w-full items-start gap-3 px-4 py-3 text-left transition-colors ${
                    selected
                      ? 'bg-primary-50 dark:bg-primary-900/20'
                      : 'hover:bg-gray-50 dark:hover:bg-slate-800'
                  }`}
                >
                  <span className="mt-0.5 rounded-lg bg-gray-100 p-2 dark:bg-slate-800">
                    <Building2 className="h-4 w-4 text-gray-500 dark:text-gray-300" />
                  </span>
                  <span className="min-w-0 flex-1">
                    <span className="block font-semibold text-gray-900 dark:text-gray-100">
                      {context.companyName}
                    </span>
                    <span className="mt-0.5 block text-sm text-gray-600 dark:text-gray-300">
                      {context.position} · {employmentStatusLabel(context.status, t)} ·{' '}
                      {t('employeeDashboard.employmentContext.since', {
                        date: formatEmploymentDate(context.startDate),
                      })}
                    </span>
                    <span className="mt-1 block text-xs text-gray-500 dark:text-gray-400">
                      {context.companyCode} · {context.employmentType}
                    </span>
                  </span>
                  {selected && <Check className="mt-1 h-4 w-4 text-primary-600 dark:text-primary-400" />}
                </button>
              )
            })}
          </div>
        </div>
      )}
    </div>
  )
}
