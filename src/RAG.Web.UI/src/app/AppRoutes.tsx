import { lazy, Suspense, useEffect, useMemo, useState, type ReactNode } from 'react'
import { createBrowserRouter, Outlet, RouterProvider, useLocation } from 'react-router-dom'
import { Layout } from '@/shared/components/layout'
import { LandingPage } from '@/features/landing/components/LandingPage'
import { ProtectedRoute, AuthRoute, AdminProtectedRoute } from '@/features/auth/components'
import { RoleProtectedRoute } from '@/features/auth/components/RoleProtectedRoute'

const Dashboard = lazy(() =>
  import('@/features/dashboard/components/Dashboard').then(module => ({ default: module.Dashboard })),
)
const ChatInterface = lazy(() =>
  import('@/features/chat/components/ChatInterface').then(module => ({ default: module.ChatInterface })),
)
const SearchInterface = lazy(() =>
  import('@/features/search/components/SearchInterface').then(module => ({ default: module.SearchInterface })),
)
const Settings = lazy(() =>
  import('@/features/settings/components/Settings').then(module => ({ default: module.Settings })),
)
const About = lazy(() =>
  import('@/features/about/components/About').then(module => ({ default: module.About })),
)
const UserGuide = lazy(() =>
  import('@/features/user-guide/components/UserGuide').then(module => ({ default: module.UserGuide })),
)
const AddressBook = lazy(() =>
  import('@/features/address-book/components/AddressBook').then(module => ({ default: module.AddressBook })),
)
const LoginForm = lazy(() =>
  import('@/features/auth/components/LoginForm').then(module => ({ default: module.LoginForm })),
)
const RegisterForm = lazy(() =>
  import('@/features/auth/components/RegisterForm').then(module => ({ default: module.RegisterForm })),
)
const ResetPasswordForm = lazy(() =>
  import('@/features/auth/components/ResetPasswordForm').then(module => ({ default: module.ResetPasswordForm })),
)
const ResetPasswordConfirmForm = lazy(() =>
  import('@/features/auth/components/ResetPasswordConfirmForm').then(module => ({
    default: module.ResetPasswordConfirmForm,
  })),
)
const CyberPanelLayout = lazy(() =>
  import('@/features/cyberpanel/components/CyberPanelLayout').then(module => ({
    default: module.CyberPanelLayout,
  })),
)
const Quizzes = lazy(() =>
  import('@/features/cyberpanel/components/Quizzes').then(module => ({ default: module.Quizzes })),
)
const QuizManager = lazy(() =>
  import('@/features/cyberpanel/components/QuizManager').then(module => ({ default: module.QuizManager })),
)
const QuizBuilder = lazy(() =>
  import('@/features/cyberpanel/components/QuizBuilder').then(module => ({ default: module.QuizBuilder })),
)
const QuizResults = lazy(() =>
  import('@/features/cyberpanel/components/QuizResults').then(module => ({ default: module.QuizResults })),
)
const QuizDetail = lazy(() =>
  import('@/features/cyberpanel/components/QuizDetail').then(module => ({ default: module.QuizDetail })),
)
const AttemptDetail = lazy(() =>
  import('@/features/cyberpanel/components/AttemptDetail').then(module => ({ default: module.AttemptDetail })),
)

function RouteSuspense({ children }: { children: ReactNode }) {
  return (
    <Suspense
      fallback={
        <div className="flex h-full min-h-[240px] items-center justify-center text-gray-500">Loading...</div>
      }
    >
      {children}
    </Suspense>
  )
}

function ShellLayout() {
  return (
    <Layout>
      <Outlet />
    </Layout>
  )
}

function ChatRoute() {
  const location = useLocation()
  const [renderKey, setRenderKey] = useState(() => Date.now())

  useEffect(() => {
    if (location.pathname === '/chat') {
      setRenderKey(Date.now())
    }
  }, [location.key, location.pathname])

  return <ChatInterface key={renderKey} />
}

