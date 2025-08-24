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
    <div className="fixed bottom-4 right-4 left-4 sm:left-auto z-50 w-full max-w-sm lg:max-w-md xl:max-w-lg pointer-events-none">
      <div className="space-y-4 flex flex-col-reverse">
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
    </div>
  )
}
