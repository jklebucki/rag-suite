// All code comments must be written in English, regardless of the conversation language.

//import React from 'react'
//import { User, Shield } from 'lucide-react'
import type { UserFilters } from '@/types'

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
  const updateFilter = (key: keyof UserFilters, value: any) => {
    onFiltersChange({ ...filters, [key]: value })
  }

  const toggleRole = (role: string) => {
    const newRoles = filters.selectedRoles.includes(role)
      ? filters.selectedRoles.filter(r => r !== role)
      : [...filters.selectedRoles, role]
    updateFilter('selectedRoles', newRoles)
  }

  return (
    <div className="px-6 py-4 border-b border-gray-200 bg-gray-50">
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {/* Role Filter */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Roles
          </label>
          <div className="space-y-2 max-h-32 overflow-y-auto">
            {availableRoles.map((role) => (
              <label key={role} className="flex items-center">
                <input
                  type="checkbox"
                  checked={filters.selectedRoles.includes(role)}
                  onChange={() => toggleRole(role)}
                  className="h-4 w-4 text-indigo-600 focus:ring-indigo-500 border-gray-300 rounded"
                />
                <span className="ml-2 text-sm text-gray-700">{role}</span>
              </label>
            ))}
          </div>
        </div>

        {/* Active Status Filter */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Status
          </label>
          <div className="space-y-2">
            {[
              { value: 'all', label: 'All Users' },
              { value: 'active', label: 'Active Only' },
              { value: 'inactive', label: 'Inactive Only' }
            ].map((option) => (
              <label key={option.value} className="flex items-center">
                <input
                  type="radio"
                  name="activeStatus"
                  value={option.value}
                  checked={filters.activeStatus === option.value}
                  onChange={(e) => updateFilter('activeStatus', e.target.value as 'all' | 'active' | 'inactive')}
                  className="h-4 w-4 text-indigo-600 focus:ring-indigo-500 border-gray-300"
                />
                <span className="ml-2 text-sm text-gray-700">{option.label}</span>
              </label>
            ))}
          </div>
        </div>

        {/* Created Date Filter */}
        <DateFilter
          label="Created Date"
          fromValue={filters.createdDateFrom}
          toValue={filters.createdDateTo}
          onFromChange={(value) => updateFilter('createdDateFrom', value)}
          onToChange={(value) => updateFilter('createdDateTo', value)}
          onPreset={(days) => onApplyDatePreset('created', days)}
        />

        {/* Last Login Filter */}
        <DateFilter
          label="Last Login"
          fromValue={filters.lastLoginFrom}
          toValue={filters.lastLoginTo}
          onFromChange={(value) => updateFilter('lastLoginFrom', value)}
          onToChange={(value) => updateFilter('lastLoginTo', value)}
          onPreset={(days) => onApplyDatePreset('lastLogin', days)}
        />
      </div>

      {/* Filter Actions */}
      <div className="flex justify-end space-x-3 pt-4 border-t border-gray-200 mt-4">
        <button
          onClick={onClear}
          className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg shadow-sm hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
        >
          Clear Filters
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
  return (
    <div>
      <label className="block text-sm font-medium text-gray-700 mb-2">
        {label}
      </label>
      <div className="space-y-2">
        <input
          type="date"
          value={fromValue}
          onChange={(e) => onFromChange(e.target.value)}
          className="block w-full text-sm border-gray-300 rounded-md focus:ring-indigo-500 focus:border-indigo-500"
          placeholder="From"
        />
        <input
          type="date"
          value={toValue}
          onChange={(e) => onToChange(e.target.value)}
          className="block w-full text-sm border-gray-300 rounded-md focus:ring-indigo-500 focus:border-indigo-500"
          placeholder="To"
        />
        <div className="flex space-x-1">
          {[7, 30, 90].map((days) => (
            <button
              key={days}
              onClick={() => onPreset(days)}
              className="px-2 py-1 text-xs bg-gray-100 text-gray-700 rounded hover:bg-gray-200"
            >
              {days} days
            </button>
          ))}
        </div>
      </div>
    </div>
  )
}
