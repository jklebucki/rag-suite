import React, { useState } from 'react'
import { User, Shield, Key, Plus, Minus } from 'lucide-react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useI18n } from '@/contexts/I18nContext'
import { authService } from '@/services/auth'
import { Modal } from '@/components/ui/Modal'
import { useToast } from '@/hooks/useToast'
import type { User as UserType } from '@/types/auth'

export function UserSettings() {
  const { t } = useI18n()
  const { showSuccess, showError } = useToast()
  const queryClient = useQueryClient()
  const [selectedUser, setSelectedUser] = useState<UserType | null>(null)
  const [isSetPasswordModalOpen, setIsSetPasswordModalOpen] = useState(false)
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')

  // Fetch users
  const { data: users = [], isLoading, error } = useQuery({
    queryKey: ['users'],
    queryFn: () => authService.getUsers(),
    enabled: authService.hasRole('Admin')
  })

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
      setNewPassword('')
      setConfirmPassword('')
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

  const handleSetPassword = () => {
    if (!selectedUser || !newPassword) return

    if (newPassword !== confirmPassword) {
      showError('Passwords do not match')
      return
    }

    if (newPassword.length < 8) {
      showError('Password must be at least 8 characters long')
      return
    }

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
        {isLoading ? (
          <div className="p-6 text-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-green-600 mx-auto"></div>
            <p className="mt-2 text-gray-600">Loading users...</p>
          </div>
        ) : error ? (
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
                {users.map((user) => (
                  <tr key={user.id}>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="flex items-center">
                        <div className="flex-shrink-0 h-10 w-10">
                          <div className="h-10 w-10 rounded-full bg-gray-300 flex items-center justify-center">
                            <User className="h-6 w-6 text-gray-600" />
                          </div>
                        </div>
                        <div className="ml-4">
                          <div className="text-sm font-medium text-gray-900">
                            {user.firstName} {user.lastName}
                          </div>
                          <div className="text-sm text-gray-500">@{user.userName}</div>
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm text-gray-900">{user.email}</div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="flex flex-wrap gap-1">
                        {user.roles.map((role) => (
                          <span
                            key={role}
                            className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800"
                          >
                            {role}
                            <button
                              onClick={() => handleRemoveRole(user.id, role)}
                              className="ml-1 text-blue-600 hover:text-blue-800"
                              disabled={removeRoleMutation.isLoading}
                              aria-label={`Remove ${role} role`}
                            >
                              <Minus className="h-3 w-3" />
                            </button>
                          </span>
                        ))}
                        <button
                          onClick={() => handleAssignRole(user.id, 'Admin')}
                          className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-800 hover:bg-gray-200"
                          disabled={assignRoleMutation.isLoading || user.roles.includes('Admin')}
                        >
                          <Plus className="h-3 w-3 mr-1" />
                          Admin
                        </button>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {new Date(user.createdAt).toLocaleDateString()}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                      <button
                        onClick={() => openSetPasswordModal(user)}
                        className="text-indigo-600 hover:text-indigo-900 flex items-center"
                      >
                        <Key className="h-4 w-4 mr-1" />
                        Set Password
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Set Password Modal */}
      <Modal
        isOpen={isSetPasswordModalOpen}
        onClose={() => {
          setIsSetPasswordModalOpen(false)
          setNewPassword('')
          setConfirmPassword('')
          setSelectedUser(null)
        }}
        title={`Set Password for ${selectedUser?.firstName} ${selectedUser?.lastName}`}
      >
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700">New Password</label>
            <input
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              className="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
              placeholder="Enter new password"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Confirm Password</label>
            <input
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              className="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
              placeholder="Confirm new password"
            />
          </div>
          <div className="flex justify-end space-x-3">
            <button
              onClick={() => {
                setIsSetPasswordModalOpen(false)
                setNewPassword('')
                setConfirmPassword('')
                setSelectedUser(null)
              }}
              className="px-4 py-2 text-sm font-medium text-gray-700 bg-gray-100 border border-gray-300 rounded-md hover:bg-gray-200"
            >
              Cancel
            </button>
            <button
              onClick={handleSetPassword}
              disabled={setPasswordMutation.isLoading || !newPassword || !confirmPassword}
              className="px-4 py-2 text-sm font-medium text-white bg-indigo-600 border border-transparent rounded-md hover:bg-indigo-700 disabled:opacity-50"
            >
              {setPasswordMutation.isLoading ? 'Setting...' : 'Set Password'}
            </button>
          </div>
        </div>
      </Modal>
    </div>
  )
}
