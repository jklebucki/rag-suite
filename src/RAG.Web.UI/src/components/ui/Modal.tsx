import React, { useEffect } from 'react'
import { X } from 'lucide-react'

interface ModalProps {
  isOpen: boolean
  onClose: () => void
  title: React.ReactNode
  children: React.ReactNode
  size?: 'sm' | 'md' | 'lg' | 'xl'
  fullscreen?: boolean
}

export function Modal({ isOpen, onClose, title, children, size = 'lg', fullscreen = false }: ModalProps) {
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

  const sizeClasses = {
    sm: 'max-w-md',
    md: 'max-w-2xl',
    lg: 'max-w-4xl',
    xl: 'max-w-6xl'
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 sm:p-0">
      {/* Backdrop */}
      <button 
        className="fixed inset-0 bg-black bg-opacity-50 transition-opacity"
        onClick={onClose}
        aria-label="Close modal"
        tabIndex={-1}
      />
      
      {/* Modal */}
      <div className={`relative bg-white w-full overflow-hidden ${
        fullscreen 
          ? 'h-full rounded-none' 
          : `rounded-lg shadow-xl ${sizeClasses[size]} max-h-[90vh] sm:max-h-[85vh]`
      }`}>
        {/* Header - ukryty w fullscreen */}
        {!fullscreen && (
          <div className="flex items-center justify-between p-4 sm:p-6 border-b border-gray-200">
            <h2 className="text-lg sm:text-xl font-semibold text-gray-900 truncate pr-2">{title}</h2>
            <button
              onClick={onClose}
              className="p-2 hover:bg-gray-100 rounded-lg transition-colors flex-shrink-0"
              aria-label="Close modal"
              title="Close modal"
            >
              <X className="h-5 w-5 text-gray-500" />
            </button>
          </div>
        )}
        
        {/* Content */}
        <div className={`overflow-y-auto ${
          fullscreen 
            ? 'h-full' 
            : 'max-h-[calc(90vh-120px)]'
        }`}>
          {children}
        </div>
      </div>
    </div>
  )
}
