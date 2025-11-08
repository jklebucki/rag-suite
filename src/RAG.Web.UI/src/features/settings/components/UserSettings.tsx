// All code comments must be written in English, regardless of the conversation language.

import React, { useState } from 'react'
import { User, Shield } from 'lucide-react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { authService } from '@/features/auth/services/auth'
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
    queryFn: () => authService.getUsers(),
    enabled: authService.hasRole('Admin')
  })

  // Fetch roles
  const { data: availableRoles = [] } = useQuery({
    queryKey: ['roles'],
    queryFn: () => authService.getRoles(),
    enabled: authService.hasRole('Admin')
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
      <div className="space-y-6">
        <div className="flex items-center space-x-3">
          <div className="p-2 bg-red-100 rounded-lg">
            <Shield className="h-6 w-6 text-red-600" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Access Denied</h1>
            <p className="text-gray-600">You need Admin role to access user management</p>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center space-x-3">
        <div className="p-2 bg-green-100 rounded-lg">
          <User className="h-6 w-6 text-green-600" />
        </div>
        <div>
          <h1 className="text-2xl font-bold text-gray-900">User Management</h1>
          <p className="text-gray-600">Manage users, roles, and permissions</p>
        </div>
      </div>

      {/* Users Table */}
      <div className="bg-white shadow rounded-lg overflow-hidden">
        {/* Search Bar */}
        <div className="px-6 py-4 border-b border-gray-200">
          <div className="flex items-center justify-between">
            <div className="flex-1 max-w-lg">
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <User className="h-5 w-5 text-gray-400" />
                </div>
                <input
                  type="text"
                  value={filters.searchText}
                  onChange={(e) => setFilters(prev => ({ ...prev, searchText: e.target.value }))}
                  className="block w-full pl-10 pr-3 py-2 border border-gray-300 rounded-lg focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                  placeholder="Search users by name, username, or email..."
                />
              </div>
            </div>
            <button
              onClick={() => setIsFiltersExpanded(!isFiltersExpanded)}
              className="ml-4 inline-flex items-center px-4 py-2 border border-gray-300 rounded-lg text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
            >
              <Shield className="h-4 w-4 mr-2" />
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
        <div className="px-6 py-3 bg-gray-50 border-b border-gray-200">
          <p className="text-sm text-gray-700">
            Showing {filteredUsers.length} of {users.length} users
          </p>
        </div>

        {isLoading ? (
          <div className="p-6 text-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-green-600 mx-auto"></div>
            <p className="mt-2 text-gray-600">Loading users...</p>
          </div>
        ) : fetchError ? (
          <div className="p-6 text-center">
            <p className="text-red-600">Failed to load users</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    User
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Email
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Roles
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Created
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
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
