import React from 'react'
import { X, AlertTriangle } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'

interface DeleteConfirmationModalProps {
  isOpen: boolean
  onClose: () => void
  onConfirm: () => void
  title: string
  message: string
  itemName: string
  details?: {
    label: string
    value: string | number
  }[]
  isLoading?: boolean
  confirmText?: string
  cancelText?: string
  deletingText?: string
  warningText?: string
}

export const DeleteConfirmationModal: React.FC<DeleteConfirmationModalProps> = ({
  isOpen,
  onClose,
  onConfirm,
  title,
  message,
  itemName,
  details,
  isLoading = false,
  confirmText,
  cancelText,
  deletingText,
  warningText
}) => {
  const { t } = useI18n()
  const confirmLabel = confirmText ?? t('common.delete')
  const cancelLabel = cancelText ?? t('common.cancel')
  const deletingLabel = deletingText ?? t('common.deleting')
  const warningLabel = warningText ?? t('common.irreversibleAction')
  const closeLabel = t('common.close')

  if (!isOpen) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl max-w-md w-full mx-4">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b dark:border-gray-700">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-red-100 dark:bg-red-900/30 rounded-full">
              <AlertTriangle className="w-5 h-5 text-red-600 dark:text-red-400" />
            </div>
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white">{title}</h3>
          </div>
          <button
            onClick={onClose}
            disabled={isLoading}
            title={closeLabel}
            aria-label={closeLabel}
            className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 disabled:opacity-50"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Body */}
        <div className="p-6 space-y-4">
          <p className="text-gray-700 dark:text-gray-300">{message}</p>

          <div className="bg-gray-50 dark:bg-gray-700/50 rounded-lg p-4">
            <p className="font-medium text-gray-900 dark:text-white mb-2">{itemName}</p>
            {details && details.length > 0 && (
              <dl className="space-y-2 text-sm">
                {details.map((detail, index) => (
                  <div key={index} className="flex justify-between">
                    <dt className="text-gray-600 dark:text-gray-400">{detail.label}:</dt>
                    <dd className="font-medium text-gray-900 dark:text-white">{detail.value}</dd>
                  </div>
                ))}
              </dl>
            )}
          </div>

          <p className="text-sm text-red-600 dark:text-red-400 font-medium">{warningLabel}</p>
        </div>

        {/* Footer */}
        <div className="flex gap-3 justify-end p-4 border-t dark:border-gray-700">
          <button
            onClick={onClose}
            disabled={isLoading}
            className="px-4 py-2 text-gray-700 dark:text-gray-300 bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 rounded-lg transition-colors disabled:opacity-50"
          >
            {cancelLabel}
          </button>
          <button
            onClick={onConfirm}
            disabled={isLoading}
            className="px-4 py-2 text-white bg-red-600 hover:bg-red-700 rounded-lg transition-colors disabled:opacity-50 flex items-center gap-2"
          >
            {isLoading ? (
              <>
                <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
                {deletingLabel}
              </>
            ) : (
              confirmLabel
            )}
          </button>
        </div>
      </div>
    </div>
  )
}
