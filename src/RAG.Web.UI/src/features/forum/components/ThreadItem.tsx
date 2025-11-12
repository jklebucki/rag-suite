/**
 * ThreadItem - Optimized thread item component using React.memo
 * 
 * This component is memoized to prevent unnecessary re-renders when other
 * threads in the list are updated.
 */

import React from 'react'
import { Card } from '@/shared/components/ui/Card'
import { formatRelativeTime } from '@/utils/date'
import type { ForumThreadSummary } from '../types/forum'
import type { LanguageCode } from '@/shared/types/i18n'

interface ThreadItemProps {
  thread: ForumThreadSummary
  language: LanguageCode
  hasUnread: boolean
  onThreadClick: (threadId: string) => void
  t: (key: string, params?: Record<string, string>) => string
}

export const ThreadItem = React.memo<ThreadItemProps>(({
  thread,
  language,
  hasUnread,
  onThreadClick,
  t,
}) => {
  return (
    <Card
      key={thread.id}
      className="border border-gray-200 transition-shadow hover:shadow-md dark:border-gray-700"
    >
      <button
        type="button"
        onClick={() => onThreadClick(thread.id)}
        className="flex w-full flex-col gap-3 p-4 text-left sm:flex-row sm:items-start sm:justify-between"
      >
        <div className="space-y-2">
          <div className="flex items-center gap-2">
            <span className="text-xs font-medium uppercase tracking-wide text-primary-600 dark:text-primary-400">
              {thread.categoryName}
            </span>
            {hasUnread && (
              <span className="inline-flex items-center rounded-full bg-primary-500 px-2 py-0.5 text-xs font-semibold text-white">
                {t('forum.list.badgeNew')}
              </span>
            )}
          </div>
          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">{thread.title}</h3>
          <p className="text-sm text-gray-600 dark:text-gray-300">
            {t('forum.list.meta', {
              author: thread.authorEmail || thread.authorId,
              replies: String(thread.replyCount),
              attachments: String(thread.attachmentCount),
            })}
          </p>
        </div>
        <div className="text-sm text-gray-500 dark:text-gray-400">
          {formatRelativeTime(thread.lastPostAt, language)}
        </div>
      </button>
    </Card>
  )
}, (prevProps, nextProps) => {
  // Custom comparison for better performance
  return (
    prevProps.thread.id === nextProps.thread.id &&
    prevProps.thread.title === nextProps.thread.title &&
    prevProps.thread.replyCount === nextProps.thread.replyCount &&
    prevProps.thread.lastPostAt === nextProps.thread.lastPostAt &&
    prevProps.hasUnread === nextProps.hasUnread &&
    prevProps.language === nextProps.language
  )
})

ThreadItem.displayName = 'ThreadItem'

