import React, { type ReactNode } from 'react'
import { Outlet } from 'react-router-dom'

interface PanelLayoutProps {
  sidebar: ReactNode
}

export function PanelLayout({ sidebar }: PanelLayoutProps) {
  return (
    <div className="flex flex-col md:flex-row h-[calc(100vh-8rem)] w-[95%] mx-auto bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-gray-200 dark:border-slate-800 overflow-hidden transition-colors">
      {sidebar}

      <div className="flex-1 overflow-y-auto p-4 md:p-6 bg-gray-50 dark:bg-slate-950 transition-colors">
        <Outlet />
      </div>
    </div>
  )
}
