export interface ForumCategory {
  id: string
  name: string
  slug: string
  description?: string | null
  isArchived: boolean
  order: number
  createdAt: string
  updatedAt: string
}

export interface ForumAttachment {
  id: string
  fileName: string
  contentType: string
  size: number
  createdAt: string
  postId?: string | null
}

export interface ForumPost {
  id: string
  threadId: string
  authorId: string
  authorEmail: string
  content: string
  isAnswer: boolean
  createdAt: string
  updatedAt: string
  attachments: ForumAttachment[]
}

export interface ForumThreadSummary {
  id: string
  categoryId: string
  categoryName: string
  title: string
  authorId: string
  authorEmail: string
  createdAt: string
  updatedAt: string
  lastPostAt: string
  isLocked: boolean
  viewCount: number
  replyCount: number
  attachmentCount: number
}

export interface ForumThreadDetail extends ForumThreadSummary {
  content: string
  attachments: ForumAttachment[]
  posts: ForumPost[]
}

export interface ListThreadsResponse {
  threads: ForumThreadSummary[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface GetThreadResponse {
  thread: ForumThreadDetail
}

export interface CreateThreadResponse {
  thread: ForumThreadDetail
}

export interface CreatePostResponse {
  post: ForumPost
}

export interface ThreadBadge {
  threadId: string
  threadTitle: string
  categoryName: string
  hasUnreadReplies: boolean
  updatedAt: string
  lastSeenPostId?: string | null
}

export interface ThreadBadgesResponse {
  badges: ThreadBadge[]
}

export interface UploadAttachment {
  fileName: string
  contentType: string
  dataBase64: string
  size: number
}

export interface CreateThreadPayload {
  categoryId: string
  title: string
  content: string
  attachments: UploadAttachment[]
}

export interface CreatePostPayload {
  content: string
  subscribeToThread: boolean
  attachments: UploadAttachment[]
}

export interface ListThreadsParams {
  page?: number
  pageSize?: number
  categoryId?: string
  search?: string
}

export interface ForumSettings {
  enableAttachments: boolean
  maxAttachmentCount: number
  maxAttachmentSizeMb: number
  enableEmailNotifications: boolean
  badgeRefreshSeconds: number
}

