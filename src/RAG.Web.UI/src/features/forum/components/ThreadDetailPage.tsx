import React, { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { ArrowLeft, Download, Loader2 } from 'lucide-react'
import { Button } from '@/shared/components/ui/Button'
import { Card } from '@/shared/components/ui/Card'
import { useI18n } from '@/shared/contexts/I18nContext'
import { useAuth } from '@/shared/contexts/AuthContext'
import { useToast } from '@/shared/contexts/ToastContext'
import {
  useAcknowledgeThreadBadge,
  useCreateForumPost,
  useForumSettingsQuery,
  useForumThread,
  useThreadBadges,
  useSubscribeToThread,
  useUnsubscribeFromThread,
} from '../hooks/useForumQueries'
import { AttachmentPicker, AttachmentDraft } from './AttachmentPicker'
import { downloadForumAttachment } from '../services/forum.service'
import { formatDateTime } from '@/utils/date'
import type { ForumAttachment, ForumPost } from '../types/forum'
import type { LanguageCode } from '@/shared/types/i18n'

export function ThreadDetailPage() {
  const { threadId } = useParams<{ threadId: string }>()
  const navigate = useNavigate()
  const { t, language } = useI18n()
  const { isAuthenticated } = useAuth()
  const { showError, showSuccess } = useToast()

  const threadQuery = useForumThread(threadId)
  const createPostMutation = useCreateForumPost(threadId ?? '')
  const acknowledgeBadge = useAcknowledgeThreadBadge()
  const subscribeMutation = useSubscribeToThread(threadId ?? '')
  const unsubscribeMutation = useUnsubscribeFromThread(threadId ?? '')
  const forumSettingsQuery = useForumSettingsQuery({ enabled: isAuthenticated })
  const forumSettings = forumSettingsQuery.data
  const attachmentsEnabled = forumSettings?.enableAttachments ?? true
  const maxAttachmentCount = forumSettings?.maxAttachmentCount ?? 5
  const maxAttachmentSizeMb = forumSettings?.maxAttachmentSizeMb ?? 5
  const badgeRefreshSeconds = forumSettings?.badgeRefreshSeconds ?? 60
  const badgesQuery = useThreadBadges(isAuthenticated, badgeRefreshSeconds)

  const [replyContent, setReplyContent] = useState('')
  const [attachments, setAttachments] = useState<AttachmentDraft[]>([])
  const [subscribeToThread, setSubscribeToThread] = useState(true)

  useEffect(() => {
    if (!attachmentsEnabled && attachments.length > 0) {
      setAttachments([])
    }
  }, [attachmentsEnabled, attachments])

  const isSubscribed = useMemo(() => {
    if (!threadId) return false
    return badgesQuery.data?.badges.some((badge) => badge.threadId === threadId) ?? false
  }, [badgesQuery.data, threadId])

  useEffect(() => {
    if (!threadId || !badgesQuery.data) return
    const badge = badgesQuery.data.badges.find((item) => item.threadId === threadId)
    if (badge?.hasUnreadReplies) {
      acknowledgeBadge.mutate(threadId)
    }
  }, [acknowledgeBadge, badgesQuery.data, threadId])

useEffect(() => {
  setSubscribeToThread(isSubscribed ? false : forumSettings?.enableEmailNotifications ?? true)
}, [isSubscribed, forumSettings?.enableEmailNotifications])

  const handleSubmitReply = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (!threadId) {
      return
    }

    if (!replyContent.trim()) {
      showError(t('forum.reply.validation.content'))
      return
    }

    try {
      await createPostMutation.mutateAsync({
        content: replyContent.trim(),
        subscribeToThread,
        attachments: attachmentsEnabled
          ? attachments.map(({ fileName, contentType, dataBase64, size }) => ({
              fileName,
              contentType,
              dataBase64,
              size,
            }))
          : [],
      })
      showSuccess(t('forum.reply.success'))
      setReplyContent('')
      setAttachments([])
    } catch (error) {
      console.error(error)
      showError(t('forum.reply.error'))
    }
  }

  const handleDownloadAttachment = async (attachment: ForumAttachment) => {
    if (!threadId) return

    try {
      const blob = await downloadForumAttachment(threadId, attachment.id)
      const url = window.URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = attachment.fileName
      document.body.appendChild(link)
      link.click()
      link.remove()
      window.URL.revokeObjectURL(url)
    } catch (error) {
      console.error(error)
      showError(t('forum.attachments.downloadError'))
    }
  }

  const handleSubscribeToggle = async () => {
    if (!threadId) return

    try {
      if (isSubscribed) {
        await unsubscribeMutation.mutateAsync()
        showSuccess(t('forum.subscription.unsubscribed'))
      } else {
        await subscribeMutation.mutateAsync(true)
        showSuccess(t('forum.subscription.subscribed'))
      }
    } catch (error) {
      console.error(error)
      showError(t('forum.subscription.error'))
    }
  }

  if (threadQuery.isLoading) {
    return (
      <div className="flex min-h-[240px] items-center justify-center">
        <Loader2 className="h-6 w-6 animate-spin text-primary-500" />
      </div>
    )
  }

  if (threadQuery.isError || !threadQuery.data) {
    return (
      <Card className="p-6 text-gray-700 dark:text-gray-200">
        <p>{t('forum.detail.error')}</p>
        <div className="mt-4">
          <Button variant="secondary" onClick={() => navigate('/forum')}>
            {t('common.back')}
          </Button>
        </div>
      </Card>
    )
  }

  const thread = threadQuery.data

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Button variant="secondary" onClick={() => navigate(-1)}>
          <ArrowLeft className="mr-2 h-4 w-4" />
          {t('common.back')}
        </Button>
        <h1 className="text-2xl font-semibold text-gray-900 dark:text-gray-100">{thread.title}</h1>
      </div>

      <Card className="border border-gray-200 dark:border-gray-700">
        <div className="space-y-4 p-6">
          <div className="flex flex-wrap items-center gap-4 text-sm text-gray-600 dark:text-gray-300">
            <span className="rounded-full bg-primary-50 px-3 py-1 font-medium text-primary-700 dark:bg-primary-900/30 dark:text-primary-200">
              {thread.categoryName}
            </span>
            <span>{t('forum.detail.author', { author: thread.authorEmail || thread.authorId })}</span>
            <span>{t('forum.detail.created', { date: formatDateTime(thread.createdAt, language) })}</span>
            <span>{t('forum.detail.updated', { date: formatDateTime(thread.lastPostAt, language) })}</span>
          </div>

          <article className="prose max-w-none text-gray-800 dark:prose-invert dark:text-gray-100">
            {thread.content.split('\n').map((paragraph, index) => (
              <p key={index} className="whitespace-pre-wrap">
                {paragraph}
              </p>
            ))}
          </article>

          {thread.attachments.length > 0 && (
            <AttachmentList
              attachments={thread.attachments}
              onDownload={handleDownloadAttachment}
              title={t('forum.attachments.threadTitle')}
            />
          )}
        </div>
      </Card>

      <section className="space-y-4">
        <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
          {t('forum.detail.replies', { count: String(thread.posts.length) })}
        </h2>

        {thread.posts.length === 0 ? (
          <Card className="p-4 text-sm text-gray-500 dark:text-gray-300">{t('forum.detail.noReplies')}</Card>
        ) : (
          <div className="space-y-3">
            {thread.posts.map((post) => (
              <PostCard key={post.id} post={post} onDownload={handleDownloadAttachment} language={language} />
            ))}
          </div>
        )}
      </section>

      <section className="space-y-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">{t('forum.reply.title')}</h2>
          <Button variant="outline" onClick={handleSubscribeToggle} disabled={subscribeMutation.isPending || unsubscribeMutation.isPending}>
            {isSubscribed ? t('forum.subscription.unsubscribe') : t('forum.subscription.subscribe')}
          </Button>
        </div>

        <Card className="border border-gray-200 dark:border-gray-700">
          <form onSubmit={handleSubmitReply} className="space-y-4 p-6">
            <div>
              <label htmlFor="forum-reply-content" className="block text-sm font-medium text-gray-700 dark:text-gray-200">
                {t('forum.reply.fields.content')}
              </label>
              <textarea
                id="forum-reply-content"
                name="replyContent"
                value={replyContent}
                onChange={(event) => setReplyContent(event.target.value)}
                rows={5}
                className="mt-1 w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100"
                maxLength={4000}
                placeholder={t('forum.reply.fields.content')}
                aria-label={t('forum.reply.fields.content')}
                required
              />
            </div>

            <label className="flex items-center text-sm text-gray-600 dark:text-gray-300">
              <input
                id="forum-reply-subscribe"
                type="checkbox"
                checked={subscribeToThread}
                onChange={(event) => setSubscribeToThread(event.target.checked)}
                className="form-checkbox"
              />
              <span className="ml-2">{t('forum.reply.subscribeLabel')}</span>
            </label>

            {attachmentsEnabled ? (
              <AttachmentPicker
                attachments={attachments}
                onAttachmentsChange={setAttachments}
                disabled={createPostMutation.isPending}
                inputId="forum-reply-attachments"
                maxAttachments={maxAttachmentCount}
                maxAttachmentSizeMb={maxAttachmentSizeMb}
              />
            ) : (
              <p className="text-sm text-gray-500 dark:text-gray-400">{t('forum.attachments.disabled')}</p>
            )}

            <div className="flex justify-end gap-3">
              <Button
                type="button"
                variant="secondary"
                onClick={() => {
                  setReplyContent('')
                  setAttachments([])
                }}
                disabled={createPostMutation.isPending}
              >
                {t('common.clear')}
              </Button>
              <Button type="submit" variant="primary" disabled={createPostMutation.isPending}>
                {createPostMutation.isPending ? t('common.processing') : t('forum.reply.submit')}
              </Button>
            </div>
          </form>
        </Card>
      </section>
    </div>
  )
}

