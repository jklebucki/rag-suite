/**
 * SubmitButton - Reusable submit button component using useFormStatus
 * 
 * This component automatically handles disabled state based on form submission status
 * without needing to pass isSubmitting prop through component tree.
 * 
 * Usage:
 * ```tsx
 * <form action={formAction}>
 *   <input name="email" />
 *   <SubmitButton>Sign In</SubmitButton>
 * </form>
 * ```
 */

import { useFormStatus } from 'react-dom'
import type { ButtonHTMLAttributes, ReactNode } from 'react'

interface SubmitButtonProps extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'type'> {
  children: ReactNode
  loadingText?: string
  showSpinner?: boolean
}

/**
 * SubmitButton component that uses useFormStatus to automatically
 * handle disabled state during form submission
 */
export function SubmitButton({ 
  children, 
  loadingText,
  showSpinner = true,
  className = '',
  disabled,
  ...props 
}: SubmitButtonProps) {
  const { pending } = useFormStatus()
  const isDisabled = disabled || pending

  return (
    <button
      type="submit"
      disabled={isDisabled}
      className={`${className} ${isDisabled ? 'opacity-50 cursor-not-allowed' : ''}`}
      {...props}
    >
      {pending && showSpinner && (
        <span className="inline-block animate-spin rounded-full h-4 w-4 border-b-2 border-current mr-2" />
      )}
      {pending && loadingText ? loadingText : children}
    </button>
  )
}

