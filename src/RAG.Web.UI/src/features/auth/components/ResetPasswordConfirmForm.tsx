import React, { useState, useEffect } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { Lock, ArrowLeft, CheckCircle } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import { useAuth } from '@/shared/contexts/AuthContext'
import { useToast } from '@/shared/contexts/ToastContext'
import { PasswordInput } from '@/features/settings/components/PasswordInput'
import { getPasswordStrength, validatePasswordRequirements } from '@/utils/passwordValidation'
import { usePasswordValidation } from '@/shared/contexts/ConfigurationContext'

interface ResetPasswordConfirmData {
  newPassword: string
  confirmPassword: string
}

interface ValidationErrors {
  newPassword?: string
  confirmPassword?: string
}

export function ResetPasswordConfirmForm() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const { t } = useI18n()
  const { confirmPasswordReset } = useAuth()
  const { addToast } = useToast()
  const { passwordRequirements } = usePasswordValidation()

  const [formData, setFormData] = useState<ResetPasswordConfirmData>({
    newPassword: '',
    confirmPassword: ''
  })

  const [errors, setErrors] = useState<ValidationErrors>({})
  const [loading, setLoading] = useState(false)
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)
  const [isSuccess, setIsSuccess] = useState(false)
  const [token, setToken] = useState<string>('')
  const passwordStrength = getPasswordStrength(formData.newPassword, passwordRequirements)
  const passwordValidation = validatePasswordRequirements(formData.newPassword, passwordRequirements)
  const passwordsMatch = formData.newPassword && formData.confirmPassword && formData.newPassword === formData.confirmPassword
  const passwordsMismatch = formData.newPassword && formData.confirmPassword && formData.newPassword !== formData.confirmPassword
  const canSubmit = !loading && Boolean(token) && Boolean(passwordsMatch) && passwordValidation.isValid

  useEffect(() => {
    const tokenParam = searchParams.get('token')
    if (tokenParam) {
      setToken(tokenParam)
    } else {
      addToast({
        type: 'error',
        title: 'Invalid Reset Link',
        message: 'The password reset link is invalid or expired.'
      })
      navigate('/login')
    }
  }, [searchParams, addToast, navigate])

  const validateForm = (): boolean => {
    const newErrors: ValidationErrors = {}

    if (!passwordValidation.isValid) {
      newErrors.newPassword = passwordRequirements?.validationMessage || t('auth.validation.password_required')
    }

    if (!formData.confirmPassword.trim()) {
      newErrors.confirmPassword = t('auth.validation.confirm_password_required')
    } else if (!passwordsMatch) {
      newErrors.confirmPassword = t('auth.validation.passwords_do_not_match')
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handlePasswordChange = (name: keyof ResetPasswordConfirmData, value: string) => {
    setFormData(prev => ({
      ...prev,
      [name]: value
    }))

    // Clear error when user starts typing
    if (errors[name as keyof ValidationErrors]) {
      setErrors(prev => ({ ...prev, [name]: undefined }))
    }
  }

  const getConfirmPasswordStatus = () => {
    if (!formData.confirmPassword) return 'none'
    return passwordsMatch ? 'match' : 'mismatch'
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) {
      return
    }

    setLoading(true)

    try {
      await confirmPasswordReset({
        token,
        NewPassword: formData.newPassword,
        ConfirmNewPassword: formData.confirmPassword
      })

      setIsSuccess(true)

      addToast({
        type: 'success',
        title: t('auth.reset_confirm.success_title'),
        message: t('auth.reset_confirm.success_message')
      })

      // Redirect to login after a delay
      setTimeout(() => {
        navigate('/login')
      }, 3000)
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to reset password. The link may be expired.'
      addToast({
        type: 'error',
        title: 'Password Reset Failed',
        message: errorMessage
      })
    } finally {
      setLoading(false)
    }
  }

  if (isSuccess) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900 py-12 px-4 sm:px-6 lg:px-8">
        <div className="max-w-md w-full space-y-8 text-gray-900 dark:text-gray-100">
          <div className="text-center">
            <div className="mx-auto h-12 w-12 flex items-center justify-center rounded-full bg-green-100 dark:bg-green-900/30">
              <CheckCircle className="h-6 w-6 text-green-600 dark:text-green-400" />
            </div>
            <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900 dark:text-gray-100">
              {t('auth.reset_confirm.success_title')}
            </h2>
            <p className="mt-2 text-center text-sm text-gray-600 dark:text-gray-300">
              {t('auth.reset_confirm.success_message')}
            </p>
            <p className="mt-4 text-center text-sm text-gray-500 dark:text-gray-400">
              {t('auth.reset_confirm.redirect_message')}
            </p>
            <div className="mt-6">
              <Link
                to="/login"
                className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 dark:bg-blue-500 dark:hover:bg-blue-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 dark:focus:ring-blue-400 focus:ring-offset-white dark:focus:ring-offset-gray-900 transition-colors"
              >
                <ArrowLeft className="h-4 w-4 mr-2" />
                {t('auth.reset_confirm.back_to_login')}
              </Link>
            </div>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8 text-gray-900 dark:text-gray-100">
        <div>
          <div className="mx-auto h-12 w-12 flex items-center justify-center rounded-full bg-blue-100 dark:bg-blue-900/30">
            <Lock className="h-6 w-6 text-blue-600 dark:text-blue-400" />
          </div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900 dark:text-gray-100">
            {t('auth.reset_confirm.title')}
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600 dark:text-gray-300">
            {t('auth.reset_confirm.subtitle')}
          </p>
        </div>

        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          <div>
            <PasswordInput
              value={formData.newPassword}
              onChange={(value) => handlePasswordChange('newPassword', value)}
              showPassword={showPassword}
              onToggleShow={() => setShowPassword(!showPassword)}
              placeholder={t('auth.placeholders.new_password')}
              label={t('auth.fields.new_password')}
              strength={passwordStrength}
              passwordRequirements={passwordRequirements}
            />
            {errors.newPassword && (
              <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.newPassword}</p>
            )}
          </div>

          <div>
            <PasswordInput
              value={formData.confirmPassword}
              onChange={(value) => handlePasswordChange('confirmPassword', value)}
              showPassword={showConfirmPassword}
              onToggleShow={() => setShowConfirmPassword(!showConfirmPassword)}
              placeholder={t('auth.placeholders.confirm_password')}
              label={t('auth.fields.confirm_password')}
              matchStatus={getConfirmPasswordStatus()}
              showRequirements={false}
            />
            {errors.confirmPassword && (
              <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.confirmPassword}</p>
            )}
          </div>

          <div>
            <button
              type="submit"
              disabled={!canSubmit || Boolean(passwordsMismatch)}
              className="group relative w-full flex justify-center py-3 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 dark:bg-blue-500 dark:hover:bg-blue-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 dark:focus:ring-blue-400 focus:ring-offset-white dark:focus:ring-offset-gray-900 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {loading ? (
                <div className="flex items-center">
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                  {t('auth.reset_confirm.resetting')}
                </div>
              ) : (
                <div className="flex items-center">
                  <Lock className="h-4 w-4 mr-2" />
                  {t('auth.reset_confirm.reset_password')}
                </div>
              )}
            </button>
          </div>

          <div className="text-center">
            <Link
              to="/login"
              className="font-medium text-blue-600 hover:text-blue-500 dark:text-blue-400 dark:hover:text-blue-300 text-sm"
            >
              <ArrowLeft className="h-4 w-4 inline mr-1" />
              {t('auth.reset_confirm.back_to_login')}
            </Link>
          </div>
        </form>
      </div>
    </div>
  )
}
