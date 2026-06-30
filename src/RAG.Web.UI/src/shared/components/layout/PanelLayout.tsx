import React, { type ReactNode } from 'react'
import { Outlet } from 'react-router-dom'

interface PanelLayoutProps {
  sidebar: ReactNode
  header?: ReactNode
}

export function PanelLayout({ sidebar, header }: PanelLayoutProps) {
  return (
    <div className="flex flex-col md:flex-row h-[calc(100vh-8rem)] w-[95%] mx-auto bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-gray-200 dark:border-slate-800 overflow-hidden transition-colors">
      {sidebar}

      <div className="flex-1 overflow-y-auto bg-gray-50 dark:bg-slate-950 transition-colors">
        {header && (
          <div className="sticky top-0 z-20 flex items-center border-b border-gray-200 bg-gray-50/95 px-4 py-2 backdrop-blur dark:border-slate-800 dark:bg-slate-950/95 md:h-[61px] md:px-6 md:py-0">
            {header}
          </div>
        )}

        <div className="p-4 md:p-6">
          <Outlet />
        </div>
      </div>
    </div>
  )
}
