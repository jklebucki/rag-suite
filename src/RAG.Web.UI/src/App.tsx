import { Routes, Route } from 'react-router-dom'
import { lazy, Suspense } from 'react'
import { Layout } from '@/components/Layout'
const Dashboard = lazy(() => import('@/components/dashboard/Dashboard'))
const ChatInterface = lazy(() => import('@/components/chat/ChatInterface'))
const SearchInterface = lazy(() => import('@/components/search/SearchInterface'))
const Settings = lazy(() => import('@/components/settings/Settings'))
const About = lazy(() => import('@/components/About'))
const LoginForm = lazy(() => import('@/components/auth/LoginForm'))
const RegisterForm = lazy(() => import('@/components/auth/RegisterForm'))
const ResetPasswordForm = lazy(() => import('@/components/auth/ResetPasswordForm'))
const ResetPasswordConfirmForm = lazy(() => import('@/components/auth/ResetPasswordConfirmForm'))
const CyberPanelLayout = lazy(() => import('@/components/cyberpanel/CyberPanelLayout'))
const Quizzes = lazy(() => import('@/components/cyberpanel/Quizzes'))
const QuizBuilder = lazy(() => import('@/components/cyberpanel/QuizBuilder'))
const QuizResults = lazy(() => import('@/components/cyberpanel/QuizResults'))
const QuizDetail = lazy(() => import('@/components/cyberpanel/QuizDetail'))
const AttemptDetail = lazy(() => import('@/components/cyberpanel/AttemptDetail'))
import { ProtectedRoute, AuthRoute, AdminProtectedRoute } from '@/components/auth'
import { RoleProtectedRoute } from '@/components/auth/RoleProtectedRoute'
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
            <Suspense fallback={<div>Loading...</div>}>
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
                  <About />
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

                {/* Cyber Panel parent with nested routes */}
                <Route
                  path="/cyberpanel/*"
                  element={
                    <ProtectedRoute>
                      <CyberPanelLayout />
                    </ProtectedRoute>
                  }
                >
                  <Route index element={<Quizzes />} />
                  <Route path="quizzes" element={<Quizzes />} />
                  <Route path="quizzes/:id" element={<QuizDetail />} />
                  <Route path="builder" element={<AdminProtectedRoute><QuizBuilder /></AdminProtectedRoute>} />
                  <Route path="results" element={<RoleProtectedRoute allowedRoles={["Admin","PowerUser","User"]}><QuizResults /></RoleProtectedRoute>} />
                  <Route path="attempts/:id" element={<RoleProtectedRoute allowedRoles={["Admin","PowerUser","User"]}><AttemptDetail /></RoleProtectedRoute>} />
                </Route>
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
            </Suspense>
          </Layout>
        </AuthProvider>
        </ConfigurationProvider>
      </ToastProvider>
    </I18nProvider>
  )
}

export default App
