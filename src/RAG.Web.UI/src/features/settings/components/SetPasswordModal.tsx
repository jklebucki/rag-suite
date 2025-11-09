// All code comments must be written in English, regardless of the conversation language.

import React, { useState } from 'react'
import { User, Key } from 'lucide-react'
import { Modal } from '@/shared/components/ui/Modal'
import { PasswordInput } from './PasswordInput'
import { getPasswordStrength } from '@/utils/passwordValidation'
import type { User as UserType } from '@/features/auth/types/auth'

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
        <div className="flex items-center gap-3">
          <div className="p-2 bg-indigo-100 rounded-full dark:bg-indigo-900/40">
            <Key className="h-5 w-5 text-indigo-600 dark:text-indigo-300" />
          </div>
          <div>
            <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Set New Password</h3>
            <p className="text-sm text-gray-600 dark:text-gray-300">
              for {user.firstName} {user.lastName}
            </p>
          </div>
        </div>
      }
      size="md"
    >
      <div className="p-6">
        <div className="surface rounded-2xl border border-gray-200 dark:border-slate-700 min-h-[500px] flex flex-col">
          <div className="p-6 space-y-6 flex-1">
            {/* User Info Card */}
            <div className="surface-muted border border-indigo-200 dark:border-indigo-800/40 rounded-xl p-4">
              <div className="flex items-center gap-3">
                <div className="h-12 w-12 rounded-full bg-indigo-100 flex items-center justify-center dark:bg-indigo-900/40">
                  <User className="h-6 w-6 text-indigo-600 dark:text-indigo-300" />
                </div>
                <div>
                  <p className="font-semibold text-gray-900 dark:text-gray-100 text-lg">
                    {user.firstName} {user.lastName}
                  </p>
                  <p className="text-sm text-indigo-600 dark:text-indigo-300 font-medium">@{user.userName}</p>
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
            <div className="flex justify-end gap-3 pt-4 border-t border-gray-200 dark:border-slate-700">
              <button
                onClick={handleClose}
                className="btn-secondary text-sm font-medium transition-colors disabled:opacity-50"
              >
                Cancel
              </button>
              <button
                onClick={handleSubmit}
                disabled={isLoading || !newPassword || !confirmPassword || passwordsMismatch || passwordStrength.score < 3}
                className="btn-primary text-sm font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {isLoading ? (
                  <div className="flex items-center gap-2">
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white" />
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
