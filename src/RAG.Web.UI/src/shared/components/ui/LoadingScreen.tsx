import { Loader2 } from 'lucide-react'
import { cn } from '@/utils/cn'

interface LoadingScreenProps {
  label?: string
  className?: string
}

export function LoadingScreen({ label = 'Loading...', className }: LoadingScreenProps) {
  return (
    <div className={cn('flex min-h-screen flex-col items-center justify-center gap-4 text-gray-600', className)}>
      <Loader2 className="h-10 w-10 animate-spin text-primary-600" aria-hidden="true" />
      <span className="text-sm font-medium">{label}</span>
    </div>
  )
}