interface AttachmentListProps {
  attachments: ForumAttachment[]
  onDownload: (attachment: ForumAttachment) => void
  title: string
}

function AttachmentList({ attachments, onDownload, title }: AttachmentListProps) {
  return (
    <div className="rounded-lg border border-gray-200 bg-gray-50 p-4 dark:border-gray-700 dark:bg-gray-800/40">
      <h3 className="text-sm font-medium text-gray-700 dark:text-gray-200">{title}</h3>
      <ul className="mt-3 space-y-2">
        {attachments.map((attachment) => (
          <li
            key={attachment.id}
            className="flex items-center justify-between rounded border border-gray-200 bg-white px-3 py-2 text-sm dark:border-gray-700 dark:bg-gray-900"
          >
            <div className="flex flex-col">
              <span className="font-medium text-gray-800 dark:text-gray-100 break-all">{attachment.fileName}</span>
              <span className="text-xs text-gray-500 dark:text-gray-400">
                {formatSize(attachment.size)} • {attachment.contentType}
              </span>
            </div>
            <Button variant="ghost" type="button" onClick={() => onDownload(attachment)}>
              <Download className="h-4 w-4" />
            </Button>
          </li>
        ))}
      </ul>
    </div>
  )
}

interface PostCardProps {
  post: ForumPost
  language: LanguageCode
  onDownload: (attachment: ForumAttachment) => void
}

