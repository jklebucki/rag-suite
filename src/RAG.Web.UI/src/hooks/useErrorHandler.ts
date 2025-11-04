/**
 * useErrorHandler hook
 * Centralized error handling with toast notifications and logging
 */

import { useCallback } from 'react'
import { useToast } from '@/contexts/ToastContext'
import { logger } from '@/utils/logger'

export interface ErrorHandlerOptions {
  title?: string
  message?: string
  showToast?: boolean
  logError?: boolean
  onError?: (error: unknown) => void
}

export interface ErrorHandler {
  handleError: (error: unknown, options?: ErrorHandlerOptions) => void
  handleAsyncError: <T>(
    promise: Promise<T>,
    options?: ErrorHandlerOptions
  ) => Promise<T | undefined>
}

/**
 * Hook for centralized error handling
 * Provides consistent error logging and user notifications
 * 
 * @example
 * const { handleError, handleAsyncError } = useErrorHandler()
 * 
 * // Handle error directly
 * try {
 *   await someOperation()
 * } catch (error) {
 *   handleError(error, {
 *     title: 'Operation Failed',
 *     message: 'Failed to complete operation'
 *   })
 * }
 * 
 * // Handle async operation
 * const result = await handleAsyncError(
 *   apiCall(),
 *   { title: 'API Error' }
 * )
 */
export function useErrorHandler(): ErrorHandler {
  const { addToast } = useToast()

  const handleError = useCallback(
    (error: unknown, options: ErrorHandlerOptions = {}): void => {
      const {
        title = 'Error',
        message,
        showToast = true,
        logError = true,
        onError,
      } = options

      // Log error to logger utility
      if (logError) {
        logger.error('Error handled by useErrorHandler:', error)
      }

      // Determine error message
      let errorMessage = message
      if (!errorMessage) {
        if (error instanceof Error) {
          errorMessage = error.message
        } else if (typeof error === 'string') {
          errorMessage = error
        } else if (error && typeof error === 'object' && 'message' in error) {
          errorMessage = String((error as { message: unknown }).message)
        } else {
          errorMessage = 'An unknown error occurred'
        }
      }

      // Show toast notification
      if (showToast) {
        addToast({
          type: 'error',
          title,
          message: errorMessage,
        })
      }

      // Call custom error handler if provided
      if (onError) {
        onError(error)
      }
    },
    [addToast]
  )

  const handleAsyncError = useCallback(
    async <T,>(
      promise: Promise<T>,
      options: ErrorHandlerOptions = {}
    ): Promise<T | undefined> => {
      try {
        return await promise
      } catch (error) {
        handleError(error, options)
        return undefined
      }
    },
    [handleError]
  )

  return {
    handleError,
    handleAsyncError,
  }
}

/**
 * Utility function to extract error message from various error types
 */
export function getErrorMessage(error: unknown): string {
  if (error instanceof Error) {
    return error.message
  }
  if (typeof error === 'string') {
    return error
  }
  if (error && typeof error === 'object') {
    if ('message' in error) {
      return String(error.message)
    }
    if ('error' in error) {
      return String(error.error)
    }
  }
  return 'An unknown error occurred'
}

/**
 * Type guard to check if error is an HTTP error with status code
 */
export function isHttpError(error: unknown): error is { status: number; message: string } {
  return (
    error !== null &&
    typeof error === 'object' &&
    'status' in error &&
    typeof (error as { status: unknown }).status === 'number'
  )
}

/**
 * Utility to check if error is a validation error
 */
export function isValidationError(error: unknown): error is { errors: Record<string, string[]> } {
  return (
    error !== null &&
    typeof error === 'object' &&
    'errors' in error &&
    typeof (error as { errors: unknown }).errors === 'object'
  )
}
