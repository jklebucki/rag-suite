import React, { createContext, useContext, ReactNode } from 'react'
import { useToast } from '@/shared/hooks/useToast'
import { ToastContainer } from '@/shared/components/ui/ToastContainer'
import type { ToastType } from '@/shared/components/ui/Toast'

interface ToastContextType {
  addToast: (options: { type: ToastType; title: string; message?: string }) => string
  showSuccess: (title: string, message?: string, options?: { autoClose?: boolean; duration?: number }) => string
  showError: (title: string, message?: string, options?: { autoClose?: boolean; duration?: number }) => string
  showWarning: (title: string, message?: string, options?: { autoClose?: boolean; duration?: number }) => string
  showInfo: (title: string, message?: string, options?: { autoClose?: boolean; duration?: number }) => string
  removeToast: (id: string) => void
  clearToasts: () => void
}

const ToastContext = createContext<ToastContextType | undefined>(undefined)

interface ToastProviderProps {
  children: ReactNode
}

export function ToastProvider({ children }: ToastProviderProps) {
  const { 
    toasts, 
    addToast: originalAddToast,
    removeToast, 
    clearToasts, 
    showSuccess, 
    showError, 
    showWarning, 
    showInfo 
  } = useToast()

  const addToast = (options: { type: ToastType; title: string; message?: string }) => {
    return originalAddToast(options.type, options.title, options.message)
  }

  const contextValue: ToastContextType = {
    addToast,
    showSuccess,
    showError,
    showWarning,
    showInfo,
    removeToast,
    clearToasts,
  }

  return (
    <ToastContext.Provider value={contextValue}>
      {children}
      <ToastContainer toasts={toasts} onRemoveToast={removeToast} />
    </ToastContext.Provider>
  )
}

export function useToastContext() {
  const context = useContext(ToastContext)
  if (context === undefined) {
    throw new Error('useToastContext must be used within a ToastProvider')
  }
  return context
}

export { useToastContext as useToast }
