import React, { createContext, useContext, ReactNode } from 'react'
import { useToast } from '@/hooks/useToast'
import { ToastContainer } from '@/components/ui/ToastContainer'
import type { ToastType } from '@/components/ui/Toast'

interface ToastContextType {
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
    removeToast, 
    clearToasts, 
    showSuccess, 
    showError, 
    showWarning, 
    showInfo 
  } = useToast()

  const contextValue: ToastContextType = {
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
