import { apiHttpClient, forumHttpClient } from '@/shared/services/api/httpClients'
import type { ApiResponse } from '@/shared/types/api'
import type {
  CreatePostPayload,
  CreatePostResponse,
  CreateThreadPayload,
  CreateThreadResponse,
  ForumCategory,
  ForumSettings,
  ForumThreadDetail,
  GetThreadResponse,
  ListThreadsParams,
  ListThreadsResponse,
  ThreadBadgesResponse,
} from '../types/forum'

export interface CreateForumCategoryPayload {
  name: string
  slug: string
  description?: string | null
  order?: number
  isArchived?: boolean
}

export interface UpdateForumCategoryPayload extends CreateForumCategoryPayload {
  id: string
}

export async function fetchForumCategories(): Promise<ForumCategory[]> {
  const { data } = await forumHttpClient.get<{ categories: ForumCategory[] }>('/categories')
  return data.categories
}

export async function fetchForumThreads(params: ListThreadsParams): Promise<ListThreadsResponse> {
  const { data } = await forumHttpClient.get<ListThreadsResponse>('/threads', { params })
  return data
}

export async function fetchForumThread(threadId: string): Promise<ForumThreadDetail> {
  const { data } = await forumHttpClient.get<ApiResponse<GetThreadResponse>>(`/threads/${threadId}`)
  return data.data.thread
}

export async function createForumThread(payload: CreateThreadPayload): Promise<CreateThreadResponse> {
  const { data } = await forumHttpClient.post<CreateThreadResponse>('/threads', payload)
  return data
}

export async function createForumPost(threadId: string, payload: CreatePostPayload): Promise<CreatePostResponse> {
  const { data } = await forumHttpClient.post<CreatePostResponse>(`/threads/${threadId}/posts`, payload)
  return data
}

export async function downloadForumAttachment(threadId: string, attachmentId: string): Promise<Blob> {
  const { data } = await forumHttpClient.get<Blob>(`/threads/${threadId}/attachments/${attachmentId}`, {
    responseType: 'blob',
  })
  return data
}

export async function subscribeToThread(threadId: string, notifyOnReply: boolean): Promise<void> {
  await forumHttpClient.post(`/threads/${threadId}/subscribe`, { notifyOnReply })
}

export async function unsubscribeFromThread(threadId: string): Promise<void> {
  await forumHttpClient.delete(`/threads/${threadId}/subscribe`)
}

export async function fetchThreadBadges(): Promise<ThreadBadgesResponse> {
  const { data } = await forumHttpClient.get<ThreadBadgesResponse>('/badges')
  return data
}

export async function acknowledgeThreadBadge(threadId: string): Promise<void> {
  await forumHttpClient.patch(`/badges/${threadId}/ack`)
}

export async function getForumSettings(): Promise<ForumSettings> {
  const { data } = await apiHttpClient.get<ForumSettings>('/settings/forum')
  return data
}

export async function updateForumSettings(payload: ForumSettings): Promise<void> {
  await apiHttpClient.put('/settings/forum', payload)
}

export async function createForumCategory(payload: CreateForumCategoryPayload): Promise<ForumCategory> {
  const { data } = await forumHttpClient.post<ForumCategory>('/categories', payload)
  return data
}

export async function updateForumCategory(payload: UpdateForumCategoryPayload): Promise<ForumCategory> {
  const { id, ...rest } = payload
  const { data } = await forumHttpClient.put<ForumCategory>(`/categories/${id}`, rest)
  return data
}

export async function deleteForumCategory(id: string): Promise<void> {
  await forumHttpClient.delete(`/categories/${id}`)
}

