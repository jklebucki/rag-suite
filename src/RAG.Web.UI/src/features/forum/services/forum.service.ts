import { forumHttpClient } from '@/shared/services/api/httpClients'
import type {
  CreatePostPayload,
  CreatePostResponse,
  CreateThreadPayload,
  CreateThreadResponse,
  ForumCategory,
  ForumThreadDetail,
  GetThreadResponse,
  ListThreadsParams,
  ListThreadsResponse,
  ThreadBadgesResponse,
} from '../types/forum'

export async function fetchForumCategories(): Promise<ForumCategory[]> {
  const { data } = await forumHttpClient.get<{ categories: ForumCategory[] }>('/categories')
  return data.categories
}

export async function fetchForumThreads(params: ListThreadsParams): Promise<ListThreadsResponse> {
  const { data } = await forumHttpClient.get<ListThreadsResponse>('/threads', { params })
  return data
}

export async function fetchForumThread(threadId: string): Promise<ForumThreadDetail> {
  const { data } = await forumHttpClient.get<GetThreadResponse>(`/threads/${threadId}`)
  return data.thread
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

