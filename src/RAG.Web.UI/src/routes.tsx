import { Routes, Route, useLocation } from 'react-router-dom'
import { lazy, useEffect, useState } from 'react'
import { Layout } from '@/shared/components/layout'
import { LandingPage } from '@/features/landing/components/LandingPage'
import { ProtectedRoute, AuthRoute, AdminProtectedRoute } from '@/features/auth/components'
import { RoleProtectedRoute } from '@/features/auth/components/RoleProtectedRoute'

// Lazy load components for code splitting
const Dashboard = lazy(() =>
  import('@/features/dashboard/components/Dashboard').then(module => ({ default: module.Dashboard }))
)
const ChatInterface = lazy(() =>
  import('@/features/chat/components/ChatInterface').then(module => ({ default: module.ChatInterface }))
)
const SearchInterface = lazy(() =>
  import('@/features/search/components/SearchInterface').then(module => ({ default: module.SearchInterface }))
)
const Settings = lazy(() =>
  import('@/features/settings/components/Settings').then(module => ({ default: module.Settings }))
)
const About = lazy(() => import('@/features/about/components/About').then(module => ({ default: module.About })))
const UserGuide = lazy(() =>
  import('@/features/user-guide/components/UserGuide').then(module => ({ default: module.UserGuide }))
)
const AddressBook = lazy(() =>
  import('@/features/address-book/components/AddressBook').then(module => ({ default: module.AddressBook }))
)
const LoginForm = lazy(() =>
  import('@/features/auth/components/LoginForm').then(module => ({ default: module.LoginForm }))
)
const RegisterForm = lazy(() =>
  import('@/features/auth/components/RegisterForm').then(module => ({ default: module.RegisterForm }))
)
const ResetPasswordForm = lazy(() =>
  import('@/features/auth/components/ResetPasswordForm').then(module => ({ default: module.ResetPasswordForm }))
)
const ResetPasswordConfirmForm = lazy(() =>
  import('@/features/auth/components/ResetPasswordConfirmForm').then(module => ({ default: module.ResetPasswordConfirmForm }))
)
const CyberPanelLayout = lazy(() =>
  import('@/features/cyberpanel/components/CyberPanelLayout').then(module => ({ default: module.CyberPanelLayout }))
)
const Quizzes = lazy(() =>
  import('@/features/cyberpanel/components/Quizzes').then(module => ({ default: module.Quizzes }))
)
const QuizManager = lazy(() =>
  import('@/features/cyberpanel/components/QuizManager').then(module => ({ default: module.QuizManager }))
)
const QuizBuilder = lazy(() =>
  import('@/features/cyberpanel/components/QuizBuilder').then(module => ({ default: module.QuizBuilder }))
)
const QuizResults = lazy(() =>
  import('@/features/cyberpanel/components/QuizResults').then(module => ({ default: module.QuizResults }))
)
const QuizDetail = lazy(() =>
  import('@/features/cyberpanel/components/QuizDetail').then(module => ({ default: module.QuizDetail }))
)
const AttemptDetail = lazy(() =>
  import('@/features/cyberpanel/components/AttemptDetail').then(module => ({ default: module.AttemptDetail }))
)

export function AppRoutes() {
  const location = useLocation()
  const [chatKey, setChatKey] = useState(Date.now())

  // Force ChatInterface to remount when navigating to /chat
  useEffect(() => {
    if (location.pathname === '/chat') {
      setChatKey(Date.now())
    }
  }, [location.pathname])

  return (
    <Routes>
      <Route
        path="/"
        element={
          <Layout>
            <LandingPage />
          </Layout>
        }
      />
      <Route
        path="/dashboard"
        element={
          <Layout>
            <ProtectedRoute>
              <Dashboard />
            </ProtectedRoute>
          </Layout>
        }
      />
      <Route
        path="/chat"
        element={
          <Layout>
            <ProtectedRoute>
              <ChatInterface key={chatKey} />
            </ProtectedRoute>
          </Layout>
        }
      />
      <Route
        path="/search"
        element={
          <Layout>
            <ProtectedRoute>
              <SearchInterface />
            </ProtectedRoute>
          </Layout>
        }
      />
      <Route
        path="/address-book"
        element={
          <Layout>
            <AddressBook />
          </Layout>
        }
      />
      <Route
        path="/guide"
        element={
          <Layout>
            <UserGuide />
          </Layout>
        }
      />
      <Route
        path="/about"
        element={
          <Layout>
            <About />
          </Layout>
        }
      />
      <Route
        path="/settings"
        element={
          <Layout>
            <AdminProtectedRoute>
              <Settings />
            </AdminProtectedRoute>
          </Layout>
        }
      />
      <Route
        path="/cyberpanel/*"
        element={
          <Layout>
            <ProtectedRoute>
              <CyberPanelLayout />
            </ProtectedRoute>
          </Layout>
        }
      >
        <Route index element={<Quizzes />} />
        <Route path="quizzes" element={<Quizzes />} />
        <Route path="quizzes/:id" element={<QuizDetail />} />
        <Route path="manager" element={<RoleProtectedRoute allowedRoles={["Admin","PowerUser"]}><QuizManager /></RoleProtectedRoute>} />
        <Route path="builder" element={<AdminProtectedRoute><QuizBuilder /></AdminProtectedRoute>} />
        <Route path="results" element={<RoleProtectedRoute allowedRoles={["Admin","PowerUser","User"]}><QuizResults /></RoleProtectedRoute>} />
        <Route path="attempts/:id" element={<RoleProtectedRoute allowedRoles={["Admin","PowerUser","User"]}><AttemptDetail /></RoleProtectedRoute>} />
      </Route>
      <Route
        path="/login"
        element={
          <Layout>
            <AuthRoute>
              <LoginForm />
            </AuthRoute>
          </Layout>
        }
      />
      <Route
        path="/register"
        element={
          <Layout>
            <AuthRoute>
              <RegisterForm />
            </AuthRoute>
          </Layout>
        }
      />
      <Route
        path="/forgot-password"
        element={
          <Layout>
            <AuthRoute>
              <ResetPasswordForm />
            </AuthRoute>
          </Layout>
        }
      />
      <Route
        path="/reset-password"
        element={
          <Layout>
            <AuthRoute>
              <ResetPasswordConfirmForm />
            </AuthRoute>
          </Layout>
        }
      />
    </Routes>
  )
}

