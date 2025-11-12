/**
 * useAsyncData.test.ts
 * 
 * Tests for useAsyncData hook using React 19's use() hook
 * 
 * Note: use() hook requires Suspense boundary and has special behavior.
 * These tests verify the hook logic and memoization.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook } from '@testing-library/react'
import { useMemoizedPromise } from './useAsyncData'

describe('useMemoizedPromise', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('should memoize Promise based on dependencies', () => {
    const promiseFactory = vi.fn(() => Promise.resolve('data'))

    const { rerender } = renderHook(
      ({ deps }) => useMemoizedPromise(promiseFactory, deps),
      {
        initialProps: { deps: ['dep1'] },
      }
    )

    expect(promiseFactory).toHaveBeenCalledTimes(1)

    // Re-render with same deps - should not recreate promise
    rerender({ deps: ['dep1'] })
    expect(promiseFactory).toHaveBeenCalledTimes(1)

    // Re-render with different deps - should recreate promise
    rerender({ deps: ['dep2'] })
    expect(promiseFactory).toHaveBeenCalledTimes(2)
  })

  it('should create new Promise when dependencies change', () => {
    const promiseFactory = vi.fn(() => Promise.resolve('data'))

    const { result, rerender } = renderHook(
      ({ deps }) => useMemoizedPromise(promiseFactory, deps),
      {
        initialProps: { deps: [1] },
      }
    )

    const firstPromise = result.current

    // Change dependency
    rerender({ deps: [2] })

    // Should create new promise
    expect(result.current).not.toBe(firstPromise)
    expect(promiseFactory).toHaveBeenCalledTimes(2)
  })

  it('should return same Promise when dependencies do not change', () => {
    const promiseFactory = vi.fn(() => Promise.resolve('data'))

    const { result, rerender } = renderHook(
      ({ deps }) => useMemoizedPromise(promiseFactory, deps),
      {
        initialProps: { deps: ['same'] },
      }
    )

    const firstPromise = result.current

    // Re-render with same deps
    rerender({ deps: ['same'] })

    // Should return same promise
    expect(result.current).toBe(firstPromise)
    expect(promiseFactory).toHaveBeenCalledTimes(1)
  })
})

// Note: useAsyncData and useAsyncDataWithFallback use use() hook
// which requires Suspense boundary and Error Boundary for proper testing.
// These are better tested in integration tests with actual components.

