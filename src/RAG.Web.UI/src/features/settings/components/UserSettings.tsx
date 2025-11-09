// All code comments must be written in English, regardless of the conversation language.

import React, { useState } from 'react'
import { User, Shield } from 'lucide-react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { authService } from '@/features/auth/services/auth.service'
import type { User as UserType } from '@/features/auth/types/auth'
import { useUserFilters } from '@/features/settings/hooks/useUserFilters'
import { useToast } from '@/shared/hooks/useToast'
import { UserFiltersPanel } from './UserFiltersPanel'
import { UserTableRow } from './UserTableRow'
import { SetPasswordModal } from './SetPasswordModal'

export function UserSettings() {
  const { showSuccess, showError } = useToast()
  const queryClient = useQueryClient()
  
  const [selectedUser, setSelectedUser] = useState<UserType | null>(null)
  const [isSetPasswordModalOpen, setIsSetPasswordModalOpen] = useState(false)
  const [isFiltersExpanded, setIsFiltersExpanded] = useState(false)

  // Fetch users
  const { data: users = [], isLoading, error: fetchError } = useQuery({
    queryKey: ['users'],
    queryFn: ({ signal }) => authService.getUsers({ signal }),
    enabled: authService.hasRole('Admin'),
  })

  // Fetch roles
  const { data: availableRoles = [] } = useQuery({
    queryKey: ['roles'],
    queryFn: ({ signal }) => authService.getRoles({ signal }),
    enabled: authService.hasRole('Admin'),
  })

  // Use filters hook
  const {
    filters,
    setFilters,
    filteredUsers,
    clearFilters,
    applyDatePreset
  } = useUserFilters(users)

  // Assign role mutation
  const assignRoleMutation = useMutation({
    mutationFn: ({ userId, roleName }: { userId: string; roleName: string }) =>
      authService.assignRole(userId, roleName),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
      showSuccess('Role assigned successfully')
    },
    onError: () => {
      showError('Failed to assign role')
    }
  })

  // Remove role mutation
  const removeRoleMutation = useMutation({
    mutationFn: ({ userId, roleName }: { userId: string; roleName: string }) =>
      authService.removeRole(userId, roleName),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
      showSuccess('Role removed successfully')
    },
    onError: () => {
      showError('Failed to remove role')
    }
  })

  // Set password mutation
  const setPasswordMutation = useMutation({
    mutationFn: ({ userId, newPassword }: { userId: string; newPassword: string }) =>
      authService.setPassword(userId, newPassword),
    onSuccess: () => {
      setIsSetPasswordModalOpen(false)
      setSelectedUser(null)
      showSuccess('Password set successfully')
    },
    onError: () => {
      showError('Failed to set password')
    }
  })

  const handleAssignRole = (userId: string, roleName: string) => {
    assignRoleMutation.mutate({ userId, roleName })
  }

  const handleRemoveRole = (userId: string, roleName: string) => {
    removeRoleMutation.mutate({ userId, roleName })
  }

  const handleSetPassword = (newPassword: string) => {
    if (!selectedUser) return
    setPasswordMutation.mutate({ userId: selectedUser.id, newPassword })
  }

  const openSetPasswordModal = (user: UserType) => {
    setSelectedUser(user)
    setIsSetPasswordModalOpen(true)
  }

  if (!authService.hasRole('Admin')) {
    return (
      <div className="surface p-6 space-y-3">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-red-100 rounded-lg dark:bg-red-900/30">
            <Shield className="h-6 w-6 text-red-600 dark:text-red-400" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Access Denied</h1>
            <p className="text-gray-600 dark:text-gray-300">You need Admin role to access user management</p>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-3">
        <div className="p-2 bg-green-100 rounded-lg dark:bg-green-900/30">
          <User className="h-6 w-6 text-green-600 dark:text-green-300" />
        </div>
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">User Management</h1>
          <p className="text-gray-600 dark:text-gray-300">Manage users, roles, and permissions</p>
        </div>
      </div>

      {/* Users Table */}
      <div className="surface rounded-2xl overflow-hidden">
        {/* Search Bar */}
        <div className="px-6 py-4 border-b border-gray-200 dark:border-slate-700">
          <div className="flex items-center justify-between">
            <div className="flex-1 max-w-lg relative">
              <div className="absolute inset-y-0 left-3 flex items-center pointer-events-none">
                <User className="h-5 w-5 text-gray-400 dark:text-gray-500" />
              </div>
              <input
                type="text"
                value={filters.searchText}
                onChange={(e) => setFilters(prev => ({ ...prev, searchText: e.target.value }))}
                className="form-input w-full pl-10 sm:text-sm"
                placeholder="Search users by name, username, or email..."
              />
            </div>
            <button
              onClick={() => setIsFiltersExpanded(!isFiltersExpanded)}
              className="ml-4 inline-flex items-center gap-2 btn-secondary text-sm"
            >
              <Shield className="h-4 w-4" />
              {isFiltersExpanded ? 'Hide Filters' : 'More Filters'}
            </button>
          </div>
        </div>

        {/* Filters Panel */}
        {isFiltersExpanded && (
          <UserFiltersPanel
            filters={filters}
            onFiltersChange={setFilters}
            availableRoles={availableRoles}
            onClear={clearFilters}
            onApplyDatePreset={applyDatePreset}
          />
        )}

        {/* Results Summary */}
        <div className="px-6 py-3 surface-muted border-b border-gray-200 dark:border-slate-700">
          <p className="text-sm text-gray-700 dark:text-gray-300">
            Showing {filteredUsers.length} of {users.length} users
          </p>
        </div>

        {isLoading ? (
          <div className="p-6 text-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-green-600 mx-auto"></div>
            <p className="mt-2 text-gray-600 dark:text-gray-300">Loading users...</p>
          </div>
        ) : fetchError ? (
          <div className="p-6 text-center">
            <p className="text-red-600 dark:text-red-400">Failed to load users</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200 dark:divide-slate-700">
              <thead className="bg-gray-50 dark:bg-slate-800/70">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                    User
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                    Email
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                    Roles
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                    Created
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white dark:bg-slate-900 divide-y divide-gray-200 dark:divide-slate-800">
                {filteredUsers.map((user) => (
                  <UserTableRow
                    key={user.id || user.email || Math.random()}
                    user={user}
                    availableRoles={availableRoles}
                    onAssignRole={handleAssignRole}
                    onRemoveRole={handleRemoveRole}
                    onSetPassword={openSetPasswordModal}
                    isAssigningRole={assignRoleMutation.isPending}
                    isRemovingRole={removeRoleMutation.isPending}
                  />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Set Password Modal */}
      <SetPasswordModal
        isOpen={isSetPasswordModalOpen}
        onClose={() => {
          setIsSetPasswordModalOpen(false)
          setSelectedUser(null)
        }}
        user={selectedUser}
        onSetPassword={handleSetPassword}
        isLoading={setPasswordMutation.isPending}
      />
    </div>
  )
}
