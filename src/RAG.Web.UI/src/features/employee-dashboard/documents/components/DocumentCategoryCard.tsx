import type { ComponentType } from 'react'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { DocumentCategory, DocumentCategoryId } from '../types/documentsTypes'
import { getCategoryDescription } from '../services/documentsUtils'

interface DocumentCategoryCardProps {
  category: DocumentCategory
  icon: ComponentType<{ className?: string }>
  count: number
  selected: boolean
  onSelect: (categoryId: DocumentCategoryId) => void
}

export function DocumentCategoryCard({
  category,
  icon: Icon,
  count,
  selected,
  onSelect,
}: DocumentCategoryCardProps) {
  const { t } = useI18n()

  return (
    <button
      type="button"
      onClick={() => onSelect(category.id)}
      className={`surface p-4 text-left transition-colors hover:border-primary-200 dark:hover:border-primary-800 ${
        selected ? 'border-primary-300 bg-primary-50/60 dark:border-primary-800 dark:bg-primary-900/10' : ''
      }`}
    >
      <div className="flex items-start gap-3">
        <div className="rounded-lg bg-primary-50 p-2 dark:bg-primary-900/20">
          <Icon className="h-5 w-5 text-primary-600 dark:text-primary-400" />
        </div>
        <div className="min-w-0 flex-1">
          <div className="flex items-center justify-between gap-3">
            <h2 className="font-semibold text-gray-900 dark:text-gray-100">{t(category.nameKey)}</h2>
            <span className="rounded-full bg-gray-100 px-2 py-0.5 text-xs font-medium text-gray-600 dark:bg-slate-800 dark:text-gray-300">
              {count}
            </span>
          </div>
          <p className="mt-1 text-xs leading-5 text-gray-500 dark:text-gray-400">
            {getCategoryDescription(category, t)}
          </p>
        </div>
      </div>
    </button>
  )
}
