// @ts-nocheck

import React, { ReactElement } from 'react'
import { render, RenderOptions } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import { I18nProvider } from '@/shared/contexts/I18nContext'
import { ToastProvider } from '@/shared/contexts/ToastContext'
import { AuthProvider } from '@/shared/contexts/AuthContext'
import { ConfigurationProvider } from '@/shared/contexts/ConfigurationContext'
import type { User } from '@/features/auth/types/auth'
import configurationService from '@/features/settings/services/configuration.service'

const globalWithMock = globalThis as typeof globalThis & { __ragFetchMocked__?: boolean }

if (typeof globalWithMock.fetch === 'function' && !globalWithMock.__ragFetchMocked__) {
  const originalFetch = globalWithMock.fetch.bind(globalWithMock)
  const defaultRegistrationConfiguration = configurationService.getDefaultConfiguration()

  globalWithMock.fetch = async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = typeof input === 'string' ? input : input instanceof URL ? input.href : 'url' in input ? input.url : ''

    const isRegistrationConfigRequest = (() => {
      if (!url) return false
      try {
        const parsed = url.startsWith('http') ? new URL(url) : new URL(url, 'http://localhost')
        return parsed.pathname === '/api/configuration/registration'
      } catch {
        return false
      }
    })()

    if (isRegistrationConfigRequest) {
      return new Response(JSON.stringify(defaultRegistrationConfiguration), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      })
    }

    return originalFetch(input, init)
  }

  globalWithMock.__ragFetchMocked__ = true
}

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
}

function AllTheProviders({
  children,
  queryClient = createTestQueryClient(),
}: AllTheProvidersProps) {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <I18nProvider>
          <ToastProvider>
            <ConfigurationProvider>
              <AuthProvider>
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
}

const customRender = (
  ui: ReactElement,
  options: CustomRenderOptions = {}
) => {
  const { queryClient, ...renderOptions } = options

  return render(ui, {
    wrapper: (props: { children: React.ReactNode }) => (
      <AllTheProviders
        queryClient={queryClient}
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
  userName: 'testuser',
  email: 'test@example.com',
  firstName: 'Test',
  lastName: 'User',
  fullName: 'Test User',
  roles: ['User'],
  isActive: true,
  createdAt: new Date().toISOString(),
  ...overrides,
})

