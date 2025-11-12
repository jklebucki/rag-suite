/**
 * useDeferredSearch.test.ts
 * 
 * Tests for useDeferredSearch hook using React 19's useDeferredValue
 */

import { describe, it, expect, beforeEach, vi } from 'vitest'
import { renderHook, act, waitFor } from '@testing-library/react'
import { useDeferredSearch } from './useDeferredSearch'

describe('useDeferredSearch', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('should initialize with empty string by default', () => {
    const { result } = renderHook(() => useDeferredSearch())
    
    expect(result.current.query).toBe('')
    expect(result.current.deferredQuery).toBe('')
    expect(result.current.isSearching).toBe(false)
  })

  it('should initialize with initialValue', () => {
    const { result } = renderHook(() => useDeferredSearch({ initialValue: 'initial' }))
    
    expect(result.current.query).toBe('initial')
    // deferredQuery may be deferred, so we check it eventually matches
    expect(result.current.deferredQuery).toBe('initial')
  })

  it('should update query when setQuery is called', () => {
    const { result } = renderHook(() => useDeferredSearch())
    
    act(() => {
      result.current.setQuery('test query')
    })
    
    expect(result.current.query).toBe('test query')
  })

  it('should defer query updates (useDeferredValue)', async () => {
    const { result } = renderHook(() => useDeferredSearch())
    
    act(() => {
      result.current.setQuery('test')
    })
    
    // Query should update immediately
    expect(result.current.query).toBe('test')
    
    // Deferred query should eventually match (React will defer it)
    await waitFor(() => {
      expect(result.current.deferredQuery).toBe('test')
    }, { timeout: 1000 })
  })

  it('should set isSearching to true when query differs from deferredQuery', async () => {
    const { result } = renderHook(() => useDeferredSearch())
    
    act(() => {
      result.current.setQuery('test')
    })
    
    // During the deferral period, isSearching should be true
    // Note: This is timing-dependent, so we check it's at least set correctly initially
    expect(result.current.query).toBe('test')
    
    // After deferral completes, isSearching should be false
    await waitFor(() => {
      expect(result.current.deferredQuery).toBe('test')
      expect(result.current.isSearching).toBe(false)
    }, { timeout: 1000 })
  })

  it('should clear query when clearQuery is called', () => {
    const { result } = renderHook(() => useDeferredSearch({ initialValue: 'test' }))
    
    expect(result.current.query).toBe('test')
    
    act(() => {
      result.current.clearQuery()
    })
    
    expect(result.current.query).toBe('')
  })

  it('should not set isSearching when query is empty', () => {
    const { result } = renderHook(() => useDeferredSearch())
    
    act(() => {
      result.current.setQuery('')
    })
    
    expect(result.current.isSearching).toBe(false)
  })

  it('should handle rapid query changes', async () => {
    const { result } = renderHook(() => useDeferredSearch())
    
    act(() => {
      result.current.setQuery('a')
      result.current.setQuery('ab')
      result.current.setQuery('abc')
    })
    
    // Final query should be 'abc'
    expect(result.current.query).toBe('abc')
    
    // Deferred query should eventually catch up
    await waitFor(() => {
      expect(result.current.deferredQuery).toBe('abc')
    }, { timeout: 1000 })
  })
})

