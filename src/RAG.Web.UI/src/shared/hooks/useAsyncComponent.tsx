/**
 * useAsyncComponent - Hook using React 19's use() for lazy loading components
 * 
 * This hook demonstrates the use of React 19's use() hook for handling
 * Promise-based component loading. It's an alternative to React.lazy().
 * 
 * @example
 * const Component = useAsyncComponent(
 *   import('./HeavyComponent').then(m => ({ default: m.HeavyComponent }))
 * )
 */

import React, { use, Suspense } from 'react'
import type { ComponentType } from 'react'

/**
 * Hook that uses React 19's use() hook to load a component from a Promise
 * 
 * @param componentPromise - Promise that resolves to a component module
 * @returns The loaded component
 */
export function useAsyncComponent<T extends ComponentType<any>>(
  componentPromise: Promise<{ default: T }>
): T {
  const module = use(componentPromise)
  return module.default
}

/**
 * Wrapper component that uses use() hook for lazy loading
 * This is an alternative to React.lazy() using React 19's use() hook
 */
export function AsyncComponent<T extends ComponentType<any>>({
  componentPromise,
  fallback,
  ...props
}: {
  componentPromise: Promise<{ default: T }>
  fallback?: React.ReactNode
} & React.ComponentProps<T>): React.ReactElement {
  const Component = useAsyncComponent(componentPromise)
  
  return (
    <Suspense fallback={fallback || <div>Loading...</div>}>
      <Component {...(props as any)} />
    </Suspense>
  )
}

