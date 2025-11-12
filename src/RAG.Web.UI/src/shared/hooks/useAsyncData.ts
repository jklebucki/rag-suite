/**
 * useAsyncData - Hook using React 19's use() for async data loading
 * 
 * This hook demonstrates the use of React 19's use() hook for handling
 * Promise-based data loading without useState/useEffect.
 * 
 * @example
 * const data = useAsyncData(fetchUserData(userId))
 */

import { use, useMemo } from 'react'

/**
 * Hook that uses React 19's use() hook to unwrap a Promise
 * 
 * This simplifies async data loading by directly using the Promise
 * without needing useState and useEffect.
 * 
 * @param promise - Promise to unwrap
 * @returns The resolved value from the Promise
 * 
 * @example
 * function UserProfile({ userId }: { userId: string }) {
 *   const user = useAsyncData(fetchUser(userId))
 *   return <div>{user.name}</div>
 * }
 */
export function useAsyncData<T>(promise: Promise<T>): T {
  return use(promise)
}

/**
 * Hook that uses React 19's use() hook with error handling
 * 
 * @param promise - Promise to unwrap
 * @param fallback - Fallback value to use if Promise rejects
 * @returns The resolved value or fallback
 */
export function useAsyncDataWithFallback<T>(
  promise: Promise<T>,
  fallback: T
): T {
  try {
    return use(promise)
  } catch (error) {
    // If Promise rejects, use() will throw, so we catch and return fallback
    return fallback
  }
}

/**
 * Hook that creates a memoized Promise for use with use()
 * 
 * This is useful when you need to recreate the Promise based on dependencies
 * 
 * @param promiseFactory - Function that creates the Promise
 * @param deps - Dependencies array (like useEffect)
 * @returns Memoized Promise
 */
export function useMemoizedPromise<T>(
  promiseFactory: () => Promise<T>,
  deps: React.DependencyList
): Promise<T> {
  return useMemo(() => promiseFactory(), deps)
}

