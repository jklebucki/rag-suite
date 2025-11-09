import React from 'react'
import { useOnlineStatus } from '@/shared/hooks/useOnlineStatus'

interface ConnectionStatusProps {
  className?: string
}

export const ConnectionStatus: React.FC<ConnectionStatusProps> = ({ className = '' }) => {
  const { isOnline } = useOnlineStatus()

  if (isOnline) return null

  return (
    <div className={`fixed top-0 left-0 right-0 bg-red-500 dark:bg-red-600 text-white text-center py-2 text-sm z-50 ${className}`}>
      🔌 Brak połączenia z internetem. Sprawdź połączenie sieciowe.
    </div>
  )
}
