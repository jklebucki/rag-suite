import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { Eye, EyeOff, User, Mail, Lock, Check } from 'lucide-react'
import { useI18n } from '@/contexts/I18nContext'
import { useAuth } from '@/contexts/AuthContext'
import { useToast } from '@/contexts/ToastContext'
import { useRegisterValidation, usePasswordRequirements } from '@/utils/registerValidation'

interface RegisterFormData {
  firstName: string
  lastName: string
  userName: string
  email: string
  password: string
  confirmPassword: string
  acceptTerms: boolean
}

export function RegisterForm() {
  const { t } = useI18n()
  const { register: registerUser } = useAuth()
  const { addToast } = useToast()
  const validationRules = useRegisterValidation()
  const passwordRequirements = usePasswordRequirements()

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    reset,
    watch,
  } = useForm<RegisterFormData>({
    mode: 'onBlur',
    defaultValues: {
      firstName: '',
      lastName: '',
      userName: '',
      email: '',
      password: '',
      confirmPassword: '',
      acceptTerms: false,
    },
  })

  const [showPassword, setShowPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)

  const password = watch('password')

  const onSubmit = async (data: RegisterFormData) => {
    try {
      await registerUser({
        firstName: data.firstName,
        lastName: data.lastName,
        userName: data.userName,
        email: data.email,
        password: data.password,
        confirmPassword: data.confirmPassword,
        acceptTerms: data.acceptTerms,
      })

      addToast({
        type: 'success',
        title: t('auth.register.success_title'),
        message: t('auth.register.success_message'),
      })

      reset()
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Registration failed. Please try again.'
      addToast({
        type: 'error',
        title: 'Registration Error',
        message: errorMessage,
      })
    }
  }

  if (!validationRules) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-gray-600">Loading configuration...</div>
      </div>
    )
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

        <form className="mt-8 space-y-6" onSubmit={handleSubmit(onSubmit)}>
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
                  type="text"
                  {...register('firstName', validationRules.firstName)}
                  className={`appearance-none rounded-md relative block w-full pl-10 pr-3 py-2 border ${
                    errors.firstName ? 'border-red-300' : 'border-gray-300'
                  } placeholder-gray-500 text-gray-900 focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm`}
                  placeholder={t('auth.placeholders.first_name')}
                />
              </div>
              {errors.firstName && (
                <p className="mt-1 text-sm text-red-600">{errors.firstName.message}</p>
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
                  type="text"
                  {...register('lastName', validationRules.lastName)}
                  className={`appearance-none rounded-md relative block w-full pl-10 pr-3 py-2 border ${
                    errors.lastName ? 'border-red-300' : 'border-gray-300'
                  } placeholder-gray-500 text-gray-900 focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm`}
                  placeholder={t('auth.placeholders.last_name')}
                />
              </div>
              {errors.lastName && (
                <p className="mt-1 text-sm text-red-600">{errors.lastName.message}</p>
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
                  type="text"
                  {...register('userName', validationRules.userName)}
                  className={`appearance-none rounded-md relative block w-full pl-10 pr-3 py-2 border ${
                    errors.userName ? 'border-red-300' : 'border-gray-300'
                  } placeholder-gray-500 text-gray-900 focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm`}
                  placeholder={t('auth.placeholders.username')}
                />
              </div>
              {errors.userName && (
                <p className="mt-1 text-sm text-red-600">{errors.userName.message}</p>
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
                  type="email"
                  autoComplete="email"
                  {...register('email', validationRules.email)}
                  className={`appearance-none rounded-md relative block w-full pl-10 pr-3 py-2 border ${
                    errors.email ? 'border-red-300' : 'border-gray-300'
                  } placeholder-gray-500 text-gray-900 focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm`}
                  placeholder={t('auth.placeholders.email')}
                />
              </div>
              {errors.email && (
                <p className="mt-1 text-sm text-red-600">{errors.email.message}</p>
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
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="new-password"
                  {...register('password', validationRules.password)}
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
                <p className="mt-1 text-sm text-red-600">{errors.password.message}</p>
              )}
              {password && passwordRequirements.length > 0 && (
                <div className="mt-2 p-2 bg-gray-50 rounded text-xs text-gray-600">
                  <p className="font-medium mb-1">Password must contain:</p>
                  <ul className="list-disc list-inside space-y-0.5">
                    {passwordRequirements.map((req, index) => (
                      <li key={index}>{req}</li>
                    ))}
                  </ul>
                </div>
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
                  type={showConfirmPassword ? 'text' : 'password'}
                  autoComplete="new-password"
                  {...register('confirmPassword', validationRules.confirmPassword)}
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
                <p className="mt-1 text-sm text-red-600">{errors.confirmPassword.message}</p>
              )}
            </div>

            {/* Terms acceptance */}
            <div className="flex items-center">
              <input
                id="acceptTerms"
                type="checkbox"
                {...register('acceptTerms', validationRules.acceptTerms)}
                className={`h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded ${
                  errors.acceptTerms ? 'border-red-300' : ''
                }`}
              />
              <label htmlFor="acceptTerms" className="ml-2 block text-sm text-gray-900">
                {t('auth.register.accept_terms')}
              </label>
            </div>
            {errors.acceptTerms && (
              <p className="text-sm text-red-600">{errors.acceptTerms.message}</p>
            )}
          </div>

          <div>
            <button
              type="submit"
              disabled={isSubmitting}
              className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isSubmitting ? (
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
