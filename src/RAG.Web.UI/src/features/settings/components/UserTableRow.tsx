// All code comments must be written in English, regardless of the conversation language.

import React from 'react'
import { User, Plus, Minus, Key, Trash2 } from 'lucide-react'
import type { User as UserType } from '@/features/auth/types/auth'
import { useI18n } from '@/shared/contexts/I18nContext'

interface UserTableRowProps {
  user: UserType
  availableRoles: string[]
  onAssignRole: (userId: string, roleName: string) => void
  onRemoveRole: (userId: string, roleName: string) => void
  onSetPassword: (user: UserType) => void
  isAssigningRole: boolean
  isRemovingRole: boolean
  onDeleteUser: (user: UserType) => void
  isDeletingUser: boolean
  disableDelete?: boolean
}

export function UserTableRow({
  user,
  availableRoles,
  onAssignRole,
  onRemoveRole,
  onSetPassword,
  isAssigningRole,
  isRemovingRole,
  onDeleteUser,
  isDeletingUser,
  disableDelete = false
}: UserTableRowProps) {
  const { t } = useI18n()
  const userRoles = user.roles || []
  const availableToAssign = availableRoles.filter(role => !userRoles.includes(role))

  return (
    <tr className="hover:bg-gray-50 dark:hover:bg-slate-800 transition-colors">
      <td className="px-4 py-2 whitespace-nowrap">
        <div className="flex items-center">
          <div className="flex-shrink-0 h-8 w-8">
            <div className="h-8 w-8 rounded-full bg-primary-100 dark:bg-primary-900/40 flex items-center justify-center text-primary-700 dark:text-primary-300">
              <User className="h-4 w-4" />
            </div>
          </div>
          <div className="ml-3">
            <div className="text-sm font-medium text-gray-900 dark:text-gray-100">
              {user.firstName || ''} {user.lastName || ''}
            </div>
            <div className="text-xs text-gray-500 dark:text-gray-400">@{user.userName || ''}</div>
          </div>
        </div>
      </td>
      <td className="px-4 py-2 whitespace-nowrap">
        <div className="text-sm text-gray-900 dark:text-gray-100">{user.email || ''}</div>
      </td>
      <td className="px-4 py-2 whitespace-nowrap">
        <div className="flex items-center gap-1">
          {userRoles.map((role) => (
            <span
              key={role}
              className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-primary-50 text-primary-700 dark:bg-primary-900/30 dark:text-primary-300"
            >
              {role}
              <button
                onClick={() => onRemoveRole(user.id, role)}
                className="ml-1 text-primary-600 hover:text-primary-700 dark:text-primary-300 dark:hover:text-primary-200"
                disabled={isRemovingRole}
                aria-label={t('settings.user.table.remove_role_aria', { role })}
              >
                <Minus className="h-3 w-3" />
              </button>
            </span>
          ))}
          <div className="flex items-center gap-1">
            {availableToAssign.map((role) => (
              <button
                key={role}
                onClick={() => onAssignRole(user.id, role)}
                className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-800 hover:bg-gray-200 dark:bg-slate-800 dark:text-gray-200 dark:hover:bg-slate-700"
                disabled={isAssigningRole}
              >
                <Plus className="h-3 w-3 mr-1" />
                {role}
              </button>
            ))}
          </div>
        </div>
      </td>
      <td className="px-4 py-2 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
        {user.createdAt ? new Date(user.createdAt).toLocaleDateString() : ''}
      </td>
      <td className="px-4 py-2 whitespace-nowrap text-sm font-medium">
        <div className="flex items-center gap-1">
          <ActionIconButton
            label={t('settings.user.actions.set_password')}
            onClick={() => onSetPassword(user)}
            className="text-primary-600 hover:bg-primary-50 hover:text-primary-700 dark:text-primary-300 dark:hover:bg-primary-900/30 dark:hover:text-primary-200"
          >
            <Key className="h-4 w-4" />
          </ActionIconButton>
          <ActionIconButton
            label={isDeletingUser ? t('common.deleting') : t('settings.user.actions.delete_user')}
            onClick={() => onDeleteUser(user)}
            disabled={isDeletingUser || disableDelete}
            className="text-red-600 hover:bg-red-50 hover:text-red-700 dark:text-red-400 dark:hover:bg-red-900/30 dark:hover:text-red-300"
          >
            <Trash2 className={`h-4 w-4 ${isDeletingUser ? 'animate-spin' : ''}`} />
          </ActionIconButton>
        </div>
      </td>
    </tr>
  )
}

interface ActionIconButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  label: string
}

function ActionIconButton({ label, className = '', children, ...props }: ActionIconButtonProps) {
  return (
    <span className="group relative inline-flex">
      <button
        type="button"
        aria-label={label}
        className={`inline-flex h-8 w-8 items-center justify-center rounded-md transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 disabled:cursor-not-allowed disabled:opacity-50 ${className}`}
        {...props}
      >
        {children}
      </button>
      <span
        role="tooltip"
        className="pointer-events-none absolute bottom-full left-1/2 z-10 mb-2 -translate-x-1/2 whitespace-nowrap rounded bg-gray-900 px-2 py-1 text-xs text-white opacity-0 shadow-sm transition-opacity group-hover:opacity-100 group-focus-within:opacity-100 dark:bg-slate-700"
      >
        {label}
      </span>
    </span>
  )
}
