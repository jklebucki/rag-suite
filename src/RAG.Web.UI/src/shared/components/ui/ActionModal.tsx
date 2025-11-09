import React from 'react'
import { AlertCircle, AlertTriangle, CheckCircle, Info } from 'lucide-react'

import { Modal } from './Modal'
import { Button } from './Button'
import { cn } from '@/utils/cn'

export type ActionModalVariant = 'info' | 'success' | 'warning' | 'danger' | 'error'

interface ActionModalProps {
  isOpen: boolean
  onClose: () => void
  title: React.ReactNode
  message?: React.ReactNode
  children?: React.ReactNode
  confirmText?: string
  cancelText?: string
  onConfirm?: () => void
  onCancel?: () => void
  isLoading?: boolean
  loadingText?: string
  variant?: ActionModalVariant
  size?: 'sm' | 'md' | 'lg' | 'xl'
  hideCancel?: boolean
  closeOnConfirm?: boolean
}

const variantConfig: Record<
  ActionModalVariant,
  {
    icon: React.ComponentType<{ className?: string }>
    iconBg: string
    iconColor: string
    buttonVariant: React.ComponentProps<typeof Button>['variant']
    confirmClassName?: string
  }
> = {
  info: {
    icon: Info,
    iconBg: 'bg-blue-100 dark:bg-blue-900/40',
    iconColor: 'text-blue-600 dark:text-blue-300',
    buttonVariant: 'primary',
    confirmClassName: ''
  },
  success: {
    icon: CheckCircle,
    iconBg: 'bg-green-100 dark:bg-green-900/40',
    iconColor: 'text-green-600 dark:text-green-300',
    buttonVariant: 'primary',
    confirmClassName: 'bg-green-600 hover:bg-green-700 focus-visible:ring-green-600 dark:bg-green-500 dark:hover:bg-green-600'
  },
  warning: {
    icon: AlertTriangle,
    iconBg: 'bg-amber-100 dark:bg-amber-900/40',
    iconColor: 'text-amber-600 dark:text-amber-300',
    buttonVariant: 'primary',
    confirmClassName: 'bg-amber-500 hover:bg-amber-600 focus-visible:ring-amber-500 dark:bg-amber-500 dark:hover:bg-amber-600'
  },
  danger: {
    icon: AlertTriangle,
    iconBg: 'bg-red-100 dark:bg-red-900/40',
    iconColor: 'text-red-600 dark:text-red-300',
    buttonVariant: 'destructive',
    confirmClassName: ''
  },
  error: {
    icon: AlertCircle,
    iconBg: 'bg-red-100 dark:bg-red-900/40',
    iconColor: 'text-red-600 dark:text-red-300',
    buttonVariant: 'destructive',
    confirmClassName: ''
  }
}

export function ActionModal({
  isOpen,
  onClose,
  title,
  message,
  children,
  confirmText = 'Confirm',
  cancelText,
  onConfirm,
  onCancel,
  isLoading = false,
  loadingText = 'Processing...',
  variant = 'info',
  size = 'md',
  hideCancel = false,
  closeOnConfirm = true
}: ActionModalProps) {
  const config = variantConfig[variant]
  const Icon = config.icon

  const handleConfirm = () => {
    onConfirm?.()
    if (closeOnConfirm) {
      onClose()
    }
  }

  const handleCancel = () => {
    onCancel?.()
    onClose()
  }

  const showCancelButton = !hideCancel && !!cancelText

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      size={size}
      title={
        <div className="flex items-center gap-3">
          <div className={`flex h-10 w-10 items-center justify-center rounded-full ${config.iconBg}`}>
            <Icon className={`h-5 w-5 ${config.iconColor}`} />
          </div>
          <span className="text-lg font-semibold text-gray-900 dark:text-gray-100">{title}</span>
        </div>
      }
    >
      <div className="p-6 space-y-6">
        {children ??
          (message
            ? typeof message === 'string'
              ? <p className="text-gray-600 dark:text-gray-300">{message}</p>
              : message
            : null)}

        <div className={cn('flex justify-end gap-3', showCancelButton ? '' : '')}>
          {showCancelButton && (
            <button
              onClick={handleCancel}
              disabled={isLoading}
              className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md transition-colors hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50 dark:bg-gray-700 dark:text-gray-200 dark:border-gray-600 dark:hover:bg-gray-600"
            >
              {cancelText}
            </button>
          )}
          <Button
            onClick={handleConfirm}
            disabled={isLoading}
            variant={config.buttonVariant}
            className={cn(
              'min-w-[96px]',
              config.confirmClassName,
              isLoading && 'cursor-wait'
            )}
          >
            {isLoading ? (
              <>
                <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
                {loadingText}
              </>
            ) : (
              confirmText
            )}
          </Button>
        </div>
      </div>
    </Modal>
  )
}

