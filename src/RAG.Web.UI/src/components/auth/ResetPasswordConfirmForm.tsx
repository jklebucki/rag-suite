import React, { useState, useEffect } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { Lock, ArrowLeft, CheckCircle } from 'lucide-react'
import { useI18n } from '@/contexts/I18nContext'
import { useAuth } from '@/contexts/AuthContext'
import { useToast } from '@/contexts/ToastContext'

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

  const [formData, setFormData] = useState<ResetPasswordConfirmData>({
    newPassword: '',
    confirmPassword: ''
  })

  const [errors, setErrors] = useState<ValidationErrors>({})
  const [loading, setLoading] = useState(false)
  const [isSuccess, setIsSuccess] = useState(false)
  const [token, setToken] = useState<string>('')

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

    // New password validation
    if (!formData.newPassword.trim()) {
      newErrors.newPassword = t('auth.validation.password_required')
    } else if (formData.newPassword.length < 6) {
      newErrors.newPassword = t('auth.validation.password_min_length')
    }

    // Confirm password validation
    if (!formData.confirmPassword.trim()) {
      newErrors.confirmPassword = t('auth.validation.confirm_password_required')
    } else if (formData.newPassword !== formData.confirmPassword) {
      newErrors.confirmPassword = t('auth.validation.passwords_do_not_match')
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target
    setFormData(prev => ({
      ...prev,
      [name]: value
    }))

    // Clear error when user starts typing
    if (errors[name as keyof ValidationErrors]) {
      setErrors(prev => ({ ...prev, [name]: undefined }))
    }
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
        newPassword: formData.newPassword,
        confirmPassword: formData.confirmPassword
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
    } catch (error: any) {
      addToast({
        type: 'error',
        title: 'Password Reset Failed',
        message: error.message || 'Failed to reset password. The link may be expired.'
      })
    } finally {
      setLoading(false)
    }
  }

  if (isSuccess) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
        <div className="max-w-md w-full space-y-8">
          <div className="text-center">
            <div className="mx-auto h-12 w-12 flex items-center justify-center rounded-full bg-green-100">
              <CheckCircle className="h-6 w-6 text-green-600" />
            </div>
            <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
              {t('auth.reset_confirm.success_title')}
            </h2>
            <p className="mt-2 text-center text-sm text-gray-600">
              {t('auth.reset_confirm.success_message')}
            </p>
            <p className="mt-4 text-center text-sm text-gray-500">
              {t('auth.reset_confirm.redirect_message')}
            </p>
            <div className="mt-6">
              <Link
                to="/login"
                className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
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
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <div>
          <div className="mx-auto h-12 w-12 flex items-center justify-center rounded-full bg-blue-100">
            <Lock className="h-6 w-6 text-blue-600" />
          </div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            {t('auth.reset_confirm.title')}
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            {t('auth.reset_confirm.subtitle')}
          </p>
        </div>

        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          <div>
            <label htmlFor="newPassword" className="sr-only">
              {t('auth.fields.new_password')}
            </label>
            <div className="relative">
              <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                <Lock className="h-5 w-5 text-gray-400" />
              </div>
              <input
                id="newPassword"
                name="newPassword"
                type="password"
                autoComplete="new-password"
                required
                value={formData.newPassword}
                onChange={handleInputChange}
                className={`appearance-none rounded-md relative block w-full pl-10 pr-3 py-3 border ${
                  errors.newPassword ? 'border-red-300' : 'border-gray-300'
                } placeholder-gray-500 text-gray-900 focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm`}
                placeholder={t('auth.placeholders.new_password')}
              />
            </div>
            {errors.newPassword && (
              <p className="mt-1 text-sm text-red-600">{errors.newPassword}</p>
            )}
          </div>

          <div>
            <label htmlFor="confirmPassword" className="sr-only">
              {t('auth.fields.confirm_password')}
            </label>
            <div className="relative">
              <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                <Lock className="h-5 w-5 text-gray-400" />
              </div>
              <input
                id="confirmPassword"
                name="confirmPassword"
                type="password"
                autoComplete="new-password"
                required
                value={formData.confirmPassword}
                onChange={handleInputChange}
                className={`appearance-none rounded-md relative block w-full pl-10 pr-3 py-3 border ${
                  errors.confirmPassword ? 'border-red-300' : 'border-gray-300'
                } placeholder-gray-500 text-gray-900 focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm`}
                placeholder={t('auth.placeholders.confirm_password')}
              />
            </div>
            {errors.confirmPassword && (
              <p className="mt-1 text-sm text-red-600">{errors.confirmPassword}</p>
            )}
          </div>

          <div>
            <button
              type="submit"
              disabled={loading}
              className="group relative w-full flex justify-center py-3 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
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
              className="font-medium text-blue-600 hover:text-blue-500 text-sm"
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
