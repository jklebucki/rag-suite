import React from 'react'
import { CheckCircle, AlertCircle, XCircle, Info, X } from 'lucide-react'

export type ToastType = 'success' | 'error' | 'warning' | 'info'

interface ToastProps {
  id?: string
  type: ToastType
  title: string
  message?: string
  isVisible: boolean
  onClose: () => void
  autoClose?: boolean
  duration?: number
}

export function Toast({ 
  id: _id,
  type, 
  title, 
  message, 
  isVisible, 
  onClose, 
  autoClose = true, 
  duration = 5000 
}: ToastProps) {
  React.useEffect(() => {
    if (autoClose && isVisible) {
      const timer = setTimeout(() => {
        onClose()
      }, duration)
      return () => clearTimeout(timer)
    }
  }, [autoClose, duration, isVisible, onClose])

  if (!isVisible) return null

  const typeConfig = {
    success: {
      icon: CheckCircle,
      bgColor: 'bg-green-50 dark:bg-green-900/30',
      borderColor: 'border-green-200 dark:border-green-700',
      iconColor: 'text-green-600 dark:text-green-400',
      titleColor: 'text-green-900 dark:text-green-100',
      messageColor: 'text-green-700 dark:text-green-200'
    },
    error: {
      icon: XCircle,
      bgColor: 'bg-red-50 dark:bg-red-900/30',
      borderColor: 'border-red-200 dark:border-red-700',
      iconColor: 'text-red-600 dark:text-red-400',
      titleColor: 'text-red-900 dark:text-red-100',
      messageColor: 'text-red-700 dark:text-red-200'
    },
    warning: {
      icon: AlertCircle,
      bgColor: 'bg-yellow-50 dark:bg-yellow-900/30',
      borderColor: 'border-yellow-200 dark:border-yellow-700',
      iconColor: 'text-yellow-600 dark:text-yellow-400',
      titleColor: 'text-yellow-900 dark:text-yellow-100',
      messageColor: 'text-yellow-700 dark:text-yellow-200'
    },
    info: {
      icon: Info,
      bgColor: 'bg-blue-50 dark:bg-blue-900/30',
      borderColor: 'border-blue-200 dark:border-blue-700',
      iconColor: 'text-blue-600 dark:text-blue-400',
      titleColor: 'text-blue-900 dark:text-blue-100',
      messageColor: 'text-blue-700 dark:text-blue-200'
    }
  }

  const config = typeConfig[type]
  const IconComponent = config.icon

  return (
    <div className={`w-full max-w-md ${config.bgColor} ${config.borderColor} border rounded-lg shadow-xl pointer-events-auto overflow-hidden transition-all duration-300 transform ${
      isVisible ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-2'
    }`}>
      <div className="p-6">
        <div className="flex items-start">
          <div className="flex-shrink-0">
            <IconComponent className={`h-7 w-7 ${config.iconColor}`} />
          </div>
          <div className="ml-4 w-0 flex-1">
            <p className={`text-base font-semibold ${config.titleColor}`}>
              {title}
            </p>
            {message && (
              <p className={`mt-2 text-sm leading-relaxed ${config.messageColor}`}>
                {message}
              </p>
            )}
          </div>
          <div className="ml-4 flex-shrink-0 flex">
            <button
              className={`rounded-md inline-flex p-1.5 ${config.messageColor} hover:${config.titleColor} focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-offset-gray-50 focus:ring-primary-600 transition-colors`}
              onClick={onClose}
            >
              <span className="sr-only">Close</span>
              <X className="h-5 w-5" />
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
