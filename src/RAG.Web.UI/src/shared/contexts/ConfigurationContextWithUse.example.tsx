/**
 * ConfigurationContextWithUse - Example using React 19's use() hook
 * 
 * This is an example implementation showing how ConfigurationContext
 * could be refactored to use React 19's use() hook instead of
 * useState + useEffect pattern.
 * 
 * NOTE: This is an example file. The actual ConfigurationContext
 * uses the traditional pattern which is more suitable for contexts
 * that need to handle errors and provide refresh functionality.
 */

import React, { createContext, useContext, use, useMemo, Suspense, ReactNode } from 'react'
import { ConfigurationState, ConfigurationContextType, RegistrationConfiguration } from '@/features/settings/types/configuration'
import configurationService from '@/features/settings/services/configuration.service'
import { createScopedLogger } from '@/utils/logger'

const log = createScopedLogger('ConfigurationContext')

// Context
const ConfigurationContext = createContext<ConfigurationContextType | undefined>(undefined)

// Provider component using use() hook
interface ConfigurationProviderProps {
  children: ReactNode
}

/**
 * Internal component that uses use() hook to load configuration
 * This component must be wrapped in Suspense boundary
 */
function ConfigurationLoader({ children }: { children: ReactNode }) {
  // Use React 19's use() hook to directly unwrap the Promise
  // This eliminates the need for useState + useEffect
  const configuration = use(
    configurationService.getRegistrationConfiguration().catch((error) => {
      log.error('Failed to fetch configuration:', error)
      // Return default configuration as fallback
      return configurationService.getDefaultConfiguration()
    })
  )

  const contextValue: ConfigurationContextType = useMemo(
    () => ({
      configuration,
      loading: false,
      error: null,
      lastFetched: new Date(),
      fetchConfiguration: async () => {
        // This would need to be handled differently with use()
        // as use() doesn't support manual refetching easily
        throw new Error('Manual refresh not supported in use() pattern')
      },
      refreshConfiguration: async () => {
        throw new Error('Manual refresh not supported in use() pattern')
      },
      clearError: () => {
        // No-op in this pattern
      },
    }),
    [configuration]
  )

  return (
    <ConfigurationContext.Provider value={contextValue}>
      {children}
    </ConfigurationContext.Provider>
  )
}

export function ConfigurationProviderWithUse({ children }: ConfigurationProviderProps) {
  return (
    <Suspense
      fallback={
        <div>
          {/* You could render a loading UI here */}
          Loading configuration...
        </div>
      }
    >
      <ConfigurationLoader>{children}</ConfigurationLoader>
    </Suspense>
  )
}

// Custom hook
export function useConfiguration(): ConfigurationContextType {
  const context = useContext(ConfigurationContext)

  if (context === undefined) {
    throw new Error('useConfiguration must be used within a ConfigurationProvider')
  }

  return context
}

/**
 * NOTES:
 * 
 * 1. use() hook requires Suspense boundary - this is automatic with React 19
 * 2. use() hook throws on Promise rejection - need error boundary or catch
 * 3. Manual refresh is harder with use() - would need to recreate Promise
 * 4. The traditional useState + useEffect pattern is better for contexts
 *    that need error handling, loading states, and manual refresh
 * 
 * use() hook is better suited for:
 * - Component-level data loading
 * - Lazy loading components
 * - Simple Promise unwrapping in components
 */

