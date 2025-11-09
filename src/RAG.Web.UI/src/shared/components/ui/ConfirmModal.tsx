import React from 'react'
import { AlertTriangle, X } from 'lucide-react'

interface ConfirmModalProps {
  isOpen: boolean
  onClose: () => void
  onConfirm: () => void
  title: string
  message: string
  confirmText?: string
  cancelText?: string
  variant?: 'danger' | 'warning' | 'info'
  isLoading?: boolean
}

export function ConfirmModal({
  isOpen,
  onClose,
  onConfirm,
  title,
  message,
  confirmText = 'Confirm',
  cancelText = 'Cancel',
  variant = 'danger',
  isLoading = false
}: ConfirmModalProps) {
  if (!isOpen) return null

  const variantStyles = {
    danger: {
      icon: 'text-red-600 dark:text-red-400',
      iconBg: 'bg-red-100 dark:bg-red-900/30',
      confirmBtn: 'bg-red-600 hover:bg-red-700 focus:ring-red-500 dark:bg-red-500 dark:hover:bg-red-600'
    },
    warning: {
      icon: 'text-yellow-600 dark:text-yellow-400',
      iconBg: 'bg-yellow-100 dark:bg-yellow-900/30',
      confirmBtn: 'bg-yellow-600 hover:bg-yellow-700 focus:ring-yellow-500 dark:bg-yellow-500 dark:hover:bg-yellow-600'
    },
    info: {
      icon: 'text-blue-600 dark:text-blue-400',
      iconBg: 'bg-blue-100 dark:bg-blue-900/30',
      confirmBtn: 'bg-blue-600 hover:bg-blue-700 focus:ring-blue-500 dark:bg-blue-500 dark:hover:bg-blue-600'
    }
  }

  const styles = variantStyles[variant]

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <button 
        className="fixed inset-0 bg-black bg-opacity-50 dark:bg-opacity-70 transition-opacity"
        onClick={onClose}
        aria-label="Close modal"
        tabIndex={-1}
      />
      
      {/* Modal */}
      <div className="relative bg-white dark:bg-gray-800 rounded-lg shadow-xl w-full max-w-md mx-4">
        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b border-gray-200 dark:border-gray-700">
          <div className="flex items-center">
            <div className={`flex-shrink-0 w-10 h-10 ${styles.iconBg} rounded-full flex items-center justify-center mr-3`}>
              <AlertTriangle className={`h-6 w-6 ${styles.icon}`} />
            </div>
            <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100">{title}</h3>
          </div>
          <button
            onClick={onClose}
            className="p-2 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition-colors"
            aria-label="Close modal"
            title="Close modal"
          >
            <X className="h-5 w-5 text-gray-500 dark:text-gray-400" />
          </button>
        </div>
        
        {/* Content */}
        <div className="p-6">
          <p className="text-gray-600 dark:text-gray-300">{message}</p>
        </div>

        {/* Footer */}
        <div className="flex justify-end gap-3 p-6 border-t border-gray-200 dark:border-gray-700">
          <button
            onClick={onClose}
            disabled={isLoading}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50 dark:bg-gray-700 dark:text-gray-200 dark:border-gray-600 dark:hover:bg-gray-600"
          >
            {cancelText}
          </button>
          <button
            onClick={onConfirm}
            disabled={isLoading}
            className={`px-4 py-2 text-sm font-medium text-white rounded-md focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-50 ${styles.confirmBtn}`}
          >
            {isLoading ? 'Processing...' : confirmText}
          </button>
        </div>
      </div>
    </div>
  )
}
