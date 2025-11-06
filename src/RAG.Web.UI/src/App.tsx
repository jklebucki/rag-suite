import { Suspense } from 'react'
import { ErrorBoundary } from '@/components/common/ErrorBoundary'
import { AppRoutes } from '@/routes'
import { ToastProvider } from '@/contexts/ToastContext'
import { I18nProvider } from '@/contexts/I18nContext'
import { AuthProvider } from '@/contexts/AuthContext'
import { ConfigurationProvider } from '@/contexts/ConfigurationContext'

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
