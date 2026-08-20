import type { ComponentType } from 'react'
import { BarChart3, ClipboardList } from 'lucide-react'
import { useHrAbsenceT, type HrAbsenceTranslationKey } from './hrAbsenceTranslations'

export type HrAbsenceTab = 'dashboard' | 'requests'

interface HrAbsenceTabsProps {
  active: HrAbsenceTab
  onChange: (tab: HrAbsenceTab) => void
}

const tabs: Array<{
  id: HrAbsenceTab
  label: HrAbsenceTranslationKey
  icon: ComponentType<{ className?: string }>
}> = [
  { id: 'dashboard', label: 'tabs.dashboard', icon: BarChart3 },
  { id: 'requests', label: 'tabs.allRequests', icon: ClipboardList },
]

export function HrAbsenceTabs({ active, onChange }: HrAbsenceTabsProps) {
  const t = useHrAbsenceT()

  return (
    <div className="surface p-1">
      <div className="grid grid-cols-1 gap-1 sm:grid-cols-2">
        {tabs.map((tab) => {
          const Icon = tab.icon
          const selected = active === tab.id

          return (
            <button
              key={tab.id}
              type="button"
              onClick={() => onChange(tab.id)}
              className={`flex min-h-11 items-center justify-center gap-2 rounded-xl px-3 py-2 text-sm font-medium transition-colors ${
                selected
                  ? 'bg-primary-600 text-white shadow-sm dark:bg-primary-500'
                  : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900 dark:text-gray-300 dark:hover:bg-slate-800 dark:hover:text-gray-100'
              }`}
            >
              <Icon className="h-4 w-4 flex-shrink-0" />
              <span>{t(tab.label)}</span>
            </button>
          )
        })}
      </div>
    </div>
  )
}
