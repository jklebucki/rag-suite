import { useState, useCallback } from 'react'
import type { ToastNotification } from '@/components/ui/ToastContainer'
import type { ToastType } from '@/components/ui/Toast'

export function useToast() {
  const [toasts, setToasts] = useState<ToastNotification[]>([])

  const addToast = useCallback((
    type: ToastType,
    title: string,
    message?: string,
    options?: { autoClose?: boolean; duration?: number }
  ) => {
    const id = Math.random().toString(36).substring(2, 9)
    const newToast: ToastNotification = {
      id,
      type,
      title,
      message,
      autoClose: options?.autoClose ?? true,
      duration: options?.duration ?? 5000,
    }

    setToasts(prev => [...prev, newToast])
    return id
  }, [])

  const removeToast = useCallback((id: string) => {
    setToasts(prev => prev.filter(toast => toast.id !== id))
  }, [])

  const clearToasts = useCallback(() => {
    setToasts([])
  }, [])

  // Convenience methods
  const showSuccess = useCallback((title: string, message?: string, options?: { autoClose?: boolean; duration?: number }) => {
    return addToast('success', title, message, options)
  }, [addToast])

  const showError = useCallback((title: string, message?: string, options?: { autoClose?: boolean; duration?: number }) => {
    return addToast('error', title, message, options)
  }, [addToast])

  const showWarning = useCallback((title: string, message?: string, options?: { autoClose?: boolean; duration?: number }) => {
    return addToast('warning', title, message, options)
  }, [addToast])

  const showInfo = useCallback((title: string, message?: string, options?: { autoClose?: boolean; duration?: number }) => {
    return addToast('info', title, message, options)
  }, [addToast])

  return {
    toasts,
    addToast,
    removeToast,
    clearToasts,
    showSuccess,
    showError,
    showWarning,
    showInfo,
  }
}
