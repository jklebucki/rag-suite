import React, { ReactElement } from 'react'
import { render, RenderOptions } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import { I18nProvider } from '@/contexts/I18nContext'
import { ToastProvider } from '@/contexts/ToastContext'
import { AuthProvider } from '@/contexts/AuthContext'
import { ConfigurationProvider } from '@/contexts/ConfigurationContext'
import type { User } from '@/types/auth'

// Create a test query client
const createTestQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        cacheTime: 0,
      },
      mutations: {
        retry: false,
      },
    },
  })

interface AllTheProvidersProps {
  children: React.ReactNode
  queryClient?: QueryClient
  initialAuthState?: {
    user?: User | null
    token?: string | null
    isAuthenticated?: boolean
  }
}

function AllTheProviders({
  children,
  queryClient = createTestQueryClient(),
  initialAuthState,
}: AllTheProvidersProps) {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <I18nProvider>
          <ToastProvider>
            <ConfigurationProvider>
              <AuthProvider initialAuthState={initialAuthState}>
                {children}
              </AuthProvider>
            </ConfigurationProvider>
          </ToastProvider>
        </I18nProvider>
      </BrowserRouter>
    </QueryClientProvider>
  )
}

interface CustomRenderOptions extends Omit<RenderOptions, 'wrapper'> {
  queryClient?: QueryClient
  initialAuthState?: {
    user?: User | null
    token?: string | null
    isAuthenticated?: boolean
  }
}

const customRender = (
  ui: ReactElement,
  options: CustomRenderOptions = {}
) => {
  const { queryClient, initialAuthState, ...renderOptions } = options

  return render(ui, {
    wrapper: (props) => (
      <AllTheProviders
        queryClient={queryClient}
        initialAuthState={initialAuthState}
        {...props}
      />
    ),
    ...renderOptions,
  })
}

// Re-export everything
export * from '@testing-library/react'
export { customRender as render, createTestQueryClient }

// Helper to create mock user
export const createMockUser = (overrides?: Partial<User>): User => ({
  id: '1',
  username: 'testuser',
  email: 'test@example.com',
  roles: ['User'],
  ...overrides,
})

