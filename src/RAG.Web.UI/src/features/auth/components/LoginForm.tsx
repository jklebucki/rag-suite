import React, { useState } from 'react'
import { Link, useNavigate, useLocation } from 'react-router-dom'
import { Eye, EyeOff, Mail, Lock, LogIn } from 'lucide-react'
import { useAuth } from '@/shared/contexts/AuthContext'
import { useI18n } from '@/shared/contexts/I18nContext'
import { useToast } from '@/shared/contexts/ToastContext'
import { validateEmail, validatePassword } from '@/utils/validation'
import type { LoginRequest } from '@/features/auth/types/auth'

interface LoginFormProps {
  onSuccess?: () => void
}

export function LoginForm({ onSuccess }: LoginFormProps) {
  const { login, loading, error, clearError } = useAuth()
  const { t } = useI18n()
  const { addToast } = useToast()
  const navigate = useNavigate()
  const location = useLocation()

  const [formData, setFormData] = useState<LoginRequest>({
    email: '',
    password: '',
    rememberMe: false,
  })
  const [showPassword, setShowPassword] = useState(false)
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({})

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value, type, checked } = e.target
    setFormData(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value
    }))

    // Clear validation error when user starts typing
    if (validationErrors[name]) {
      setValidationErrors(prev => ({
        ...prev,
        [name]: ''
      }))
    }

    // Clear general error
    if (error) {
      clearError()
    }
  }

  const validateForm = (): boolean => {
    const errors: Record<string, string> = {}

    // Email validation using utility
    if (!formData.email) {
      errors.email = t('auth.validation.email_required')
    } else if (!validateEmail(formData.email)) {
      errors.email = t('auth.validation.email_invalid')
    }

    // Password validation using utility
    const passwordValidation = validatePassword(formData.password, 6)
    if (!passwordValidation.isValid) {
      errors.password = passwordValidation.error || t('auth.validation.password_required')
    }

    setValidationErrors(errors)
    return Object.keys(errors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) {
      return
    }

    const success = await login(formData)

    if (success) {
      addToast({
        type: 'success',
        title: t('auth.login.success_title'),
        message: t('auth.login.success_message')
      })
      onSuccess?.()

      // Navigate back to the page that required auth, if provided
      const from = (location.state as { from?: { pathname: string } })?.from?.pathname || '/'
      navigate(from, { replace: true })
    }
  }

  return (
    <div className="w-full max-w-md mx-auto text-gray-900 dark:text-gray-100">
      <div className="text-center mb-8">
        <div className="inline-flex items-center justify-center w-16 h-16 bg-blue-100 dark:bg-blue-900/30 rounded-full mb-4">
          <LogIn className="w-8 h-8 text-blue-600 dark:text-blue-400" />
        </div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-2">
          {t('auth.login.title')}
        </h2>
        <p className="text-gray-600 dark:text-gray-300">
          {t('auth.login.subtitle')}
        </p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Email Field */}
        <div>
          <label htmlFor="email" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
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
              onChange={handleChange}
              className={`form-input pl-10 pr-3 ${
                validationErrors.email ? 'form-input-error' : ''
              }`}
              placeholder={t('auth.placeholders.email')}
            />
          </div>
          {validationErrors.email && (
            <p className="mt-1 text-sm text-red-600 dark:text-red-400">{validationErrors.email}</p>
          )}
        </div>

        {/* Password Field */}
        <div>
          <label htmlFor="password" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            {t('auth.fields.password')}
          </label>
          <div className="relative">
            <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
              <Lock className="h-5 w-5 text-gray-400 dark:text-gray-500" />
            </div>
            <input
              id="password"
              name="password"
              type={showPassword ? 'text' : 'password'}
              autoComplete="current-password"
              required
              value={formData.password}
              onChange={handleChange}
              className={`form-input pl-10 pr-10 ${
                validationErrors.password ? 'form-input-error' : ''
              }`}
              placeholder={t('auth.placeholders.password')}
            />
            <button
              type="button"
              aria-label={showPassword ? t('auth.login.hide_password') : t('auth.login.show_password')}
              className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-gray-600 dark:text-gray-500 dark:hover:text-gray-300"
              onClick={() => setShowPassword(!showPassword)}
            >
              {showPassword ? (
                <EyeOff className="h-5 w-5" />
              ) : (
                <Eye className="h-5 w-5" />
              )}
            </button>
          </div>
          {validationErrors.password && (
            <p className="mt-1 text-sm text-red-600 dark:text-red-400">{validationErrors.password}</p>
          )}
        </div>

        {/* Remember Me & Forgot Password */}
        <div className="flex items-center justify-between">
          <label className="flex items-center">
            <input
              type="checkbox"
              name="rememberMe"
              checked={formData.rememberMe}
              onChange={handleChange}
              className="form-checkbox"
            />
            <span className="ml-2 text-sm text-gray-700 dark:text-gray-300">
              {t('auth.login.remember_me')}
            </span>
          </label>

          <Link
            to="/forgot-password"
            className="text-sm text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 font-medium"
          >
            {t('auth.login.forgot_password')}
          </Link>
        </div>

        {/* Error Message */}
        {error && (
          <div className="bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-700 rounded-lg p-3">
            <p className="text-sm text-red-600 dark:text-red-400">{error}</p>
          </div>
        )}

        {/* Submit Button */}
        <button
          type="submit"
          disabled={loading}
          className="w-full flex justify-center items-center py-3 px-4 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 dark:bg-blue-500 dark:hover:bg-blue-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 dark:focus:ring-blue-400 focus:ring-offset-white dark:focus:ring-offset-gray-900 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
        >
          {loading ? (
            <div className="flex items-center">
              <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
              {t('auth.login.signing_in')}
            </div>
          ) : (
            <div className="flex items-center">
              <LogIn className="w-4 h-4 mr-2" />
              {t('auth.login.sign_in')}
            </div>
          )}
        </button>

        {/* Register Link */}
        <div className="text-center">
          <span className="text-sm text-gray-600 dark:text-gray-300">
            {t('auth.login.no_account')}{' '}
          </span>
          <Link
            to="/register"
            className="text-sm text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 font-medium"
          >
            {t('auth.login.sign_up')}
          </Link>
        </div>
      </form>
    </div>
  )
}
