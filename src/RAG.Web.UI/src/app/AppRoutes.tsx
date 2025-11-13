import { Suspense, useEffect, useMemo, useState, type ReactNode } from 'react'
import { createBrowserRouter, Outlet, RouterProvider, useLocation } from 'react-router-dom'
import { Layout } from '@/shared/components/layout'
import { LandingPage } from '@/features/landing/components/LandingPage'
import { ProtectedRoute, AuthRoute, AdminProtectedRoute } from '@/features/auth/components'
import { RoleProtectedRoute } from '@/features/auth/components/RoleProtectedRoute'
import { useAsyncComponent } from '@/shared/hooks/useAsyncComponent'

// Component promises for lazy loading using React 19's use() hook
const DashboardPromise = import('@/features/dashboard/components/Dashboard').then(module => ({ default: module.Dashboard }))
const ChatInterfacePromise = import('@/features/chat/components/ChatInterface').then(module => ({ default: module.ChatInterface }))
const SearchInterfacePromise = import('@/features/search/components/SearchInterface').then(module => ({ default: module.SearchInterface }))
const SettingsPromise = import('@/features/settings/components/Settings').then(module => ({ default: module.Settings }))
const AboutPromise = import('@/features/about/components/About').then(module => ({ default: module.About }))
const UserGuidePromise = import('@/features/user-guide/components/UserGuide').then(module => ({ default: module.UserGuide }))
const AddressBookPromise = import('@/features/address-book/components/AddressBook').then(module => ({ default: module.AddressBook }))
const LoginFormPromise = import('@/features/auth/components/LoginForm').then(module => ({ default: module.LoginForm }))
const RegisterFormPromise = import('@/features/auth/components/RegisterForm').then(module => ({ default: module.RegisterForm }))
const ResetPasswordFormPromise = import('@/features/auth/components/ResetPasswordForm').then(module => ({ default: module.ResetPasswordForm }))
const ResetPasswordConfirmFormPromise = import('@/features/auth/components/ResetPasswordConfirmForm').then(module => ({
  default: module.ResetPasswordConfirmForm,
}))
const CyberPanelLayoutPromise = import('@/features/cyberpanel/components/CyberPanelLayout').then(module => ({
  default: module.CyberPanelLayout,
}))
const QuizzesPromise = import('@/features/cyberpanel/components/Quizzes').then(module => ({ default: module.Quizzes }))
const QuizManagerPromise = import('@/features/cyberpanel/components/QuizManager').then(module => ({ default: module.QuizManager }))
const QuizBuilderPromise = import('@/features/cyberpanel/components/QuizBuilder').then(module => ({ default: module.QuizBuilder }))
const QuizResultsPromise = import('@/features/cyberpanel/components/QuizResults').then(module => ({ default: module.QuizResults }))
const QuizDetailPromise = import('@/features/cyberpanel/components/QuizDetail').then(module => ({ default: module.QuizDetail }))
const AttemptDetailPromise = import('@/features/cyberpanel/components/AttemptDetail').then(module => ({ default: module.AttemptDetail }))
const ForumPagePromise = import('@/features/forum/components/ForumPage').then(module => ({ default: module.ForumPage }))
const ThreadDetailPagePromise = import('@/features/forum/components/ThreadDetailPage').then(module => ({ default: module.ThreadDetailPage }))

// Component loaders using use() hook
function DashboardLoader() {
  const Dashboard = useAsyncComponent(DashboardPromise)
  return <Dashboard />
}

function ChatInterfaceLoader() {
  const ChatInterface = useAsyncComponent(ChatInterfacePromise)
  return <ChatInterface />
}

function SearchInterfaceLoader() {
  const SearchInterface = useAsyncComponent(SearchInterfacePromise)
  return <SearchInterface />
}

function SettingsLoader() {
  const Settings = useAsyncComponent(SettingsPromise)
  return <Settings />
}

function AboutLoader() {
  const About = useAsyncComponent(AboutPromise)
  return <About />
}

function UserGuideLoader() {
  const UserGuide = useAsyncComponent(UserGuidePromise)
  return <UserGuide />
}

function AddressBookLoader() {
  const AddressBook = useAsyncComponent(AddressBookPromise)
  return <AddressBook />
}

function LoginFormLoader() {
  const LoginForm = useAsyncComponent(LoginFormPromise)
  return <LoginForm />
}

function RegisterFormLoader() {
  const RegisterForm = useAsyncComponent(RegisterFormPromise)
  return <RegisterForm />
}

function ResetPasswordFormLoader() {
  const ResetPasswordForm = useAsyncComponent(ResetPasswordFormPromise)
  return <ResetPasswordForm />
}

function ResetPasswordConfirmFormLoader() {
  const ResetPasswordConfirmForm = useAsyncComponent(ResetPasswordConfirmFormPromise)
  return <ResetPasswordConfirmForm />
}