export function createAppRouter() {
  return createBrowserRouter(
    [
      {
        element: <ShellLayout />,
        children: [
          {
            index: true,
            element: <LandingPage />,
          },
          {
            path: 'dashboard',
            element: (
              <RouteSuspense>
                <ProtectedRoute>
                  <Dashboard />
                </ProtectedRoute>
              </RouteSuspense>
            ),
          },
          {
            path: 'chat',
            element: (
              <RouteSuspense>
                <ProtectedRoute>
                  <ChatRoute />
                </ProtectedRoute>
              </RouteSuspense>
            ),
          },
          {
            path: 'search',
            element: (
              <RouteSuspense>
                <ProtectedRoute>
                  <SearchInterface />
                </ProtectedRoute>
              </RouteSuspense>
            ),
          },
          {
            path: 'address-book',
            element: (
              <RouteSuspense>
                <AddressBook />
              </RouteSuspense>
            ),
          },
          {
            path: 'guide',
            element: (
              <RouteSuspense>
                <UserGuide />
              </RouteSuspense>
            ),
          },
          {
            path: 'about',
            element: (
              <RouteSuspense>
                <About />
              </RouteSuspense>
            ),
          },
          {
            path: 'settings',
            element: (
              <RouteSuspense>
                <AdminProtectedRoute>
                  <Settings />
                </AdminProtectedRoute>
              </RouteSuspense>
            ),
          },
          {
            path: 'cyberpanel',
            element: (
              <RouteSuspense>
                <ProtectedRoute>
                  <CyberPanelLayout />
                </ProtectedRoute>
              </RouteSuspense>
            ),
            children: [
              {
                index: true,
                element: (
                  <RouteSuspense>
                    <Quizzes />
                  </RouteSuspense>
                ),
              },
              {
                path: 'quizzes',
                element: (
                  <RouteSuspense>
                    <Quizzes />
                  </RouteSuspense>
                ),
              },
              {
                path: 'quizzes/:id',
                element: (
                  <RouteSuspense>
                    <QuizDetail />
                  </RouteSuspense>
                ),
              },
              {
                path: 'manager',
                element: (
                  <RouteSuspense>
                    <RoleProtectedRoute allowedRoles={['Admin', 'PowerUser']}>
                      <QuizManager />
                    </RoleProtectedRoute>
                  </RouteSuspense>
                ),
              },
              {
                path: 'builder',
                element: (
                  <RouteSuspense>
                    <AdminProtectedRoute>
                      <QuizBuilder />
                    </AdminProtectedRoute>
                  </RouteSuspense>
                ),
              },
              {
                path: 'results',
                element: (
                  <RouteSuspense>
                    <RoleProtectedRoute allowedRoles={['Admin', 'PowerUser', 'User']}>
                      <QuizResults />
                    </RoleProtectedRoute>
                  </RouteSuspense>
                ),
              },
              {
                path: 'attempts/:id',
                element: (
                  <RouteSuspense>
                    <RoleProtectedRoute allowedRoles={['Admin', 'PowerUser', 'User']}>
                      <AttemptDetail />
                    </RoleProtectedRoute>
                  </RouteSuspense>
                ),
              },
            ],
          },
          {
            path: 'login',
            element: (
              <RouteSuspense>
                <AuthRoute>
                  <LoginForm />
                </AuthRoute>
              </RouteSuspense>
            ),
          },
          {
            path: 'register',
            element: (
              <RouteSuspense>
                <AuthRoute>
                  <RegisterForm />
                </AuthRoute>
              </RouteSuspense>
            ),
          },
          {
            path: 'forgot-password',
            element: (
              <RouteSuspense>
                <AuthRoute>
                  <ResetPasswordForm />
                </AuthRoute>
              </RouteSuspense>
            ),
          },
          {
            path: 'reset-password',
            element: (
              <RouteSuspense>
                <AuthRoute>
                  <ResetPasswordConfirmForm />
                </AuthRoute>
              </RouteSuspense>
            ),
          },
        ],
      },
    ],
    {
      future: {
        v7_startTransition: true,
      },
    },
  )
}

export function AppRouterProvider() {
  const router = useMemo(() => createAppRouter(), [])
  return <RouterProvider router={router} />
}

