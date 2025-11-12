/**
 * useOptimisticMutation - Custom hook combining useOptimistic with React Query mutations
 * 
 * This hook provides optimistic updates for mutations, automatically rolling back
 * on error and syncing with React Query cache.
 * 
 * Usage:
 * ```tsx
 * const { optimisticData, addOptimistic, mutation } = useOptimisticMutation({
 *   queryKey: ['messages'],
 *   mutationFn: sendMessage,
 *   onOptimistic: (newMessage) => ({ ...newMessage, id: `temp-${Date.now()}` }),
 *   onSuccess: (data) => data,
 * })
 * ```
 */

import { useOptimistic, useTransition, useMemo } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { UseMutationOptions, UseMutationResult } from '@tanstack/react-query'

interface UseOptimisticMutationOptions<TData, TVariables, TContext> {
  queryKey: unknown[]
  mutationFn: (variables: TVariables) => Promise<TData>
  onOptimistic: (variables: TVariables) => TData
  onSuccess?: (data: TData) => TData | void
  onError?: (error: Error, variables: TVariables, context: TContext | undefined) => void
  optimisticUpdateFn?: (currentData: TData[], optimisticItem: TData) => TData[]
  options?: Omit<UseMutationOptions<TData, Error, TVariables, TContext>, 'mutationFn' | 'onSuccess' | 'onError'>
}

interface UseOptimisticMutationReturn<TData, TVariables> {
  optimisticData: TData[]
  addOptimistic: (variables: TVariables) => void
  mutation: UseMutationResult<TData, Error, TVariables>
  isPending: boolean
}

export function useOptimisticMutation<TData, TVariables = void, TContext = unknown>({
  queryKey,
  mutationFn,
  onOptimistic,
  onSuccess,
  onError,
  optimisticUpdateFn,
  options,
}: UseOptimisticMutationOptions<TData, TVariables, TContext>): UseOptimisticMutationReturn<TData, TVariables> {
  const queryClient = useQueryClient()
  const [isPending, startTransition] = useTransition()

  // Get current data from cache
  // Note: We read directly from cache each render - useOptimistic will handle updates
  // useOptimistic needs a stable initial value, so we memoize it
  const currentData = useMemo(
    () => (queryClient.getQueryData<TData[]>(queryKey) || []) as TData[],
    [queryClient, queryKey]
  )

  // Optimistic state
  // useOptimistic tracks the base state and applies optimistic updates
  // The base state is currentData from cache, which updates when cache changes
  const [optimisticData, addOptimistic] = useOptimistic(
    currentData,
    optimisticUpdateFn || ((state: TData[], newItem: TData) => [...state, newItem])
  )

  // Mutation with automatic cache sync
  const mutation = useMutation({
    mutationFn,
    onSuccess: (data) => {
      // Update cache with real data
      queryClient.setQueryData<TData[]>(queryKey, (old = []) => {
        const updated = onSuccess ? onSuccess(data) : data
        if (updated) {
          // Replace optimistic item with real data
          return old.map((item: TData) => {
            // Find and replace optimistic item (you may need custom logic here)
            return item
          }).concat([updated as TData])
        }
        return old
      })
      
      // Invalidate to refetch
      queryClient.invalidateQueries({ queryKey })
    },
    onError: (error, variables, context) => {
      // Rollback is automatic with useOptimistic
      onError?.(error, variables, context)
    },
    ...options,
  })

  const handleAddOptimistic = (variables: TVariables) => {
    startTransition(() => {
      const optimisticItem = onOptimistic(variables)
      addOptimistic(optimisticItem)
    })
  }

  return {
    optimisticData,
    addOptimistic: handleAddOptimistic,
    mutation,
    isPending: isPending || mutation.isPending,
  }
}

