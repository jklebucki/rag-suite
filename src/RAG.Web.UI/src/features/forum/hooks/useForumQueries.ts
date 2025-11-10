import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  acknowledgeThreadBadge,
  createForumCategory,
  createForumPost,
  createForumThread,
  deleteForumCategory,
  fetchForumCategories,
  fetchForumThread,
  fetchForumThreads,
  fetchThreadBadges,
  getForumSettings,
  subscribeToThread,
  unsubscribeFromThread,
  updateForumCategory,
  updateForumSettings,
} from '../services/forum.service'
import type {
  CreatePostPayload,
  CreateThreadPayload,
  ForumCategory,
  ForumSettings,
  ForumThreadDetail,
  ListThreadsParams,
  ListThreadsResponse,
  ThreadBadgesResponse,
} from '../types/forum'

export const forumKeys = {
  all: ['forum'] as const,
  threads: (params: ListThreadsParams) => [...forumKeys.all, 'threads', params] as const,
  thread: (threadId: string) => [...forumKeys.all, 'thread', threadId] as const,
  badges: () => [...forumKeys.all, 'badges'] as const,
  settings: () => [...forumKeys.all, 'settings'] as const,
  categories: () => [...forumKeys.all, 'categories'] as const,
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

export function useForumSettingsQuery(options?: { enabled?: boolean }) {
  return useQuery<ForumSettings>({
    queryKey: forumKeys.settings(),
    queryFn: getForumSettings,
    enabled: options?.enabled ?? true,
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

export function useThreadBadges(enabled: boolean, refreshSeconds = 60) {
  return useQuery<ThreadBadgesResponse>({
    queryKey: forumKeys.badges(),
    queryFn: fetchThreadBadges,
    enabled,
    refetchInterval: enabled ? refreshSeconds * 1000 : false,
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

export function useUpdateForumSettings() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: ForumSettings) => updateForumSettings(payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: forumKeys.settings() })
    },
  })
}

export function useCreateForumCategoryMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: createForumCategory,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: forumKeys.categories() })
    },
  })
}

export function useUpdateForumCategoryMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: updateForumCategory,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: forumKeys.categories() })
    },
  })
}

export function useDeleteForumCategoryMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: deleteForumCategory,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: forumKeys.categories() })
    },
  })
}

