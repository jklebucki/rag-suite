// All code comments must be written in English, regardless of the conversation language.

import React, { useEffect, useMemo, useState } from 'react'
import { ChevronLeft, ChevronRight, User, Shield } from 'lucide-react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { authService } from '@/features/auth/services/auth.service'
import type { User as UserType } from '@/features/auth/types/auth'
import { useUserFilters } from '@/features/settings/hooks/useUserFilters'
import { useToast } from '@/shared/hooks/useToast'
import { useAuth } from '@/shared/contexts/AuthContext'
import { UserFiltersPanel } from './UserFiltersPanel'
import { UserTableRow } from './UserTableRow'
import { SetPasswordModal } from './SetPasswordModal'
import { ActionModal } from '@/shared/components/ui/ActionModal'
import { useI18n } from '@/shared/contexts/I18nContext'

export function UserSettings() {
  const { showSuccess, showError } = useToast()
  const queryClient = useQueryClient()
  const { t } = useI18n()
  const { user: currentUser, refreshAuth } = useAuth()
  const isAdmin = currentUser?.roles?.includes('Admin') ?? false
  
  const [selectedUser, setSelectedUser] = useState<UserType | null>(null)
  const [isSetPasswordModalOpen, setIsSetPasswordModalOpen] = useState(false)
  const [isFiltersExpanded, setIsFiltersExpanded] = useState(false)
  const [userToDelete, setUserToDelete] = useState<UserType | null>(null)
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false)
  const [pageSize, setPageSize] = useState(10)
  const [currentPage, setCurrentPage] = useState(1)

  // Fetch users
  const { data: users = [], isLoading, error: fetchError } = useQuery({
    queryKey: ['users'],
    queryFn: ({ signal }) => authService.getUsers({ signal }),
    enabled: isAdmin,
  })

  // Fetch roles
  const { data: availableRoles = [] } = useQuery({
    queryKey: ['roles'],
    queryFn: ({ signal }) => authService.getRoles({ signal }),
    enabled: isAdmin,
  })

  // Use filters hook
  const {
    filters,
    setFilters,
    filteredUsers,
    clearFilters,
    applyDatePreset
  } = useUserFilters(users)

  const totalPages = Math.max(1, Math.ceil(filteredUsers.length / pageSize))
  const pageStart = (currentPage - 1) * pageSize
  const paginatedUsers = useMemo(
    () => filteredUsers.slice(pageStart, pageStart + pageSize),
    [filteredUsers, pageSize, pageStart]
  )

  useEffect(() => {
    setCurrentPage(1)
  }, [filters, pageSize])

  useEffect(() => {
    setCurrentPage(page => Math.min(page, totalPages))
  }, [totalPages])

  // Assign role mutation
  const assignRoleMutation = useMutation({
    mutationFn: ({ userId, roleName }: { userId: string; roleName: string }) =>
      authService.assignRole(userId, roleName),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
      if (variables.userId === currentUser?.id) {
        void refreshAuth()
      }
      showSuccess(t('settings.user.toasts.role_assigned'))
    },
    onError: () => {
      showError(t('settings.user.toasts.role_assign_error'))
    }
  })

  // Remove role mutation
  const removeRoleMutation = useMutation({
    mutationFn: ({ userId, roleName }: { userId: string; roleName: string }) =>
      authService.removeRole(userId, roleName),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
      if (variables.userId === currentUser?.id) {
        void refreshAuth()
      }
      showSuccess(t('settings.user.toasts.role_removed'))
    },
    onError: () => {
      showError(t('settings.user.toasts.role_remove_error'))
    }
  })

  // Set password mutation
  const setPasswordMutation = useMutation({
    mutationFn: ({ userId, newPassword }: { userId: string; newPassword: string }) =>
      authService.setPassword(userId, newPassword),
    onSuccess: () => {
      setIsSetPasswordModalOpen(false)
      setSelectedUser(null)
      showSuccess(t('settings.user.toasts.password_set'))
    },
    onError: (error) => {
      showError(error instanceof Error ? error.message : t('settings.user.toasts.password_set_error'))
    }
  })

  const deleteUserMutation = useMutation({
    mutationFn: (userId: string) => authService.deleteUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
      showSuccess(t('settings.user.toasts.user_deleted'))
      setIsDeleteModalOpen(false)
      setUserToDelete(null)
    },
    onError: () => {
      showError(t('settings.user.toasts.user_delete_error'))
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

  const openDeleteUserModal = (user: UserType) => {
    setUserToDelete(user)
    setIsDeleteModalOpen(true)
  }

  const closeDeleteUserModal = () => {
    if (deleteUserMutation.isPending) return
    setIsDeleteModalOpen(false)
    setUserToDelete(null)
  }

  const handleDeleteUser = () => {
    if (!userToDelete) return
    deleteUserMutation.mutate(userToDelete.id)
  }

  if (!isAdmin) {
    return (
      <div className="surface p-6 space-y-3">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-red-100 rounded-lg dark:bg-red-900/30">
            <Shield className="h-6 w-6 text-red-600 dark:text-red-400" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">
              {t('settings.user.access_denied.title')}
            </h1>
            <p className="text-gray-600 dark:text-gray-300">
              {t('settings.user.access_denied.description')}
            </p>
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
          <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">
            {t('settings.user.title')}
          </h1>
          <p className="text-gray-600 dark:text-gray-300">{t('settings.user.subtitle')}</p>
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
              placeholder={t('settings.user.search_placeholder')}
              />
            </div>
            <button
              onClick={() => setIsFiltersExpanded(!isFiltersExpanded)}
              className="ml-4 inline-flex items-center gap-2 btn-secondary text-sm"
            >
              <Shield className="h-4 w-4" />
            {isFiltersExpanded ? t('settings.user.filters.hide') : t('settings.user.filters.show')}
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
        <div className="px-4 py-2 surface-muted border-b border-gray-200 dark:border-slate-700">
          <p className="text-sm text-gray-700 dark:text-gray-300">
            {t('settings.user.summary', {
              current: paginatedUsers.length.toString(),
              total: users.length.toString()
            })}
          </p>
        </div>

        {isLoading ? (
          <div className="p-6 text-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-green-600 mx-auto"></div>
            <p className="mt-2 text-gray-600 dark:text-gray-300">{t('settings.user.loading')}</p>
          </div>
        ) : fetchError ? (
          <div className="p-6 text-center">
            <p className="text-red-600 dark:text-red-400">{t('settings.user.error.loading')}</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200 dark:divide-slate-700">
              <thead className="bg-gray-50 dark:bg-slate-800/70">
                <tr>
                  <th className="px-4 py-2 text-left text-xs font-semibold text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                    {t('settings.user.table.user')}
                  </th>
                  <th className="px-4 py-2 text-left text-xs font-semibold text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                    {t('settings.user.table.email')}
                  </th>
                  <th className="px-4 py-2 text-left text-xs font-semibold text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                    {t('settings.user.table.roles')}
                  </th>
                  <th className="px-4 py-2 text-left text-xs font-semibold text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                    {t('settings.user.table.created')}
                  </th>
                  <th className="px-4 py-2 text-left text-xs font-semibold text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                    {t('settings.user.table.actions')}
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white dark:bg-slate-900 divide-y divide-gray-200 dark:divide-slate-800">
                {paginatedUsers.map((user) => (
                  <UserTableRow
                    key={user.id || user.email || Math.random()}
                    user={user}
                    availableRoles={availableRoles}
                    onAssignRole={handleAssignRole}
                    onRemoveRole={handleRemoveRole}
                    onSetPassword={openSetPasswordModal}
                    isAssigningRole={assignRoleMutation.isPending}
                    isRemovingRole={removeRoleMutation.isPending}
                    onDeleteUser={openDeleteUserModal}
                    isDeletingUser={deleteUserMutation.isPending && userToDelete?.id === user.id}
                    disableDelete={deleteUserMutation.isPending && userToDelete?.id !== user.id}
                  />
                ))}
              </tbody>
            </table>
          </div>
        )}

        {!isLoading && !fetchError && filteredUsers.length > 0 && (
          <div className="flex flex-wrap items-center justify-between gap-3 border-t border-gray-200 px-4 py-2 dark:border-slate-700">
            <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
              {t('settings.user.pagination.rows_per_page')}
              <select
                value={pageSize}
                onChange={(event) => setPageSize(Number(event.target.value))}
                className="form-input h-8 py-0 text-sm"
              >
                {[10, 20, 50].map(size => <option key={size} value={size}>{size}</option>)}
              </select>
            </label>
            <div className="flex items-center gap-2">
              <span className="text-sm text-gray-600 dark:text-gray-400">
                {t('settings.user.pagination.results', {
                  from: (pageStart + 1).toString(),
                  to: Math.min(pageStart + pageSize, filteredUsers.length).toString(),
                  total: filteredUsers.length.toString()
                })}
              </span>
              <button
                type="button"
                onClick={() => setCurrentPage(page => page - 1)}
                disabled={currentPage === 1}
                aria-label={t('settings.user.pagination.previous')}
                title={t('settings.user.pagination.previous')}
                className="inline-flex h-8 w-8 items-center justify-center rounded-md text-gray-600 transition-colors hover:bg-gray-100 disabled:cursor-not-allowed disabled:opacity-40 dark:text-gray-300 dark:hover:bg-slate-800"
              >
                <ChevronLeft className="h-4 w-4" />
              </button>
              <span className="min-w-12 text-center text-sm text-gray-700 dark:text-gray-300">
                {t('settings.user.pagination.page', { current: currentPage.toString(), total: totalPages.toString() })}
              </span>
              <button
                type="button"
                onClick={() => setCurrentPage(page => page + 1)}
                disabled={currentPage === totalPages}
                aria-label={t('settings.user.pagination.next')}
                title={t('settings.user.pagination.next')}
                className="inline-flex h-8 w-8 items-center justify-center rounded-md text-gray-600 transition-colors hover:bg-gray-100 disabled:cursor-not-allowed disabled:opacity-40 dark:text-gray-300 dark:hover:bg-slate-800"
              >
                <ChevronRight className="h-4 w-4" />
              </button>
            </div>
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

      <ActionModal
        isOpen={isDeleteModalOpen}
        onClose={closeDeleteUserModal}
        onConfirm={handleDeleteUser}
        title={t('settings.user.actions.delete_confirm_title')}
        message={t('settings.user.actions.delete_confirm_message', {
          name: userToDelete ? `${userToDelete.firstName ?? ''} ${userToDelete.lastName ?? ''}`.trim() || userToDelete.email || userToDelete.userName : ''
        })}
        confirmText={deleteUserMutation.isPending ? t('common.deleting') : t('settings.user.actions.delete_user')}
        cancelText={t('common.cancel')}
        variant="danger"
        isLoading={deleteUserMutation.isPending}
        closeOnConfirm={false}
      />
    </div>
  )
}
