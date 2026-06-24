import { useI18n } from '@/shared/contexts/I18nContext'

export type LeaveRequestTab = 'new' | 'history'

interface TabBarProps {
  active: LeaveRequestTab
  onChange: (tab: LeaveRequestTab) => void
}

export function TabBar({ active, onChange }: TabBarProps) {
  const { t } = useI18n()

  const tabs: Array<{ id: LeaveRequestTab; label: string }> = [
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
