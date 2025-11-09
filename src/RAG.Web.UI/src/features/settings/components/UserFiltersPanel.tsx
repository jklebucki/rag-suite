// All code comments must be written in English, regardless of the conversation language.

import { useI18n } from '@/shared/contexts/I18nContext'
import type { UserFilters } from '@/features/settings/types/settings'

interface UserFiltersProps {
  filters: UserFilters
  onFiltersChange: (filters: UserFilters) => void
  availableRoles: string[]
  onClear: () => void
  onApplyDatePreset: (field: 'created' | 'lastLogin', days: number) => void
}

export function UserFiltersPanel({
  filters,
  onFiltersChange,
  availableRoles,
  onClear,
  onApplyDatePreset
}: UserFiltersProps) {
  const { t } = useI18n()

  const updateFilter = (key: keyof UserFilters, value: string | string[] | Date | null) => {
    onFiltersChange({ ...filters, [key]: value })
  }

  const toggleRole = (role: string) => {
    const newRoles = filters.selectedRoles.includes(role)
      ? filters.selectedRoles.filter(r => r !== role)
      : [...filters.selectedRoles, role]
    updateFilter('selectedRoles', newRoles)
  }

  return (
    <div className="px-6 py-4 border-b border-gray-200 dark:border-slate-700 surface-muted">
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {/* Role Filter */}
        <div>
          <p className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-2" id="roles-heading">
            {t('settings.user.filters.roles')}
          </p>
          <div className="space-y-2 max-h-32 overflow-y-auto pr-1">
            {availableRoles.map((role) => (
              <label key={role} className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-200">
                <input
                  type="checkbox"
                  checked={filters.selectedRoles.includes(role)}
                  onChange={() => toggleRole(role)}
                  className="form-checkbox h-5 w-9"
                />
                <span>{role}</span>
              </label>
            ))}
          </div>
        </div>

        {/* Active Status Filter */}
        <div>
          <p className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-2" id="status-heading">
            {t('settings.user.filters.status')}
          </p>
          <div className="space-y-2">
            {[
              { value: 'all', label: t('settings.user.filters.status_all') },
              { value: 'active', label: t('settings.user.filters.status_active') },
              { value: 'inactive', label: t('settings.user.filters.status_inactive') }
            ].map((option) => (
              <label key={option.value} className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-200">
                <input
                  type="radio"
                  name="activeStatus"
                  value={option.value}
                  checked={filters.activeStatus === option.value}
                  onChange={(e) => updateFilter('activeStatus', e.target.value as 'all' | 'active' | 'inactive')}
                  className="h-4 w-4 text-primary-600 focus:ring-primary-500 border-gray-300 dark:border-slate-600 dark:bg-slate-800"
                />
                <span>{option.label}</span>
              </label>
            ))}
          </div>
        </div>

        {/* Created Date Filter */}
        <DateFilter
          label={t('settings.user.filters.created')}
          fromValue={filters.createdDateFrom}
          toValue={filters.createdDateTo}
          onFromChange={(value) => updateFilter('createdDateFrom', value)}
          onToChange={(value) => updateFilter('createdDateTo', value)}
          onPreset={(days) => onApplyDatePreset('created', days)}
        />

        {/* Last Login Filter */}
        <DateFilter
          label={t('settings.user.filters.last_login')}
          fromValue={filters.lastLoginFrom}
          toValue={filters.lastLoginTo}
          onFromChange={(value) => updateFilter('lastLoginFrom', value)}
          onToChange={(value) => updateFilter('lastLoginTo', value)}
          onPreset={(days) => onApplyDatePreset('lastLogin', days)}
        />
      </div>

      {/* Filter Actions */}
      <div className="flex justify-end gap-3 pt-4 border-t border-gray-200 dark:border-slate-700 mt-4">
        <button
          onClick={onClear}
          className="btn-secondary text-sm font-medium"
        >
          {t('settings.user.filters.clear')}
        </button>
      </div>
    </div>
  )
}

interface DateFilterProps {
  label: string
  fromValue: string
  toValue: string
  onFromChange: (value: string) => void
  onToChange: (value: string) => void
  onPreset: (days: number) => void
}

function DateFilter({ label, fromValue, toValue, onFromChange, onToChange, onPreset }: DateFilterProps) {
  const { t } = useI18n()

  return (
    <div>
      <label className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-2">
        {label}
      </label>
      <div className="space-y-2">
        <input
          type="date"
          value={fromValue}
          onChange={(e) => onFromChange(e.target.value)}
          className="form-input text-sm"
          placeholder={t('settings.user.filters.from')}
        />
        <input
          type="date"
          value={toValue}
          onChange={(e) => onToChange(e.target.value)}
          className="form-input text-sm"
          placeholder={t('settings.user.filters.to')}
        />
        <div className="flex gap-2 flex-wrap">
          {[7, 30, 90].map((days) => (
            <button
              key={days}
              onClick={() => onPreset(days)}
              className="btn-secondary px-3 py-1 text-xs"
            >
              {t('settings.user.filters.days', { days: days.toString() })}
            </button>
          ))}
        </div>
      </div>
    </div>
  )
}
