import React from 'react'

interface TextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  error?: boolean
}

export const Textarea = React.forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ className = '', error, ...props }, ref) => {
    return (
      <textarea
        ref={ref}
        className={`flex min-h-[80px] w-full rounded-md border bg-white px-3 py-2 text-sm ring-offset-white placeholder:text-gray-400 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-gray-800 dark:text-gray-100 dark:placeholder:text-gray-500 dark:ring-offset-gray-900 ${
          error 
            ? 'border-red-500 focus-visible:ring-red-500 dark:border-red-600' 
            : 'border-gray-300 focus-visible:ring-blue-600 dark:border-gray-600 dark:focus-visible:ring-blue-500'
        } ${className}`}
        {...props}
      />
    )
  }
)

Textarea.displayName = 'Textarea'
