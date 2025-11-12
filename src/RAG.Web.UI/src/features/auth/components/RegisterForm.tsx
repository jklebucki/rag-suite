import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { Eye, EyeOff, User, Mail, Lock, Check } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import { useAuth } from '@/shared/contexts/AuthContext'
import { useToast } from '@/shared/contexts/ToastContext'
import { useRegisterValidation, usePasswordRequirements } from '@/features/auth/hooks/useRegisterValidation'
import { SubmitButton } from '@/shared/components/ui/SubmitButton'

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
  const baseInputClasses = 'form-input'

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
      <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900">
        <div className="text-gray-600 dark:text-gray-300">Loading configuration...</div>
      </div>
    )
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8 text-gray-900 dark:text-gray-100">
        <div>
          <div className="mx-auto h-12 w-12 flex items-center justify-center rounded-full bg-blue-100 dark:bg-blue-900/30">
            <User className="h-6 w-6 text-blue-600 dark:text-blue-400" />
          </div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900 dark:text-gray-100">
            {t('auth.register.title')}
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600 dark:text-gray-300">
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
                  <User className="h-5 w-5 text-gray-400 dark:text-gray-500" />
                </div>
                <input
                  id="firstName"
                  type="text"
                  {...register('firstName', validationRules.firstName)}
                  className={`${baseInputClasses} pl-10 pr-3 ${errors.firstName ? 'form-input-error' : ''}`}
                  placeholder={t('auth.placeholders.first_name')}
                />
              </div>
              {errors.firstName && (
                <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.firstName.message}</p>
              )}
            </div>

            {/* Last Name */}
            <div>
              <label htmlFor="lastName" className="sr-only">
                {t('auth.fields.last_name')}
              </label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <User className="h-5 w-5 text-gray-400 dark:text-gray-500" />
                </div>
                <input
                  id="lastName"
                  type="text"
                  {...register('lastName', validationRules.lastName)}
                  className={`${baseInputClasses} pl-10 pr-3 ${errors.lastName ? 'form-input-error' : ''}`}
                  placeholder={t('auth.placeholders.last_name')}
                />
              </div>
              {errors.lastName && (
                <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.lastName.message}</p>
              )}
            </div>

            {/* Username */}
            <div>
              <label htmlFor="userName" className="sr-only">
                {t('auth.fields.username')}
              </label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <User className="h-5 w-5 text-gray-400 dark:text-gray-500" />
                </div>
                <input
                  id="userName"
                  type="text"
                  {...register('userName', validationRules.userName)}
                  className={`${baseInputClasses} pl-10 pr-3 ${errors.userName ? 'form-input-error' : ''}`}
                  placeholder={t('auth.placeholders.username')}
                />
              </div>
              {errors.userName && (
                <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.userName.message}</p>
              )}
            </div>

            {/* Email */}
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
                  type="email"
                  autoComplete="email"
                  {...register('email', validationRules.email)}
                  className={`${baseInputClasses} pl-10 pr-3 ${errors.email ? 'form-input-error' : ''}`}
                  placeholder={t('auth.placeholders.email')}
                />
              </div>
              {errors.email && (
                <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.email.message}</p>
              )}
            </div>

            {/* Password */}
            <div>
              <label htmlFor="password" className="sr-only">
                {t('auth.fields.password')}
              </label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <Lock className="h-5 w-5 text-gray-400 dark:text-gray-500" />
                </div>
                <input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="new-password"
                  {...register('password', validationRules.password)}
                  className={`${baseInputClasses} pl-10 pr-10 ${errors.password ? 'form-input-error' : ''}`}
                  placeholder={t('auth.placeholders.password')}
                />
                <button
                  type="button"
                  className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-gray-500 dark:text-gray-500 dark:hover:text-gray-300"
                  onClick={() => setShowPassword(!showPassword)}
                >
                  {showPassword ? (
                    <EyeOff className="h-5 w-5" />
                  ) : (
                    <Eye className="h-5 w-5" />
                  )}
                </button>
              </div>
              {errors.password && (
                <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.password.message}</p>
              )}
              {password && passwordRequirements.length > 0 && (
                <div className="mt-2 p-2 bg-gray-50 dark:bg-gray-800/80 rounded text-xs text-gray-600 dark:text-gray-300">
                  <p className="font-medium mb-1 text-gray-700 dark:text-gray-200">Password must contain:</p>
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
                  <Lock className="h-5 w-5 text-gray-400 dark:text-gray-500" />
                </div>
                <input
                  id="confirmPassword"
                  type={showConfirmPassword ? 'text' : 'password'}
                  autoComplete="new-password"
                  {...register('confirmPassword', validationRules.confirmPassword)}
                  className={`${baseInputClasses} pl-10 pr-10 ${errors.confirmPassword ? 'form-input-error' : ''}`}
                  placeholder={t('auth.placeholders.confirm_password')}
                />
                <button
                  type="button"
                  className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-gray-500 dark:text-gray-500 dark:hover:text-gray-300"
                  onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                >
                  {showConfirmPassword ? (
                    <EyeOff className="h-5 w-5" />
                  ) : (
                    <Eye className="h-5 w-5" />
                  )}
                </button>
              </div>
              {errors.confirmPassword && (
                <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.confirmPassword.message}</p>
              )}
            </div>

            {/* Terms acceptance */}
            <div className="flex items-center">
              <input
                id="acceptTerms"
                type="checkbox"
                {...register('acceptTerms', validationRules.acceptTerms)}
                className={`form-checkbox ${errors.acceptTerms ? 'border-red-300 dark:border-red-600' : ''}`}
              />
              <label htmlFor="acceptTerms" className="ml-2 block text-sm text-gray-900 dark:text-gray-300">
                {t('auth.register.accept_terms')}
              </label>
            </div>
            {errors.acceptTerms && (
              <p className="text-sm text-red-600 dark:text-red-400">{errors.acceptTerms.message}</p>
            )}
          </div>

          <div>
            <SubmitButton
              disabled={isSubmitting}
              className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 dark:bg-blue-500 dark:hover:bg-blue-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 dark:focus:ring-blue-400 focus:ring-offset-white dark:focus:ring-offset-gray-900 transition-colors"
              loadingText={t('auth.register.signing_up')}
            >
              <div className="flex items-center">
                <Check className="h-4 w-4 mr-2" />
                {t('auth.register.sign_up')}
              </div>
            </SubmitButton>
          </div>

          <div className="text-center">
            <span className="text-sm text-gray-600 dark:text-gray-300">
              {t('auth.register.have_account')}{' '}
              <Link
                to="/login"
                className="font-medium text-blue-600 hover:text-blue-500 dark:text-blue-400 dark:hover:text-blue-300"
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
