import type { PropsWithChildren } from 'react'
import { ErrorBoundary } from '@/shared/components/common/ErrorBoundary'
import { ToastProvider } from '@/shared/contexts/ToastContext'
import { I18nProvider } from '@/shared/contexts/I18nContext'
import { AuthProvider } from '@/shared/contexts/AuthContext'
import { ConfigurationProvider } from '@/shared/contexts/ConfigurationContext'

export function AppProviders({ children }: PropsWithChildren) {
  return (
    <ErrorBoundary>
      <I18nProvider>
        <ToastProvider>
          <ConfigurationProvider>
            <AuthProvider>{children}</AuthProvider>
          </ConfigurationProvider>
        </ToastProvider>
      </I18nProvider>
    </ErrorBoundary>
  )
}

