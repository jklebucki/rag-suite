import React, { useEffect } from 'react'
import { X } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import { createPortal } from 'react-dom'

interface ModalProps {
  isOpen: boolean
  onClose: () => void
  title: React.ReactNode
  children: React.ReactNode
  size?: 'sm' | 'md' | 'lg' | 'xl' | 'screen'
  fullscreen?: boolean
  /** Render a slim, understated header instead of the default prominent one. */
  subtleHeader?: boolean
}

export function Modal({ isOpen, onClose, title, children, size = 'lg', fullscreen = false, subtleHeader = false }: ModalProps) {
  const { t } = useI18n()
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen) {
        onClose()
      }
    }

    if (isOpen) {
      document.addEventListener('keydown', handleEscape)
      document.body.style.overflow = 'hidden'
    }

    return () => {
      document.removeEventListener('keydown', handleEscape)
      document.body.style.overflow = 'unset'
    }
  }, [isOpen, onClose])

  if (!isOpen) return null

  if (typeof document === 'undefined') {
    return null
  }

  const sizeClasses = {
    sm: 'max-w-md',
    md: 'max-w-2xl',
    lg: 'max-w-4xl',
    xl: 'max-w-6xl',
    screen: '!w-[80vw] max-w-none h-[95vh]'
  }

  const closeLabel = t('common.close')
  // Tighter padding for the near-fullscreen size so the 95vh dialog stays fully on-screen and centered.
  const modalWrapperClasses = `relative z-10 flex min-h-full w-full items-center justify-center ${size === 'screen' ? 'p-3' : 'p-4 sm:p-6'}`

  const portalTarget = document.getElementById('modal-root') ?? document.body

  const modalContent = (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      {/* Backdrop */}
      <button
        className="fixed inset-0 z-0 bg-black bg-opacity-50 dark:bg-opacity-70 transition-opacity"
        onClick={onClose}
        aria-label={closeLabel}
        tabIndex={-1}
      />

      {/* Modal wrapper */}
      <div className={modalWrapperClasses}>
        <div
          role="dialog"
          aria-modal="true"
          aria-label={typeof title === 'string' ? title : undefined}
          className={`relative bg-white dark:bg-gray-800 w-full overflow-hidden ${
            fullscreen
              ? 'h-full rounded-none'
              : size === 'screen'
                ? 'flex flex-col rounded-xl shadow-xl !w-[80vw] max-w-none h-[95vh]'
                : `rounded-lg shadow-xl ${sizeClasses[size]} max-h-[90vh] sm:max-h-[85vh]`
          }`}
        >
          {/* Header - hidden in fullscreen */}
          {!fullscreen && (
            <div
              className={`flex flex-shrink-0 items-center justify-between border-b border-gray-200 dark:border-gray-700 ${
                subtleHeader ? 'gap-2 px-4 py-2.5' : 'p-4 sm:p-6'
              }`}
            >
              <h2
                className={`truncate pr-2 ${
                  subtleHeader
                    ? 'text-sm font-medium text-gray-500 dark:text-gray-400'
                    : 'text-lg sm:text-xl font-semibold text-gray-900 dark:text-gray-100'
                }`}
              >
                {title}
              </h2>
              <button
                onClick={onClose}
                className={`flex-shrink-0 rounded-lg transition-colors hover:bg-gray-100 dark:hover:bg-gray-700 ${
                  subtleHeader ? 'p-1' : 'p-2'
                }`}
                aria-label={closeLabel}
                title={closeLabel}
              >
                <X className={`text-gray-400 dark:text-gray-500 ${subtleHeader ? 'h-4 w-4' : 'h-5 w-5'}`} />
              </button>
            </div>
          )}

          {/* Content */}
          <div
            className={`overflow-y-auto ${
              fullscreen
                ? 'h-full'
                : size === 'screen'
                  ? 'flex-1 min-h-0'
                  : 'max-h-[calc(90vh-120px)]'
            }`}
          >
            {children}
          </div>
        </div>
      </div>
    </div>
  )

  return createPortal(modalContent, portalTarget)
}
