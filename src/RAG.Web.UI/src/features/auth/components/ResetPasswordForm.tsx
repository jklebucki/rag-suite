import React, { useState } from 'react'
import { Link } from 'react-router-dom'
import { Mail, ArrowLeft } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import { useAuth } from '@/shared/contexts/AuthContext'
import { useToast } from '@/shared/contexts/ToastContext'
import { validateEmail } from '@/utils/validation'

interface ResetPasswordData {
  email: string
}

interface ValidationErrors {
  email?: string
}

export function ResetPasswordForm() {
  const { t } = useI18n()
  const { resetPassword } = useAuth()
  const { addToast } = useToast()

  const [formData, setFormData] = useState<ResetPasswordData>({
    email: ''
  })

  const [errors, setErrors] = useState<ValidationErrors>({})
  const [loading, setLoading] = useState(false)
  const [isSuccess, setIsSuccess] = useState(false)

  const validateForm = (): boolean => {
    const newErrors: ValidationErrors = {}

    // Email validation using utility
    if (!formData.email.trim()) {
      newErrors.email = t('auth.validation.email_required')
    } else if (!validateEmail(formData.email)) {
      newErrors.email = t('auth.validation.email_invalid')
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
      await resetPassword({ email: formData.email, uiUrl: window.location.origin })

      setIsSuccess(true)

      addToast({
        type: 'success',
        title: t('auth.reset.success_title'),
        message: t('auth.reset.success_message')
      })
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to send reset instructions. Please try again.'
      addToast({
        type: 'error',
        title: 'Reset Password Error',
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
              <Mail className="h-6 w-6 text-green-600 dark:text-green-400" />
            </div>
            <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900 dark:text-gray-100">
              {t('auth.reset.success_title')}
            </h2>
            <p className="mt-2 text-center text-sm text-gray-600 dark:text-gray-300">
              {t('auth.reset.success_message')}
            </p>
            <div className="mt-6">
              <Link
                to="/login"
                className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 dark:bg-blue-500 dark:hover:bg-blue-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 dark:focus:ring-blue-400 focus:ring-offset-white dark:focus:ring-offset-gray-900 transition-colors"
              >
                <ArrowLeft className="h-4 w-4 mr-2" />
                {t('auth.reset.back_to_login')}
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
            <Mail className="h-6 w-6 text-blue-600 dark:text-blue-400" />
          </div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900 dark:text-gray-100">
            {t('auth.reset.title')}
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600 dark:text-gray-300">
            {t('auth.reset.subtitle')}
          </p>
        </div>

        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          <div>
            <label htmlFor="email" className="sr-only">
              {t('auth.fields.email')}
            </label>
            <div className="relative">
              <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                <Mail className="h-5 w-5 text-gray-400 dark:text-gray-500" />
              </div>
              <input
                id="email"
                name="email"
                type="email"
                autoComplete="email"
                required
                value={formData.email}
                onChange={handleInputChange}
                className={`form-input pl-10 pr-3 ${errors.email ? 'form-input-error' : ''}`}
                placeholder={t('auth.placeholders.email')}
              />
            </div>
            {errors.email && (
              <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.email}</p>
            )}
          </div>

          <div>
            <button
              type="submit"
              disabled={loading}
              className="group relative w-full flex justify-center py-3 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 dark:bg-blue-500 dark:hover:bg-blue-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 dark:focus:ring-blue-400 focus:ring-offset-white dark:focus:ring-offset-gray-900 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {loading ? (
                <div className="flex items-center">
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                  {t('auth.reset.sending')}
                </div>
              ) : (
                <div className="flex items-center">
                  <Mail className="h-4 w-4 mr-2" />
                  {t('auth.reset.send_instructions')}
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
              {t('auth.reset.back_to_login')}
            </Link>
          </div>
        </form>
      </div>
    </div>
  )
}
