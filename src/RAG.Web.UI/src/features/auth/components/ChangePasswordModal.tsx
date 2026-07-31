import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Eye, EyeOff, Lock } from 'lucide-react'
import { Modal } from '@/shared/components/ui/Modal'
import { useI18n } from '@/shared/contexts/I18nContext'
import { useToast } from '@/shared/contexts/ToastContext'
import { usePasswordValidation } from '@/shared/contexts/ConfigurationContext'
import { usePasswordRequirements } from '@/features/auth/hooks/useRegisterValidation'
import { authService } from '@/features/auth/services/auth.service'
import type { TranslationKeys } from '@/shared/types/i18n'
import { logger } from '@/utils/logger'

interface ChangePasswordFormData {
  currentPassword: string
  newPassword: string
  confirmPassword: string
}

interface ChangePasswordModalProps {
  isOpen: boolean
  onClose: () => void
}

export function ChangePasswordModal({ isOpen, onClose }: ChangePasswordModalProps) {
  const { t } = useI18n()
  const { showSuccess, showError } = useToast()
  const { validatePassword } = usePasswordValidation()
  const passwordRequirements = usePasswordRequirements()

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    reset,
    watch,
  } = useForm<ChangePasswordFormData>({
    mode: 'onBlur',
    defaultValues: {
      currentPassword: '',
      newPassword: '',
      confirmPassword: '',
    },
  })

  const [showCurrentPassword, setShowCurrentPassword] = useState(false)
  const [showNewPassword, setShowNewPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)

  const newPassword = watch('newPassword')

  const handleClose = () => {
    if (isSubmitting) return
    reset()
    setShowCurrentPassword(false)
    setShowNewPassword(false)
    setShowConfirmPassword(false)
    onClose()
  }

  const onSubmit = async (data: ChangePasswordFormData) => {
    try {
      await authService.changePassword({
        currentPassword: data.currentPassword,
        newPassword: data.newPassword,
        confirmPassword: data.confirmPassword,
      })

      showSuccess(t('auth.change_password.success_title'), t('auth.change_password.success_message'))
      reset()
      onClose()
    } catch (error: unknown) {
      logger.error('Failed to change password:', error)
      const message = error instanceof Error ? error.message : t('auth.change_password.error_message')
      showError(t('auth.change_password.error_title'), message)
    }
  }

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title={
        <div className="flex flex-col gap-1">
          <h3 className="text-lg sm:text-xl font-semibold text-gray-900 dark:text-gray-100">
            {t('auth.change_password.title')}
          </h3>
          <p className="text-sm text-gray-600 dark:text-gray-400">
            {t('auth.change_password.subtitle')}
          </p>
        </div>
      }
      size="md"
    >
      <form onSubmit={handleSubmit(onSubmit)} className="p-6">
        <div className="space-y-5">
          {/* Current password */}
          <div className="space-y-2">
            <label
              htmlFor="currentPassword"
              className="block text-sm font-medium text-gray-700 dark:text-gray-200"
            >
              {t('auth.fields.current_password')}
            </label>
            <div className="relative">
              <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                <Lock className="h-5 w-5 text-gray-400 dark:text-gray-500" />
              </div>
              <input
                id="currentPassword"
                type={showCurrentPassword ? 'text' : 'password'}
                autoComplete="current-password"
                disabled={isSubmitting}
                {...register('currentPassword', {
                  required: t('auth.validation.current_password_required'),
                })}
                className={`form-input w-full pl-10 pr-10 ${errors.currentPassword ? 'form-input-error' : ''}`}
                placeholder={t('auth.placeholders.current_password')}
              />
              <button
                type="button"
                className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-gray-500 dark:text-gray-500 dark:hover:text-gray-300"
                onClick={() => setShowCurrentPassword(!showCurrentPassword)}
                aria-label={t('auth.fields.current_password')}
              >
                {showCurrentPassword ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
              </button>
            </div>
            {errors.currentPassword && (
              <p className="text-sm text-red-600 dark:text-red-400">{errors.currentPassword.message}</p>
            )}
          </div>

          {/* New password */}
          <div className="space-y-2">
            <label
              htmlFor="newPassword"
              className="block text-sm font-medium text-gray-700 dark:text-gray-200"
            >
              {t('auth.fields.new_password')}
            </label>
            <div className="relative">
              <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                <Lock className="h-5 w-5 text-gray-400 dark:text-gray-500" />
              </div>
              <input
                id="newPassword"
                type={showNewPassword ? 'text' : 'password'}
                autoComplete="new-password"
                disabled={isSubmitting}
                {...register('newPassword', {
                  required: t('auth.validation.password_required'),
                  validate: {
                    // Same configuration-driven rules the backend enforces.
                    dynamicValidation: (value: string) => {
                      const result = validatePassword(value)
                      if (result.isValid) return true

                      const [errorKey, param] = result.errors[0].split('#')
                      return t(errorKey as keyof TranslationKeys, param ? { min: param } : undefined)
                    },
                    mustDiffer: (value: string, formValues: ChangePasswordFormData) =>
                      value !== formValues.currentPassword ||
                      t('auth.validation.password_must_differ'),
                  },
                })}
                className={`form-input w-full pl-10 pr-10 ${errors.newPassword ? 'form-input-error' : ''}`}
                placeholder={t('auth.placeholders.new_password')}
              />
              <button
                type="button"
                className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-gray-500 dark:text-gray-500 dark:hover:text-gray-300"
                onClick={() => setShowNewPassword(!showNewPassword)}
                aria-label={t('auth.fields.new_password')}
              >
                {showNewPassword ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
              </button>
            </div>
            {errors.newPassword && (
              <p className="text-sm text-red-600 dark:text-red-400">{errors.newPassword.message}</p>
            )}
            {newPassword && passwordRequirements.length > 0 && (
              <div className="p-2 bg-gray-50 dark:bg-gray-800/80 rounded text-xs text-gray-600 dark:text-gray-300">
                <p className="font-medium mb-1 text-gray-700 dark:text-gray-200">
                  {t('auth.requirements.password_title')}
                </p>
                <ul className="list-disc list-inside space-y-0.5">
                  {passwordRequirements.map((requirement, index) => (
                    <li key={index}>{requirement}</li>
                  ))}
                </ul>
              </div>
            )}
          </div>

          {/* Confirm new password */}
          <div className="space-y-2">
            <label
              htmlFor="confirmPassword"
              className="block text-sm font-medium text-gray-700 dark:text-gray-200"
            >
              {t('auth.fields.confirm_password')}
            </label>
            <div className="relative">
              <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                <Lock className="h-5 w-5 text-gray-400 dark:text-gray-500" />
              </div>
              <input
                id="confirmPassword"
                type={showConfirmPassword ? 'text' : 'password'}
                autoComplete="new-password"
                disabled={isSubmitting}
                {...register('confirmPassword', {
                  required: t('auth.validation.confirm_password_required'),
                  validate: {
                    passwordMatch: (value: string, formValues: ChangePasswordFormData) =>
                      value === formValues.newPassword ||
                      t('auth.validation.passwords_do_not_match'),
                  },
                })}
                className={`form-input w-full pl-10 pr-10 ${errors.confirmPassword ? 'form-input-error' : ''}`}
                placeholder={t('auth.placeholders.confirm_password')}
              />
              <button
                type="button"
                className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-gray-500 dark:text-gray-500 dark:hover:text-gray-300"
                onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                aria-label={t('auth.fields.confirm_password')}
              >
                {showConfirmPassword ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
              </button>
            </div>
            {errors.confirmPassword && (
              <p className="text-sm text-red-600 dark:text-red-400">{errors.confirmPassword.message}</p>
            )}
          </div>

          <div className="flex gap-3 justify-end pt-2">
            <button type="button" onClick={handleClose} className="btn-secondary" disabled={isSubmitting}>
              {t('common.cancel')}
            </button>
            <button type="submit" className="btn-primary" disabled={isSubmitting}>
              {isSubmitting ? (
                <span className="flex items-center gap-2">
                  <span className="h-4 w-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
                  {t('auth.change_password.changing')}
                </span>
              ) : (
                t('auth.change_password.change')
              )}
            </button>
          </div>
        </div>
      </form>
    </Modal>
  )
}
