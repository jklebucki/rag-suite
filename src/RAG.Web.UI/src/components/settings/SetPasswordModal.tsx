// All code comments must be written in English, regardless of the conversation language.

import React, { useState } from 'react'
import { User, Key } from 'lucide-react'
import { Modal } from '@/components/ui/Modal'
import { PasswordInput } from './PasswordInput'
import { getPasswordStrength } from './passwordValidation'
import type { User as UserType } from '@/types/auth'

interface SetPasswordModalProps {
  isOpen: boolean
  onClose: () => void
  user: UserType | null
  onSetPassword: (password: string) => void
  isLoading: boolean
}

export function SetPasswordModal({
  isOpen,
  onClose,
  user,
  onSetPassword,
  isLoading
}: SetPasswordModalProps) {
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)

  const passwordStrength = getPasswordStrength(newPassword)
  const passwordsMatch = newPassword && confirmPassword && newPassword === confirmPassword
  const passwordsMismatch = newPassword && confirmPassword && newPassword !== confirmPassword

  const handleSubmit = () => {
    if (!passwordsMatch || passwordStrength.score < 3) return
    onSetPassword(newPassword)
  }

  const handleClose = () => {
    setNewPassword('')
    setConfirmPassword('')
    setShowPassword(false)
    setShowConfirmPassword(false)
    onClose()
  }

  const getMatchStatus = () => {
    if (!confirmPassword) return 'none'
    return passwordsMatch ? 'match' : 'mismatch'
  }

  if (!user) return null

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title={
        <div className="flex items-center space-x-3">
          <div className="p-2 bg-indigo-100 rounded-full">
            <Key className="h-5 w-5 text-indigo-600" />
          </div>
          <div>
            <h3 className="text-lg font-semibold text-gray-900">Set New Password</h3>
            <p className="text-sm text-gray-600">
              for {user.firstName} {user.lastName}
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
                    {user.firstName} {user.lastName}
                  </p>
                  <p className="text-sm text-indigo-600 font-medium">@{user.userName}</p>
                </div>
              </div>
            </div>

            {/* New Password Field */}
            <PasswordInput
              value={newPassword}
              onChange={setNewPassword}
              showPassword={showPassword}
              onToggleShow={() => setShowPassword(!showPassword)}
              placeholder="Enter a strong password"
              label="New Password"
              strength={passwordStrength}
            />

            {/* Confirm Password Field */}
            <PasswordInput
              value={confirmPassword}
              onChange={setConfirmPassword}
              showPassword={showConfirmPassword}
              onToggleShow={() => setShowConfirmPassword(!showConfirmPassword)}
              placeholder="Confirm your password"
              label="Confirm Password"
              matchStatus={getMatchStatus()}
            />

            {/* Action Buttons */}
            <div className="flex justify-end space-x-3 pt-4 border-t border-gray-200">
              <button
                onClick={handleClose}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg shadow-sm hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={handleSubmit}
                disabled={isLoading || !newPassword || !confirmPassword || passwordsMismatch || passwordStrength.score < 3}
                className="px-4 py-2 text-sm font-medium text-white bg-indigo-600 border border-transparent rounded-lg shadow-sm hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                {isLoading ? (
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
  )
}
