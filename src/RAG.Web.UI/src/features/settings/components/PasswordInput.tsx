// All code comments must be written in English, regardless of the conversation language.

import React from 'react'
import { Eye, EyeOff, Lock, CheckCircle, XCircle } from 'lucide-react'
import type { PasswordStrength } from '@/features/settings/types/settings'
import { useI18n } from '@/shared/contexts/I18nContext'

interface PasswordInputProps {
  value: string
  onChange: (value: string) => void
  showPassword: boolean
  onToggleShow: () => void
  placeholder: string
  label: string
  strength?: PasswordStrength
  matchStatus?: 'match' | 'mismatch' | 'none'
}

export function PasswordInput({
  value,
  onChange,
  showPassword,
  onToggleShow,
  placeholder,
  label,
  strength,
  matchStatus = 'none'
}: PasswordInputProps) {
  const getStateClasses = () => {
    if (matchStatus === 'mismatch') {
      return 'border-red-400 focus-visible:ring-red-500 focus-visible:border-red-500 dark:border-red-600'
    }
    if (matchStatus === 'match') {
      return 'border-green-400 focus-visible:ring-green-500 focus-visible:border-green-500 dark:border-green-500'
    }
    return ''
  }

  const inputId = `password-input-${label.toLowerCase().replace(/\s+/g, '-')}`
  const { t } = useI18n()

  const strengthLabels: Record<PasswordStrength['label'], string> = {
    Weak: t('settings.user.password.strength.weak'),
    Fair: t('settings.user.password.strength.fair'),
    Good: t('settings.user.password.strength.good'),
    Strong: t('settings.user.password.strength.strong')
  }
  
  return (
    <div className="space-y-2">
      <label htmlFor={inputId} className="block text-sm font-medium text-gray-700 dark:text-gray-200">
        {label}
      </label>
      <div className="relative">
        <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
          <Lock className="h-5 w-5 text-gray-400 dark:text-gray-500" />
        </div>
        <input
          id={inputId}
          type={showPassword ? 'text' : 'password'}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          className={`form-input pl-10 pr-10 py-3 transition-colors ${getStateClasses()}`}
          placeholder={placeholder}
        />
        <button
          type="button"
          onClick={onToggleShow}
          className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-gray-600 dark:text-gray-500 dark:hover:text-gray-300 transition-colors"
        >
          {showPassword ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
        </button>
      </div>

      {/* Password Strength Indicator */}
      {strength && value && (
        <div className="space-y-2">
          <div className="flex items-center justify-between text-sm">
            <span className="text-gray-600 dark:text-gray-300">{t('settings.user.password.strength.label')}</span>
            <span className={`font-medium ${
              strength.score <= 2 ? 'text-red-600' :
              strength.score <= 3 ? 'text-yellow-600' :
              strength.score <= 4 ? 'text-blue-600' : 'text-green-500'
            }`}>
              {strengthLabels[strength.label]}
            </span>
          </div>
          <div className="w-full bg-gray-200 dark:bg-slate-800 rounded-full h-2 relative overflow-hidden">
            <div
              className={`absolute left-0 top-0 h-2 rounded-full transition-all duration-300 ${
                strength.score <= 2 ? 'bg-red-500 w-2/5' :
                strength.score <= 3 ? 'bg-yellow-500 w-3/5' :
                strength.score <= 4 ? 'bg-blue-500 w-4/5' : 'bg-green-500 w-full'
              }`}
            ></div>
          </div>

          {/* Password Requirements */}
          <div className="grid grid-cols-2 gap-2 text-xs">
            <PasswordRequirement met={strength.checks.length} label={t('settings.user.password.requirements.length')} />
            <PasswordRequirement met={strength.checks.uppercase} label={t('settings.user.password.requirements.uppercase')} />
            <PasswordRequirement met={strength.checks.lowercase} label={t('settings.user.password.requirements.lowercase')} />
            <PasswordRequirement met={strength.checks.number} label={t('settings.user.password.requirements.number')} />
            <PasswordRequirement met={strength.checks.special} label={t('settings.user.password.requirements.special')} />
          </div>
        </div>
      )}

      {/* Password Match Indicator */}
      {matchStatus !== 'none' && value && (
        <div className="flex items-center gap-2 text-sm">
          {matchStatus === 'match' ? (
            <>
              <CheckCircle className="h-4 w-4 text-green-600" />
              <span className="text-green-600">{t('settings.user.password.match')}</span>
            </>
          ) : (
            <>
              <XCircle className="h-4 w-4 text-red-600" />
              <span className="text-red-600">{t('settings.user.password.mismatch')}</span>
            </>
          )}
        </div>
      )}
    </div>
  )
}

function PasswordRequirement({ met, label }: { met: boolean; label: string }) {
  return (
    <div className={`flex items-center gap-2 ${met ? 'text-green-600' : 'text-gray-400 dark:text-gray-500'}`}>
      {met ? <CheckCircle className="h-3 w-3" /> : <XCircle className="h-3 w-3" />}
      <span>{label}</span>
    </div>
  )
}
