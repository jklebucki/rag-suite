import { Routes, Route } from 'react-router-dom'
import { Layout } from '@/components/Layout'
import { Dashboard } from '@/components/dashboard/Dashboard'
import { ChatInterface } from '@/components/chat/ChatInterface'
import { SearchInterface } from '@/components/search/SearchInterface'
import { Settings } from '@/components/settings/Settings'
import { About } from '@/components/About'
import { LoginForm, RegisterForm, ResetPasswordForm, ResetPasswordConfirmForm, ProtectedRoute, AuthRoute, AdminProtectedRoute } from '@/components/auth'
import { ToastProvider } from '@/contexts/ToastContext'
import { I18nProvider } from '@/contexts/I18nContext'
import { AuthProvider } from '@/contexts/AuthContext'
import { ConfigurationProvider } from '@/contexts/ConfigurationContext'

function App() {
  return (
    <I18nProvider>
      <ToastProvider>
        <ConfigurationProvider>
          <AuthProvider>
          <Layout>
            <Routes>
              <Route
                path="/"
                element={
                  <ProtectedRoute>
                    <Dashboard />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/chat"
                element={
                  <ProtectedRoute>
                    <ChatInterface />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/search"
                element={
                  <ProtectedRoute>
                    <SearchInterface />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/about"
                element={
                  <ProtectedRoute>
                    <About />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/settings"
                element={
                  <AdminProtectedRoute>
                    <Settings />
                  </AdminProtectedRoute>
                }
              />
              <Route
                path="/login"
                element={
                  <AuthRoute>
                    <LoginForm />
                  </AuthRoute>
                }
              />
              <Route
                path="/register"
                element={
                  <AuthRoute>
                    <RegisterForm />
                  </AuthRoute>
                }
              />
              <Route
                path="/forgot-password"
                element={
                  <AuthRoute>
                    <ResetPasswordForm />
                  </AuthRoute>
                }
              />
              <Route
                path="/reset-password"
                element={
                  <AuthRoute>
                    <ResetPasswordConfirmForm />
                  </AuthRoute>
                }
              />
            </Routes>
          </Layout>
        </AuthProvider>
        </ConfigurationProvider>
      </ToastProvider>
    </I18nProvider>
  )
}

export default App