function CyberPanelLayoutLoader() {
  const CyberPanelLayout = useAsyncComponent(CyberPanelLayoutPromise)
  return <CyberPanelLayout />
}

function QuizzesLoader() {
  const Quizzes = useAsyncComponent(QuizzesPromise)
  return <Quizzes />
}

function QuizManagerLoader() {
  const QuizManager = useAsyncComponent(QuizManagerPromise)
  return <QuizManager />
}

function QuizBuilderLoader() {
  const QuizBuilder = useAsyncComponent(QuizBuilderPromise)
  return <QuizBuilder />
}

function QuizResultsLoader() {
  const QuizResults = useAsyncComponent(QuizResultsPromise)
  return <QuizResults />
}

function QuizDetailLoader() {
  const QuizDetail = useAsyncComponent(QuizDetailPromise)
  return <QuizDetail />
}

function AttemptDetailLoader() {
  const AttemptDetail = useAsyncComponent(AttemptDetailPromise)
  return <AttemptDetail />
}

function ForumPageLoader() {
  const ForumPage = useAsyncComponent(ForumPagePromise)
  return <ForumPage />
}

function ThreadDetailPageLoader() {
  const ThreadDetailPage = useAsyncComponent(ThreadDetailPagePromise)
  return <ThreadDetailPage />
}

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

  return <ChatInterfaceLoader key={renderKey} />
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
                  <DashboardLoader />
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
                  <SearchInterfaceLoader />
                </ProtectedRoute>
              </RouteSuspense>
            ),
          },
          {
            path: 'forum',
            element: (
              <RouteSuspense>
                <ProtectedRoute>
                  <ForumPageLoader />
                </ProtectedRoute>
              </RouteSuspense>
            ),
          },
          {
            path: 'forum/:threadId',
            element: (
              <RouteSuspense>
                <ProtectedRoute>
                  <ThreadDetailPageLoader />
                </ProtectedRoute>
              </RouteSuspense>
            ),
          },
          {
            path: 'address-book',
            element: (
              <RouteSuspense>
                <AddressBookLoader />
              </RouteSuspense>
            ),
          },
          {
            path: 'guide',
            element: (
              <RouteSuspense>
                <UserGuideLoader />
              </RouteSuspense>
            ),
          },
          {
            path: 'about',
            element: (
              <RouteSuspense>
                <AboutLoader />
              </RouteSuspense>
            ),
          },
          {
            path: 'settings',
            element: (
              <RouteSuspense>
                <AdminProtectedRoute>
                  <SettingsLoader />
                </AdminProtectedRoute>
              </RouteSuspense>
            ),
          },
          {
            path: 'cyberpanel',
            element: (
              <RouteSuspense>
                <ProtectedRoute>
                  <CyberPanelLayoutLoader />
                </ProtectedRoute>
              </RouteSuspense>
            ),
            children: [
              {
                index: true,
                element: (
                  <RouteSuspense>
                    <QuizzesLoader />
                  </RouteSuspense>
                ),
              },
              {
                path: 'quizzes',
                element: (
                  <RouteSuspense>
                    <QuizzesLoader />
                  </RouteSuspense>
                ),
              },
              {
                path: 'quizzes/:id',
                element: (
                  <RouteSuspense>
                    <QuizDetailLoader />
                  </RouteSuspense>
                ),
              },
              {
                path: 'manager',
                element: (
                  <RouteSuspense>
                    <RoleProtectedRoute allowedRoles={['Admin', 'PowerUser']}>
                      <QuizManagerLoader />
                    </RoleProtectedRoute>
                  </RouteSuspense>
                ),
              },
              {
                path: 'builder',
                element: (
                  <RouteSuspense>
                    <AdminProtectedRoute>
                      <QuizBuilderLoader />
                    </AdminProtectedRoute>
                  </RouteSuspense>
                ),
              },
              {
                path: 'results',
                element: (
                  <RouteSuspense>
                    <RoleProtectedRoute allowedRoles={['Admin', 'PowerUser', 'User']}>
                      <QuizResultsLoader />
                    </RoleProtectedRoute>
                  </RouteSuspense>
                ),
              },
              {
                path: 'attempts/:id',
                element: (
                  <RouteSuspense>
                    <RoleProtectedRoute allowedRoles={['Admin', 'PowerUser', 'User']}>
                      <AttemptDetailLoader />
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
                  <LoginFormLoader />
                </AuthRoute>
              </RouteSuspense>
            ),
          },
          {
            path: 'register',
            element: (
              <RouteSuspense>
                <AuthRoute>
                  <RegisterFormLoader />
                </AuthRoute>
              </RouteSuspense>
            ),
          },
          {
            path: 'forgot-password',
            element: (
              <RouteSuspense>
                <AuthRoute>
                  <ResetPasswordFormLoader />
                </AuthRoute>
              </RouteSuspense>
            ),
          },
          {
            path: 'reset-password',
            element: (
              <RouteSuspense>
                <AuthRoute>
                  <ResetPasswordConfirmFormLoader />
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

