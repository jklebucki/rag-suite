import { CheckCircle, XCircle } from 'lucide-react'

interface SuccessBannerProps {
  message: string
  onDismiss: () => void
}

export function SuccessBanner({ message, onDismiss }: SuccessBannerProps) {
  return (
    <div className="flex items-start gap-3 px-4 py-3 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-xl text-sm text-green-800 dark:text-green-300">
      <CheckCircle className="h-5 w-5 flex-shrink-0 mt-0.5" />
      <span className="flex-1">{message}</span>
      <button
        onClick={onDismiss}
        className="text-green-600 dark:text-green-400 hover:text-green-800 dark:hover:text-green-200 transition-colors flex-shrink-0"
        aria-label="Dismiss"
      >
        <XCircle className="h-4 w-4" />
      </button>
    </div>
  )
}
