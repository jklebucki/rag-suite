import React from 'react'
import { Toast, ToastType } from './Toast'

export interface ToastNotification {
  id: string
  type: ToastType
  title: string
  message?: string
  autoClose?: boolean
  duration?: number
}

interface ToastContainerProps {
  toasts: ToastNotification[]
  onRemoveToast: (id: string) => void
}

export function ToastContainer({ toasts, onRemoveToast }: ToastContainerProps) {
  return (
    <div className="fixed top-4 right-4 z-50 space-y-4 pointer-events-none">
      {toasts.map((toast) => (
        <Toast
          key={toast.id}
          id={toast.id}
          type={toast.type}
          title={toast.title}
          message={toast.message}
          isVisible={true}
          onClose={() => onRemoveToast(toast.id)}
          autoClose={toast.autoClose}
          duration={toast.duration}
        />
      ))}
    </div>
  )
}
