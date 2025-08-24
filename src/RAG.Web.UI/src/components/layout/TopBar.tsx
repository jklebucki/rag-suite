import React from 'react'
import { Menu, User } from 'lucide-react'

interface TopBarProps {
  onToggleSidebar: () => void
}

export function TopBar({ onToggleSidebar }: TopBarProps) {
  return (
    <header className="h-16 bg-white border-b border-gray-200 flex items-center justify-between px-6">
      <button
        onClick={onToggleSidebar}
        className="p-2 rounded-lg hover:bg-gray-100 lg:hidden"
        aria-label="Open menu"
        title="Open menu"
      >
        <Menu className="h-5 w-5" />
      </button>

      <button
        className="p-2 rounded-lg hover:bg-gray-100 ml-auto"
        title="User menu"
        aria-label="User menu"
      >
        <User className="h-5 w-5" />
      </button>
    </header>
  )
}
