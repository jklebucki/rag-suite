// All code comments must be written in English, regardless of the conversation language.

import React from 'react'
import { User, Plus, Minus, Key } from 'lucide-react'
import type { User as UserType } from '@/features/auth/types/auth'

interface UserTableRowProps {
  user: UserType
  availableRoles: string[]
  onAssignRole: (userId: string, roleName: string) => void
  onRemoveRole: (userId: string, roleName: string) => void
  onSetPassword: (user: UserType) => void
  isAssigningRole: boolean
  isRemovingRole: boolean
}

export function UserTableRow({
  user,
  availableRoles,
  onAssignRole,
  onRemoveRole,
  onSetPassword,
  isAssigningRole,
  isRemovingRole
}: UserTableRowProps) {
  const userRoles = user.roles || []
  const availableToAssign = availableRoles.filter(role => !userRoles.includes(role))

  return (
    <tr>
      <td className="px-6 py-4 whitespace-nowrap">
        <div className="flex items-center">
          <div className="flex-shrink-0 h-10 w-10">
            <div className="h-10 w-10 rounded-full bg-gray-300 flex items-center justify-center">
              <User className="h-6 w-6 text-gray-600" />
            </div>
          </div>
          <div className="ml-4">
            <div className="text-sm font-medium text-gray-900">
              {user.firstName || ''} {user.lastName || ''}
            </div>
            <div className="text-sm text-gray-500">@{user.userName || ''}</div>
          </div>
        </div>
      </td>
      <td className="px-6 py-4 whitespace-nowrap">
        <div className="text-sm text-gray-900">{user.email || ''}</div>
      </td>
      <td className="px-6 py-4 whitespace-nowrap">
        <div className="flex flex-wrap gap-1">
          {userRoles.map((role) => (
            <span
              key={role}
              className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800"
            >
              {role}
              <button
                onClick={() => onRemoveRole(user.id, role)}
                className="ml-1 text-blue-600 hover:text-blue-800"
                disabled={isRemovingRole}
                aria-label={`Remove ${role} role`}
              >
                <Minus className="h-3 w-3" />
              </button>
            </span>
          ))}
          <div className="flex flex-wrap gap-1 mt-1">
            {availableToAssign.map((role) => (
              <button
                key={role}
                onClick={() => onAssignRole(user.id, role)}
                className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-800 hover:bg-gray-200"
                disabled={isAssigningRole}
              >
                <Plus className="h-3 w-3 mr-1" />
                {role}
              </button>
            ))}
          </div>
        </div>
      </td>
      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
        {user.createdAt ? new Date(user.createdAt).toLocaleDateString() : ''}
      </td>
      <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
        <button
          onClick={() => onSetPassword(user)}
          className="text-indigo-600 hover:text-indigo-900 flex items-center"
        >
          <Key className="h-4 w-4 mr-1" />
          Set Password
        </button>
      </td>
    </tr>
  )
}
