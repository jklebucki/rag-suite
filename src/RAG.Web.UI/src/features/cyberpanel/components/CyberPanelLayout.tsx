import React from 'react'
import { useLocation } from 'react-router-dom'
import { CyberPanelSidebar } from '@/features/cyberpanel/components/CyberPanelSidebar'
import { PanelLayout } from '@/shared/components/layout'

export function CyberPanelLayout() {
  const location = useLocation()

  return (
    <PanelLayout
      sidebar={
        <CyberPanelSidebar
          isActiveRoute={(href) => location.pathname.startsWith(href)}
        />
      }
    />
  )
}
