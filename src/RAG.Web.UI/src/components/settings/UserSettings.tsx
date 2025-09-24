import React, { useState } from 'react'
import { User, Shield, Key, Plus, Minus, Eye, EyeOff, Lock, CheckCircle, XCircle } from 'lucide-react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useI18n } from '@/contexts/I18nContext'
import { authService } from '@/services/auth'
import { Modal } from '@/components/ui/Modal'
import { useToast } from '@/hooks/useToast'
import type { User as UserType } from '@/types/auth'

// Filter interfaces
interface UserFilters {
  searchText: string
  selectedRoles: string[]
  activeStatus: 'all' | 'active' | 'inactive'
  createdDateFrom: string
  createdDateTo: string
  lastLoginFrom: string
  lastLoginTo: string
}

interface DatePreset {
  label: string
  days: number
}

export function UserSettings() {
  const { t } = useI18n()
  const { showSuccess, showError } = useToast()
  const queryClient = useQueryClient()
  const [selectedUser, setSelectedUser] = useState<UserType | null>(null)
  const [isSetPasswordModalOpen, setIsSetPasswordModalOpen] = useState(false)
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)

  // Filter states
  const [filters, setFilters] = useState<UserFilters>({
    searchText: '',
    selectedRoles: [],
    activeStatus: 'all',
    createdDateFrom: '',
    createdDateTo: '',
    lastLoginFrom: '',
    lastLoginTo: ''
  })
  const [isFiltersExpanded, setIsFiltersExpanded] = useState(false)
  const [debouncedSearchText, setDebouncedSearchText] = useState('')

  // Filter users based on current filters
  const filterUsers = (users: UserType[]): UserType[] => {
    return users.filter(user => {
      // Text search filter
      if (debouncedSearchText) {
        const searchLower = debouncedSearchText.toLowerCase()
        const matchesSearch =
          user.firstName.toLowerCase().includes(searchLower) ||
          user.lastName.toLowerCase().includes(searchLower) ||
          user.fullName.toLowerCase().includes(searchLower) ||
          user.userName.toLowerCase().includes(searchLower) ||
          user.email.toLowerCase().includes(searchLower)

        if (!matchesSearch) return false
      }

      // Role filter
      if (filters.selectedRoles.length > 0) {
        const hasAllSelectedRoles = filters.selectedRoles.every(role =>
          user.roles.includes(role)
        )
        if (!hasAllSelectedRoles) return false
      }

      // Active status filter
      if (filters.activeStatus !== 'all') {
        if (filters.activeStatus === 'active' && !user.isActive) return false
        if (filters.activeStatus === 'inactive' && user.isActive) return false
      }

      // Created date filter
      if (filters.createdDateFrom) {
        const createdDate = new Date(user.createdAt)
        const fromDate = new Date(filters.createdDateFrom)
        if (createdDate < fromDate) return false
      }
      if (filters.createdDateTo) {
        const createdDate = new Date(user.createdAt)
        const toDate = new Date(filters.createdDateTo)
        toDate.setHours(23, 59, 59, 999) // End of day
        if (createdDate > toDate) return false
      }

      // Last login filter
      if (filters.lastLoginFrom) {
        if (!user.lastLoginAt) return false
        const lastLoginDate = new Date(user.lastLoginAt)
        const fromDate = new Date(filters.lastLoginFrom)
        if (lastLoginDate < fromDate) return false
      }
      if (filters.lastLoginTo) {
        if (!user.lastLoginAt) return false
        const lastLoginDate = new Date(user.lastLoginAt)
        const toDate = new Date(filters.lastLoginTo)
        toDate.setHours(23, 59, 59, 999) // End of day
        if (lastLoginDate > toDate) return false
      }

      return true
    })
  }

  // Debounce search text
  React.useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearchText(filters.searchText)
    }, 300)

    return () => clearTimeout(timer)
  }, [filters.searchText])

  // URL persistence for filters
  React.useEffect(() => {
    const urlParams = new URLSearchParams(window.location.search)

    // Read filters from URL on mount
    const searchText = urlParams.get('search') || ''
    const selectedRoles = urlParams.get('roles')?.split(',').filter(Boolean) || []
    const activeStatus = (urlParams.get('status') as 'all' | 'active' | 'inactive') || 'all'
    const createdDateFrom = urlParams.get('createdFrom') || ''
    const createdDateTo = urlParams.get('createdTo') || ''
    const lastLoginFrom = urlParams.get('loginFrom') || ''
    const lastLoginTo = urlParams.get('loginTo') || ''

    setFilters({
      searchText,
      selectedRoles,
      activeStatus,
      createdDateFrom,
      createdDateTo,
      lastLoginFrom,
      lastLoginTo
    })
  }, [])

  // Update URL when filters change
  React.useEffect(() => {
    const urlParams = new URLSearchParams()

    if (filters.searchText) urlParams.set('search', filters.searchText)
    if (filters.selectedRoles.length > 0) urlParams.set('roles', filters.selectedRoles.join(','))
    if (filters.activeStatus !== 'all') urlParams.set('status', filters.activeStatus)
    if (filters.createdDateFrom) urlParams.set('createdFrom', filters.createdDateFrom)
    if (filters.createdDateTo) urlParams.set('createdTo', filters.createdDateTo)
    if (filters.lastLoginFrom) urlParams.set('loginFrom', filters.lastLoginFrom)
    if (filters.lastLoginTo) urlParams.set('loginTo', filters.lastLoginTo)

    const newUrl = urlParams.toString()
    const currentUrl = window.location.search.substring(1)

    if (newUrl !== currentUrl) {
      window.history.replaceState(null, '', newUrl ? `?${newUrl}` : window.location.pathname)
    }
  }, [filters])

  // Date preset helper
  const applyDatePreset = (field: 'created' | 'lastLogin', days: number) => {
    const today = new Date()
    const fromDate = new Date(today)
    fromDate.setDate(today.getDate() - days)

    if (field === 'created') {
      setFilters(prev => ({
        ...prev,
        createdDateFrom: fromDate.toISOString().split('T')[0],
        createdDateTo: today.toISOString().split('T')[0]
      }))
    } else {
      setFilters(prev => ({
        ...prev,
        lastLoginFrom: fromDate.toISOString().split('T')[0],
        lastLoginTo: today.toISOString().split('T')[0]
      }))
    }
  }

  // Fetch users
  const { data: users = [], isLoading, error } = useQuery({
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

  // Password strength checker
  const getPasswordStrength = (password: string) => {
    let strength = 0
    const checks = {
      length: password.length >= 8,
      uppercase: /[A-Z]/.test(password),
      lowercase: /[a-z]/.test(password),
      number: /\d/.test(password),
      special: /[!@#$%^&*(),.?":{}|<>]/.test(password)
    }

    strength = Object.values(checks).filter(Boolean).length

    return {
      score: strength,
      checks,
      label: strength <= 2 ? 'Weak' : strength <= 3 ? 'Fair' : strength <= 4 ? 'Good' : 'Strong'
    }
  }

  const passwordStrength = getPasswordStrength(newPassword)
  const passwordsMatch = newPassword && confirmPassword && newPassword === confirmPassword
  const passwordsMismatch = newPassword && confirmPassword && newPassword !== confirmPassword

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
                        onChange={(e) => {
                          if (e.target.checked) {
                            setFilters(prev => ({
                              ...prev,
                              selectedRoles: [...prev.selectedRoles, role]
                            }))
                          } else {
                            setFilters(prev => ({
                              ...prev,
                              selectedRoles: prev.selectedRoles.filter(r => r !== role)
                            }))
                          }
                        }}
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
                        onChange={(e) => setFilters(prev => ({
                          ...prev,
                          activeStatus: e.target.value as 'all' | 'active' | 'inactive'
                        }))}
                        className="h-4 w-4 text-indigo-600 focus:ring-indigo-500 border-gray-300"
                      />
                      <span className="ml-2 text-sm text-gray-700">{option.label}</span>
                    </label>
                  ))}
                </div>
              </div>

              {/* Created Date Filter */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Created Date
                </label>
                <div className="space-y-2">
                  <input
                    type="date"
                    value={filters.createdDateFrom}
                    onChange={(e) => setFilters(prev => ({
                      ...prev,
                      createdDateFrom: e.target.value
                    }))}
                    className="block w-full text-sm border-gray-300 rounded-md focus:ring-indigo-500 focus:border-indigo-500"
                    placeholder="From"
                  />
                  <input
                    type="date"
                    value={filters.createdDateTo}
                    onChange={(e) => setFilters(prev => ({
                      ...prev,
                      createdDateTo: e.target.value
                    }))}
                    className="block w-full text-sm border-gray-300 rounded-md focus:ring-indigo-500 focus:border-indigo-500"
                    placeholder="To"
                  />
                  <div className="flex space-x-1">
                    <button
                      onClick={() => applyDatePreset('created', 7)}
                      className="px-2 py-1 text-xs bg-gray-100 text-gray-700 rounded hover:bg-gray-200"
                    >
                      7 days
                    </button>
                    <button
                      onClick={() => applyDatePreset('created', 30)}
                      className="px-2 py-1 text-xs bg-gray-100 text-gray-700 rounded hover:bg-gray-200"
                    >
                      30 days
                    </button>
                    <button
                      onClick={() => applyDatePreset('created', 90)}
                      className="px-2 py-1 text-xs bg-gray-100 text-gray-700 rounded hover:bg-gray-200"
                    >
                      90 days
                    </button>
                  </div>
                </div>
              </div>

              {/* Last Login Filter */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Last Login
                </label>
                <div className="space-y-2">
                  <input
                    type="date"
                    value={filters.lastLoginFrom}
                    onChange={(e) => setFilters(prev => ({
                      ...prev,
                      lastLoginFrom: e.target.value
                    }))}
                    className="block w-full text-sm border-gray-300 rounded-md focus:ring-indigo-500 focus:border-indigo-500"
                    placeholder="From"
                  />
                  <input
                    type="date"
                    value={filters.lastLoginTo}
                    onChange={(e) => setFilters(prev => ({
                      ...prev,
                      lastLoginTo: e.target.value
                    }))}
                    className="block w-full text-sm border-gray-300 rounded-md focus:ring-indigo-500 focus:border-indigo-500"
                    placeholder="To"
                  />
                  <div className="flex space-x-1">
                    <button
                      onClick={() => applyDatePreset('lastLogin', 7)}
                      className="px-2 py-1 text-xs bg-gray-100 text-gray-700 rounded hover:bg-gray-200"
                    >
                      7 days
                    </button>
                    <button
                      onClick={() => applyDatePreset('lastLogin', 30)}
                      className="px-2 py-1 text-xs bg-gray-100 text-gray-700 rounded hover:bg-gray-200"
                    >
                      30 days
                    </button>
                    <button
                      onClick={() => applyDatePreset('lastLogin', 90)}
                      className="px-2 py-1 text-xs bg-gray-100 text-gray-700 rounded hover:bg-gray-200"
                    >
                      90 days
                    </button>
                  </div>
                </div>
              </div>
            </div>

            {/* Filter Actions */}
            <div className="flex justify-end space-x-3 pt-4 border-t border-gray-200">
              <button
                onClick={() => {
                  setFilters({
                    searchText: '',
                    selectedRoles: [],
                    activeStatus: 'all',
                    createdDateFrom: '',
                    createdDateTo: '',
                    lastLoginFrom: '',
                    lastLoginTo: ''
                  })
                  setDebouncedSearchText('')
                }}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg shadow-sm hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
              >
                Clear Filters
              </button>
              <button
                onClick={() => setIsFiltersExpanded(false)}
                className="px-4 py-2 text-sm font-medium text-white bg-indigo-600 border border-transparent rounded-lg shadow-sm hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
              >
                Apply Filters
              </button>
            </div>
          </div>
        )}

        {/* Results Summary */}
        <div className="px-6 py-3 bg-gray-50 border-b border-gray-200">
          <p className="text-sm text-gray-700">
            Showing {filterUsers(users).length} of {users.length} users
          </p>
        </div>
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
                {filterUsers(users).map((user) => (
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
                        <div className="flex flex-wrap gap-1 mt-1">
                          {availableRoles.filter(role => !user.roles.includes(role)).map((role) => (
                            <button
                              key={role}
                              onClick={() => handleAssignRole(user.id, role)}
                              className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-800 hover:bg-gray-200"
                              disabled={assignRoleMutation.isLoading}
                            >
                              <Plus className="h-3 w-3 mr-1" />
                              {role}
                            </button>
                          ))}
                        </div>
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
          setShowPassword(false)
          setShowConfirmPassword(false)
        }}
        title={
          <div className="flex items-center space-x-3">
            <div className="p-2 bg-indigo-100 rounded-full">
              <Key className="h-5 w-5 text-indigo-600" />
            </div>
            <div>
              <h3 className="text-lg font-semibold text-gray-900">Set New Password</h3>
              <p className="text-sm text-gray-600">
                for {selectedUser?.firstName} {selectedUser?.lastName}
              </p>
            </div>
          </div>
        }
        size="md"
      >
        <div className="p-6">
          <div className="bg-white rounded-xl shadow-lg border border-gray-200 min-h-[500px] flex flex-col">
            <div className="p-6 space-y-6 flex-1">
              {/* User Info Card */}
              <div className="bg-gradient-to-r from-indigo-50 to-blue-50 rounded-lg p-4 border border-indigo-100">
                <div className="flex items-center space-x-3">
                  <div className="h-12 w-12 rounded-full bg-indigo-100 flex items-center justify-center">
                    <User className="h-6 w-6 text-indigo-600" />
                  </div>
                  <div>
                    <p className="font-semibold text-gray-900 text-lg">
                      {selectedUser?.firstName} {selectedUser?.lastName}
                    </p>
                    <p className="text-sm text-indigo-600 font-medium">@{selectedUser?.userName}</p>
                  </div>
                </div>
              </div>

          {/* New Password Field */}
          <div className="space-y-2">
            <label className="block text-sm font-medium text-gray-700">
              New Password
            </label>
            <div className="relative">
              <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                <Lock className="h-5 w-5 text-gray-400" />
              </div>
              <input
                type={showPassword ? 'text' : 'password'}
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                className="block w-full pl-10 pr-10 py-3 border border-gray-300 rounded-lg shadow-sm placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-colors"
                placeholder="Enter a strong password"
                autoComplete="new-password"
              />
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-gray-600 transition-colors"
              >
                {showPassword ? (
                  <EyeOff className="h-5 w-5" />
                ) : (
                  <Eye className="h-5 w-5" />
                )}
              </button>
            </div>

            {/* Password Strength Indicator */}
            {newPassword && (
              <div className="space-y-2">
                <div className="flex items-center justify-between text-sm">
                  <span className="text-gray-600">Password strength:</span>
                  <span className={`font-medium ${
                    passwordStrength.score <= 2 ? 'text-red-600' :
                    passwordStrength.score <= 3 ? 'text-yellow-600' :
                    passwordStrength.score <= 4 ? 'text-blue-600' : 'text-green-600'
                  }`}>
                    {passwordStrength.label}
                  </span>
                </div>
                <div className="w-full bg-gray-200 rounded-full h-2">
                  <div
                    className={`h-2 rounded-full transition-all duration-300 ${
                      passwordStrength.score <= 2 ? 'bg-red-500' :
                      passwordStrength.score <= 3 ? 'bg-yellow-500' :
                      passwordStrength.score <= 4 ? 'bg-blue-500' : 'bg-green-500'
                    }`}
                    style={{ width: `${Math.min((passwordStrength.score / 5) * 100, 100)}%` }}
                  ></div>
                </div>

                {/* Password Requirements */}
                <div className="grid grid-cols-2 gap-2 text-xs">
                  <div className={`flex items-center space-x-2 ${passwordStrength.checks.length ? 'text-green-600' : 'text-gray-400'}`}>
                    {passwordStrength.checks.length ? <CheckCircle className="h-3 w-3" /> : <XCircle className="h-3 w-3" />}
                    <span>8+ characters</span>
                  </div>
                  <div className={`flex items-center space-x-2 ${passwordStrength.checks.uppercase ? 'text-green-600' : 'text-gray-400'}`}>
                    {passwordStrength.checks.uppercase ? <CheckCircle className="h-3 w-3" /> : <XCircle className="h-3 w-3" />}
                    <span>Uppercase</span>
                  </div>
                  <div className={`flex items-center space-x-2 ${passwordStrength.checks.lowercase ? 'text-green-600' : 'text-gray-400'}`}>
                    {passwordStrength.checks.lowercase ? <CheckCircle className="h-3 w-3" /> : <XCircle className="h-3 w-3" />}
                    <span>Lowercase</span>
                  </div>
                  <div className={`flex items-center space-x-2 ${passwordStrength.checks.number ? 'text-green-600' : 'text-gray-400'}`}>
                    {passwordStrength.checks.number ? <CheckCircle className="h-3 w-3" /> : <XCircle className="h-3 w-3" />}
                    <span>Number</span>
                  </div>
                  <div className={`flex items-center space-x-2 ${passwordStrength.checks.special ? 'text-green-600' : 'text-gray-400'}`}>
                    {passwordStrength.checks.special ? <CheckCircle className="h-3 w-3" /> : <XCircle className="h-3 w-3" />}
                    <span>Special char</span>
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* Confirm Password Field */}
          <div className="space-y-2">
            <label className="block text-sm font-medium text-gray-700">
              Confirm Password
            </label>
            <div className="relative">
              <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                <Lock className="h-5 w-5 text-gray-400" />
              </div>
              <input
                type={showConfirmPassword ? 'text' : 'password'}
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                className={`block w-full pl-10 pr-10 py-3 border rounded-lg shadow-sm placeholder-gray-400 focus:outline-none focus:ring-2 transition-colors ${
                  passwordsMismatch
                    ? 'border-red-300 focus:ring-red-500 focus:border-red-500'
                    : passwordsMatch
                    ? 'border-green-300 focus:ring-green-500 focus:border-green-500'
                    : 'border-gray-300 focus:ring-indigo-500 focus:border-indigo-500'
                }`}
                placeholder="Confirm your password"
                autoComplete="new-password"
              />
              <button
                type="button"
                onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-gray-600 transition-colors"
              >
                {showConfirmPassword ? (
                  <EyeOff className="h-5 w-5" />
                ) : (
                  <Eye className="h-5 w-5" />
                )}
              </button>
            </div>

            {/* Password Match Indicator */}
            {confirmPassword && (
              <div className="flex items-center space-x-2 text-sm">
                {passwordsMatch ? (
                  <>
                    <CheckCircle className="h-4 w-4 text-green-600" />
                    <span className="text-green-600">Passwords match</span>
                  </>
                ) : passwordsMismatch ? (
                  <>
                    <XCircle className="h-4 w-4 text-red-600" />
                    <span className="text-red-600">Passwords do not match</span>
                  </>
                ) : null}
              </div>
            )}
          </div>

          {/* Action Buttons */}
          <div className="flex justify-end space-x-3 pt-4 border-t border-gray-200">
            <button
              onClick={() => {
                setIsSetPasswordModalOpen(false)
                setNewPassword('')
                setConfirmPassword('')
                setSelectedUser(null)
                setShowPassword(false)
                setShowConfirmPassword(false)
              }}
              className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg shadow-sm hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 transition-colors"
            >
              Cancel
            </button>
            <button
              onClick={handleSetPassword}
              disabled={setPasswordMutation.isLoading || !newPassword || !confirmPassword || passwordsMismatch || passwordStrength.score < 3}
              className="px-4 py-2 text-sm font-medium text-white bg-indigo-600 border border-transparent rounded-lg shadow-sm hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {setPasswordMutation.isLoading ? (
                <div className="flex items-center space-x-2">
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                  <span>Setting Password...</span>
                </div>
              ) : (
                'Set Password'
              )}
            </button>
          </div>
            </div>
          </div>
        </div>
      </Modal>
    </div>
  )
}
