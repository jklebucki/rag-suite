import React, { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Eye, EyeOff, User, Mail, Lock, Check } from 'lucide-react'
import { useI18n } from '@/contexts/I18nContext'
import { useAuth } from '@/contexts/AuthContext'
import { useToast } from '@/contexts/ToastContext'
import { useConfiguration, usePasswordValidation } from '@/contexts/ConfigurationContext'

interface RegisterData {
  firstName: string
  lastName: string
  userName: string
  email: string
  password: string
  confirmPassword: string
  acceptTerms: boolean
}

interface ValidationErrors {
  firstName?: string
  lastName?: string
  userName?: string
  email?: string
  password?: string
  confirmPassword?: string
  acceptTerms?: string
}

export function RegisterForm() {
  const navigate = useNavigate()
  const { t } = useI18n()
  const { register } = useAuth()
  const { addToast } = useToast()
  const { configuration } = useConfiguration()
  const { validatePassword } = usePasswordValidation()

  const [formData, setFormData] = useState<RegisterData>({
    firstName: '',
    lastName: '',
    userName: '',
    email: '',
    password: '',
    confirmPassword: '',
    acceptTerms: false
  })

  const [errors, setErrors] = useState<ValidationErrors>({})
  const [loading, setLoading] = useState(false)
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)

  const validateForm = (): boolean => {
    const newErrors: ValidationErrors = {}

    if (!configuration) {
      newErrors.firstName = 'Configuration not loaded'
      setErrors(newErrors)
      return false
    }

    const { userFieldRequirements } = configuration

    // First name validation
    if (!formData.firstName.trim()) {
      newErrors.firstName = t('auth.validation.first_name_required')
    } else if (userFieldRequirements.firstName.maxLength && formData.firstName.length > userFieldRequirements.firstName.maxLength) {
      newErrors.firstName = `First name cannot exceed ${userFieldRequirements.firstName.maxLength} characters`
    }

    // Last name validation
    if (!formData.lastName.trim()) {
      newErrors.lastName = t('auth.validation.last_name_required')
    } else if (userFieldRequirements.lastName.maxLength && formData.lastName.length > userFieldRequirements.lastName.maxLength) {
      newErrors.lastName = `Last name cannot exceed ${userFieldRequirements.lastName.maxLength} characters`
    }

    // Username validation
    if (!formData.userName.trim()) {
      newErrors.userName = t('auth.validation.username_required')
    } else {
      const minLength = userFieldRequirements.userName.minLength || 3
      const maxLength = userFieldRequirements.userName.maxLength || 50

      if (formData.userName.trim().length < minLength) {
        newErrors.userName = t('auth.validation.username_min_length')
      } else if (formData.userName.length > maxLength) {
        newErrors.userName = `Username cannot exceed ${maxLength} characters`
      }
    }

    // Email validation
    if (!formData.email.trim()) {
      newErrors.email = t('auth.validation.email_required')
    } else if (userFieldRequirements.email.pattern && !new RegExp(userFieldRequirements.email.pattern).test(formData.email)) {
      newErrors.email = t('auth.validation.email_invalid')
    } else if (userFieldRequirements.email.maxLength && formData.email.length > userFieldRequirements.email.maxLength) {
      newErrors.email = `Email cannot exceed ${userFieldRequirements.email.maxLength} characters`
    }

    // Password validation using dynamic validation
    if (!formData.password) {
      newErrors.password = t('auth.validation.password_required')
    } else {
      const passwordValidation = validatePassword(formData.password)
      if (!passwordValidation.isValid && passwordValidation.errors.length > 0) {
        // Map validation errors to user-friendly messages
        const errorKey = passwordValidation.errors[0]

        if (errorKey.includes('password_min_length')) {
          const minLength = configuration.passwordRequirements.requiredLength
          newErrors.password = `Password must be at least ${minLength} characters`
        } else if (errorKey.includes('password_require_digit')) {
          newErrors.password = t('auth.validation.password_require_digit')
        } else if (errorKey.includes('password_require_uppercase')) {
          newErrors.password = t('auth.validation.password_require_uppercase')
        } else if (errorKey.includes('password_require_lowercase')) {
          newErrors.password = t('auth.validation.password_require_lowercase')
        } else if (errorKey.includes('password_require_special')) {
          newErrors.password = t('auth.validation.password_require_special')
        } else {
          newErrors.password = t('auth.validation.password_min_length')
        }
      }
    }

    // Confirm password validation
    if (formData.password !== formData.confirmPassword) {
      newErrors.confirmPassword = t('auth.validation.password_mismatch')
    }

    // Terms validation
    if (!formData.acceptTerms) {
      newErrors.acceptTerms = t('auth.validation.terms_required')
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value, type, checked } = e.target
    setFormData(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value
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
      await register({
        firstName: formData.firstName,
        lastName: formData.lastName,
        userName: formData.userName,
        email: formData.email,
        password: formData.password,
        confirmPassword: formData.confirmPassword,
        acceptTerms: formData.acceptTerms
      })

      addToast({
        type: 'success',
        title: t('auth.register.success_title'),
        message: t('auth.register.success_message')
      })

      // Reset form
      setFormData({
        firstName: '',
        lastName: '',
        userName: '',
        email: '',
        password: '',
        confirmPassword: '',
        acceptTerms: false
      })
    } catch (error: any) {
      addToast({
        type: 'error',
        title: 'Registration Error',
        message: error.message || 'Registration failed. Please try again.'
      })
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <div>
          <div className="mx-auto h-12 w-12 flex items-center justify-center rounded-full bg-blue-100">
            <User className="h-6 w-6 text-blue-600" />
          </div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            {t('auth.register.title')}
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            {t('auth.register.subtitle')}
          </p>
        </div>

        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          <div className="space-y-4">
            {/* First Name */}
            <div>
              <label htmlFor="firstName" className="sr-only">
                {t('auth.fields.first_name')}
              </label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <User className="h-5 w-5 text-gray-400" />
                </div>
                <input
                  id="firstName"
                  name="firstName"
                  type="text"
                  required
                  value={formData.firstName}
                  onChange={handleInputChange}
                  className={`appearance-none rounded-md relative block w-full pl-10 pr-3 py-2 border ${
                    errors.firstName ? 'border-red-300' : 'border-gray-300'
                  } placeholder-gray-500 text-gray-900 focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm`}
                  placeholder={t('auth.placeholders.first_name')}
                />
              </div>
              {errors.firstName && (
                <p className="mt-1 text-sm text-red-600">{errors.firstName}</p>
              )}
            </div>

            {/* Last Name */}
            <div>
              <label htmlFor="lastName" className="sr-only">
                {t('auth.fields.last_name')}
              </label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <User className="h-5 w-5 text-gray-400" />
                </div>
                <input
                  id="lastName"
                  name="lastName"
                  type="text"
                  required
                  value={formData.lastName}
                  onChange={handleInputChange}
                  className={`appearance-none rounded-md relative block w-full pl-10 pr-3 py-2 border ${
                    errors.lastName ? 'border-red-300' : 'border-gray-300'
                  } placeholder-gray-500 text-gray-900 focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm`}
                  placeholder={t('auth.placeholders.last_name')}
                />
              </div>
              {errors.lastName && (
                <p className="mt-1 text-sm text-red-600">{errors.lastName}</p>
              )}
            </div>

            {/* Username */}
            <div>
              <label htmlFor="userName" className="sr-only">
                {t('auth.fields.username')}
              </label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <User className="h-5 w-5 text-gray-400" />
                </div>
                <input
                  id="userName"
                  name="userName"
                  type="text"
                  required
                  value={formData.userName}
                  onChange={handleInputChange}
                  className={`appearance-none rounded-md relative block w-full pl-10 pr-3 py-2 border ${
                    errors.userName ? 'border-red-300' : 'border-gray-300'
                  } placeholder-gray-500 text-gray-900 focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm`}
                  placeholder={t('auth.placeholders.username')}
                />
              </div>
              {errors.userName && (
                <p className="mt-1 text-sm text-red-600">{errors.userName}</p>
              )}
            </div>

            {/* Email */}
            <div>
              <label htmlFor="email" className="sr-only">
                {t('auth.fields.email')}
              </label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <Mail className="h-5 w-5 text-gray-400" />
                </div>
                <input
                  id="email"
                  name="email"
                  type="email"
                  autoComplete="email"
                  required
                  value={formData.email}
                  onChange={handleInputChange}
                  className={`appearance-none rounded-md relative block w-full pl-10 pr-3 py-2 border ${
                    errors.email ? 'border-red-300' : 'border-gray-300'
                  } placeholder-gray-500 text-gray-900 focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm`}
                  placeholder={t('auth.placeholders.email')}
                />
              </div>
              {errors.email && (
                <p className="mt-1 text-sm text-red-600">{errors.email}</p>
              )}
            </div>

            {/* Password */}
            <div>
              <label htmlFor="password" className="sr-only">
                {t('auth.fields.password')}
              </label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <Lock className="h-5 w-5 text-gray-400" />
                </div>
                <input
                  id="password"
                  name="password"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="new-password"
                  required
                  value={formData.password}
                  onChange={handleInputChange}
                  className={`appearance-none rounded-md relative block w-full pl-10 pr-10 py-2 border ${
                    errors.password ? 'border-red-300' : 'border-gray-300'
                  } placeholder-gray-500 text-gray-900 focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm`}
                  placeholder={t('auth.placeholders.password')}
                />
                <button
                  type="button"
                  className="absolute inset-y-0 right-0 pr-3 flex items-center"
                  onClick={() => setShowPassword(!showPassword)}
                >
                  {showPassword ? (
                    <EyeOff className="h-5 w-5 text-gray-400 hover:text-gray-500" />
                  ) : (
                    <Eye className="h-5 w-5 text-gray-400 hover:text-gray-500" />
                  )}
                </button>
              </div>
              {errors.password && (
                <p className="mt-1 text-sm text-red-600">{errors.password}</p>
              )}
            </div>

            {/* Confirm Password */}
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
                  type={showConfirmPassword ? 'text' : 'password'}
                  autoComplete="new-password"
                  required
                  value={formData.confirmPassword}
                  onChange={handleInputChange}
                  className={`appearance-none rounded-md relative block w-full pl-10 pr-10 py-2 border ${
                    errors.confirmPassword ? 'border-red-300' : 'border-gray-300'
                  } placeholder-gray-500 text-gray-900 focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm`}
                  placeholder={t('auth.placeholders.confirm_password')}
                />
                <button
                  type="button"
                  className="absolute inset-y-0 right-0 pr-3 flex items-center"
                  onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                >
                  {showConfirmPassword ? (
                    <EyeOff className="h-5 w-5 text-gray-400 hover:text-gray-500" />
                  ) : (
                    <Eye className="h-5 w-5 text-gray-400 hover:text-gray-500" />
                  )}
                </button>
              </div>
              {errors.confirmPassword && (
                <p className="mt-1 text-sm text-red-600">{errors.confirmPassword}</p>
              )}
            </div>

            {/* Terms acceptance */}
            <div className="flex items-center">
              <input
                id="acceptTerms"
                name="acceptTerms"
                type="checkbox"
                checked={formData.acceptTerms}
                onChange={handleInputChange}
                className={`h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded ${
                  errors.acceptTerms ? 'border-red-300' : ''
                }`}
              />
              <label htmlFor="acceptTerms" className="ml-2 block text-sm text-gray-900">
                {t('auth.register.accept_terms')}
              </label>
            </div>
            {errors.acceptTerms && (
              <p className="text-sm text-red-600">{errors.acceptTerms}</p>
            )}
          </div>

          <div>
            <button
              type="submit"
              disabled={loading}
              className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {loading ? (
                <div className="flex items-center">
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                  {t('auth.register.signing_up')}
                </div>
              ) : (
                <div className="flex items-center">
                  <Check className="h-4 w-4 mr-2" />
                  {t('auth.register.sign_up')}
                </div>
              )}
            </button>
          </div>

          <div className="text-center">
            <span className="text-sm text-gray-600">
              {t('auth.register.have_account')}{' '}
              <Link
                to="/login"
                className="font-medium text-blue-600 hover:text-blue-500"
              >
                {t('auth.register.sign_in')}
              </Link>
            </span>
          </div>
        </form>
      </div>
    </div>
  )
}
