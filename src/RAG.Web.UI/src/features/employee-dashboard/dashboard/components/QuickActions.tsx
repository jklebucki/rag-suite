import React from 'react'
import {
  CalendarDays,
  Download,
  History,
  FolderOpen,
  UserCog,
  Zap,
} from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { useI18n } from '@/shared/contexts/I18nContext'

interface QuickActionItem {
  icon: React.ComponentType<{ className?: string }>
  labelKey: string
  colorClass: string
  bgClass: string
  onClick?: () => void
}

export function QuickActions() {
  const { t } = useI18n()
  const navigate = useNavigate()

  const actions: QuickActionItem[] = [
    {
      icon: CalendarDays,
      labelKey: 'employeeDashboard.quickActions.submitLeave',
      colorClass: 'text-green-700 dark:text-green-300',
      bgClass: 'bg-green-50 hover:bg-green-100 dark:bg-green-900/20 dark:hover:bg-green-900/40',
      onClick: () => navigate('/employee-dashboard/leave'),
    },
    {
      icon: Download,
      labelKey: 'employeeDashboard.quickActions.downloadPayslip',
      colorClass: 'text-blue-700 dark:text-blue-300',
      bgClass: 'bg-blue-50 hover:bg-blue-100 dark:bg-blue-900/20 dark:hover:bg-blue-900/40',
      onClick: () => navigate('/employee-dashboard/salary'),
    },
    {
      icon: History,
      labelKey: 'employeeDashboard.quickActions.hrHistory',
      colorClass: 'text-purple-700 dark:text-purple-300',
      bgClass: 'bg-purple-50 hover:bg-purple-100 dark:bg-purple-900/20 dark:hover:bg-purple-900/40',
      onClick: () => navigate('/employee-dashboard/leave'),
    },
    {
      icon: FolderOpen,
      labelKey: 'employeeDashboard.quickActions.documents',
      colorClass: 'text-amber-700 dark:text-amber-300',
      bgClass: 'bg-amber-50 hover:bg-amber-100 dark:bg-amber-900/20 dark:hover:bg-amber-900/40',
      onClick: () => navigate('/employee-dashboard/documents'),
    },
    {
      icon: UserCog,
      labelKey: 'employeeDashboard.quickActions.updatePersonalData',
      colorClass: 'text-rose-700 dark:text-rose-300',
      bgClass: 'bg-rose-50 hover:bg-rose-100 dark:bg-rose-900/20 dark:hover:bg-rose-900/40',
      onClick: () => navigate('/employee-dashboard/personal'),
    },
  ]

  return (
    <div className="surface p-5 flex flex-col gap-3">
      <div className="flex items-center gap-2">
        <div className="p-2 bg-primary-50 dark:bg-primary-900/20 rounded-lg">
          <Zap className="h-5 w-5 text-primary-600 dark:text-primary-400" />
        </div>
        <h2 className="font-semibold text-gray-900 dark:text-gray-100">
          {t('employeeDashboard.quickActions.title')}
        </h2>
      </div>

      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-2">
        {actions.map(({ icon: Icon, labelKey, colorClass, bgClass, onClick }) => (
          <button
            key={labelKey}
            onClick={onClick}
            className={`flex flex-col items-center gap-2 p-4 rounded-xl border border-transparent transition-all cursor-pointer text-center focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 ${bgClass}`}
            type="button"
          >
            <div className={`p-2 rounded-lg bg-white/60 dark:bg-slate-900/40`}>
              <Icon className={`h-5 w-5 ${colorClass}`} />
            </div>
            <span className={`text-xs font-medium leading-tight ${colorClass}`}>
              {t(labelKey as Parameters<typeof t>[0])}
            </span>
          </button>
        ))}
      </div>
    </div>
  )
}
