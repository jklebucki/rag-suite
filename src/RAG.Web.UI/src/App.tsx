import { Suspense } from 'react'
import { ErrorBoundary } from '@/shared/components/common/ErrorBoundary'
import { AppRoutes } from '@/routes'
import { ToastProvider } from '@/shared/contexts/ToastContext'
import { I18nProvider } from '@/shared/contexts/I18nContext'
import { AuthProvider } from '@/shared/contexts/AuthContext'
import { ConfigurationProvider } from '@/shared/contexts/ConfigurationContext'

function App() {
  return (
    <ErrorBoundary>
      <I18nProvider>
        <ToastProvider>
          <ConfigurationProvider>
            <AuthProvider>
              <Suspense fallback={<div>Loading...</div>}>
                <AppRoutes />
              </Suspense>
            </AuthProvider>
          </ConfigurationProvider>
        </ToastProvider>
      </I18nProvider>
    </ErrorBoundary>
  )
}

export default App
