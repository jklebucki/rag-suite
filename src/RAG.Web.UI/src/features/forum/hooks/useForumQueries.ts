import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  acknowledgeThreadBadge,
  createForumPost,
  createForumThread,
  fetchForumCategories,
  fetchForumThread,
  fetchForumThreads,
  fetchThreadBadges,
  subscribeToThread,
  unsubscribeFromThread,
} from '../services/forum.service'
import type {
  CreatePostPayload,
  CreateThreadPayload,
  ForumCategory,
  ForumThreadDetail,
  ListThreadsParams,
  ListThreadsResponse,
  ThreadBadgesResponse,
} from '../types/forum'

export const forumKeys = {
  all: ['forum'] as const,
  categories: () => [...forumKeys.all, 'categories'] as const,
  threads: (params: ListThreadsParams) => [...forumKeys.all, 'threads', params] as const,
  thread: (threadId: string) => [...forumKeys.all, 'thread', threadId] as const,
  badges: () => [...forumKeys.all, 'badges'] as const,
}

export function useForumCategories() {
  return useQuery<ForumCategory[]>({
    queryKey: forumKeys.categories(),
    queryFn: fetchForumCategories,
    staleTime: 1000 * 60 * 10,
  })
}

export function useForumThreads(params: ListThreadsParams) {
  return useQuery<ListThreadsResponse>({
    queryKey: forumKeys.threads(params),
    queryFn: () => fetchForumThreads(params),
  })
}

export function useForumThread(threadId?: string) {
  return useQuery<ForumThreadDetail>({
    queryKey: threadId ? forumKeys.thread(threadId) : ['forum', 'thread', 'unknown'],
    queryFn: () => fetchForumThread(threadId as string),
    enabled: Boolean(threadId),
  })
}

export function useCreateForumThread() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: CreateThreadPayload) => createForumThread(payload),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: forumKeys.categories() }),
        queryClient.invalidateQueries({ queryKey: forumKeys.badges() }),
        queryClient.invalidateQueries({ queryKey: forumKeys.all, exact: false }),
      ])
    },
  })
}

export function useCreateForumPost(threadId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: CreatePostPayload) => createForumPost(threadId, payload),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: forumKeys.thread(threadId) }),
        queryClient.invalidateQueries({ queryKey: forumKeys.badges() }),
        queryClient.invalidateQueries({ queryKey: forumKeys.all, exact: false }),
      ])
    },
  })
}

export function useThreadBadges(enabled: boolean) {
  return useQuery<ThreadBadgesResponse>({
    queryKey: forumKeys.badges(),
    queryFn: fetchThreadBadges,
    enabled,
    refetchInterval: enabled ? 60_000 : false,
  })
}

export function useSubscribeToThread(threadId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (notifyOnReply: boolean) => subscribeToThread(threadId, notifyOnReply),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: forumKeys.thread(threadId) }),
        queryClient.invalidateQueries({ queryKey: forumKeys.badges() }),
      ])
    },
  })
}

export function useUnsubscribeFromThread(threadId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => unsubscribeFromThread(threadId),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: forumKeys.thread(threadId) }),
        queryClient.invalidateQueries({ queryKey: forumKeys.badges() }),
      ])
    },
  })
}

export function useAcknowledgeThreadBadge() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (threadId: string) => acknowledgeThreadBadge(threadId),
    onSuccess: async (_data, threadId) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: forumKeys.badges() }),
        queryClient.invalidateQueries({ queryKey: forumKeys.thread(threadId) }),
      ])
    },
  })
}