const PostCard = React.memo<PostCardProps>(({ post, language, onDownload }) => {
  const { t } = useI18n()

  return (
    <Card className="border border-gray-200 dark:border-gray-700">
      <div className="space-y-3 p-5">
        <div className="flex flex-wrap items-center justify-between gap-2 text-sm text-gray-600 dark:text-gray-400">
          <span className="font-medium text-gray-800 dark:text-gray-100">{post.authorEmail || post.authorId}</span>
          <span>{t('forum.detail.postedAt', { date: formatDateTime(post.createdAt, language) })}</span>
        </div>
        <div className="text-sm text-gray-800 dark:text-gray-100 whitespace-pre-wrap">{post.content}</div>
        {post.attachments.length > 0 && (
          <AttachmentList
            attachments={post.attachments}
            onDownload={onDownload}
            title={t('forum.attachments.replyTitle')}
          />
        )}
      </div>
    </Card>
  )
}, (prevProps, nextProps) => {
  // Custom comparison for better performance
  return (
    prevProps.post.id === nextProps.post.id &&
    prevProps.post.content === nextProps.post.content &&
    prevProps.post.createdAt === nextProps.post.createdAt &&
    prevProps.language === nextProps.language
  )
})

PostCard.displayName = 'PostCard'

// Export for testing
export { PostCard }

function formatSize(bytes: number): string {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  const value = bytes / Math.pow(k, i)
  return `${value.toFixed(value > 100 ? 0 : 1)} ${sizes[i]}`
}

