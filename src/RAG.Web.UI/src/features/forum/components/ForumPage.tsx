import React, { useMemo, useState, useOptimistic, useTransition, useDeferredValue } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus, RefreshCcw, Search as SearchIcon } from 'lucide-react'
import { Button } from '@/shared/components/ui/Button'
import { Card } from '@/shared/components/ui/Card'
import { Input } from '@/shared/components/ui/Input'
import { useI18n } from '@/shared/contexts/I18nContext'
import { useAuth } from '@/shared/contexts/AuthContext'
import { useToast } from '@/shared/contexts/ToastContext'
import { formatRelativeTime } from '@/utils/date'
import {
  useForumCategories,
  useForumThreads,
  useCreateForumThread,
  useThreadBadges,
  useForumSettingsQuery,
} from '../hooks/useForumQueries'
import type { ForumCategory, ForumSettings, ForumThreadSummary, ListThreadsParams } from '../types/forum'
import type { LanguageCode } from '@/shared/types/i18n'
import { AttachmentPicker, AttachmentDraft } from './AttachmentPicker'
import { Modal } from '@/shared/components/ui/Modal'
import { ThreadItem } from './ThreadItem'
import { SearchingIndicator } from '@/shared/components/common/SearchingIndicator'

const PAGE_SIZE = 10

export function ForumPage() {
  const { t, language } = useI18n()
  const navigate = useNavigate()
  const { showError, showSuccess } = useToast()
  const { isAuthenticated, user } = useAuth()

  const [page, setPage] = useState(1)
  const [selectedCategory, setSelectedCategory] = useState<string | undefined>()
  const [searchTerm, setSearchTerm] = useState('')
  const [isComposerOpen, setComposerOpen] = useState(false)

  // Use useDeferredValue for better UI responsiveness during typing
  const deferredSearchTerm = useDeferredValue(searchTerm)
  const isSearching = searchTerm !== deferredSearchTerm

  const threadParams: ListThreadsParams = useMemo(
    () => ({
      page,
      pageSize: PAGE_SIZE,
      categoryId: selectedCategory,
      search: deferredSearchTerm || undefined,
    }),
    [page, selectedCategory, deferredSearchTerm],
  )

  const categoriesQuery = useForumCategories()
  const threadsQuery = useForumThreads(threadParams)
  const createThreadMutation = useCreateForumThread()
  const forumSettingsQuery = useForumSettingsQuery({ enabled: isAuthenticated })
  const badgeRefreshSeconds = forumSettingsQuery.data?.badgeRefreshSeconds ?? 60
  const badgesQuery = useThreadBadges(isAuthenticated, badgeRefreshSeconds)

  // Optimistic threads state using React 19 useOptimistic
  const [isPending, startTransition] = useTransition()
  const baseThreads = threadsQuery.data?.threads ?? []
  const [optimisticThreads, addOptimisticThread] = useOptimistic(
    baseThreads,
    (state: ForumThreadSummary[], newThread: ForumThreadSummary) => {
      // Check if thread already exists (prevent duplicates)
      if (state.some(thread => thread.id === newThread.id)) {
        return state
      }
      // Add new thread at the beginning (most recent first)
      return [newThread, ...state]
    }
  )

  const unreadThreadIds = useMemo(() => {
    if (!badgesQuery.data) return new Set<string>()
    return new Set(badgesQuery.data.badges.filter((badge) => badge.hasUnreadReplies).map((badge) => badge.threadId))
  }, [badgesQuery.data])

  const handleOpenComposer = () => {
    setComposerOpen(true)
  }

  const handleCloseComposer = () => {
    setComposerOpen(false)
  }

  const handleCategoryChange = (categoryId?: string) => {
    setSelectedCategory(categoryId)
    setPage(1)
  }

  const handleSearchChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setSearchTerm(event.target.value)
    setPage(1)
  }

  const handleCreateThread = async (values: CreateThreadFormState) => {
    const category = categoriesQuery.data?.find(c => c.id === values.categoryId)
    const now = new Date().toISOString()

    // Create optimistic thread
    const optimisticThread: ForumThreadSummary = {
      id: `temp-${Date.now()}`,
      categoryId: values.categoryId,
      categoryName: category?.name || '',
      title: values.title,
      authorId: user?.id || '',
      authorEmail: user?.email || '',
      createdAt: now,
      updatedAt: now,
      lastPostAt: now,
      isLocked: false,
      viewCount: 0,
      replyCount: 0,
      attachmentCount: values.attachments.length,
    }

    // Add optimistic thread immediately
    startTransition(() => {
      addOptimisticThread(optimisticThread)
    })

    try {
      const result = await createThreadMutation.mutateAsync({
        categoryId: values.categoryId,
        title: values.title,
        content: values.content,
        attachments: values.attachments.map(({ fileName, contentType, dataBase64, size }) => ({
          fileName,
          contentType,
          dataBase64,
          size,
        })),
      })
      showSuccess(t('forum.create.success'))
      setComposerOpen(false)
      setPage(1)
      // React Query will automatically update the list, useOptimistic will sync
    } catch (error) {
      console.error(error)
      showError(t('forum.create.error'))
      // useOptimistic automatically rolls back on error
    }
  }

  const threads = optimisticThreads

  return (
    <div className="space-y-6">
      <header className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-gray-900 dark:text-gray-100">{t('forum.title')}</h1>
          <p className="text-sm text-gray-600 dark:text-gray-300">{t('forum.subtitle')}</p>
        </div>
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
          <div className="relative">
            <SearchIcon className="absolute left-3 top-2.5 h-4 w-4 text-gray-400" />
            <Input
              value={searchTerm}
              onChange={handleSearchChange}
              placeholder={t('forum.search.placeholder')}
              className="pl-9"
            />
            {isSearching && (
              <div className="absolute right-3 top-2.5">
                <div className="h-4 w-4 animate-spin rounded-full border-2 border-gray-300 border-t-primary-500" />
              </div>
            )}
          </div>
          <Button
            variant="primary"
            onClick={handleOpenComposer}
            disabled={createThreadMutation.isPending}
          >
            <Plus className="mr-2 h-4 w-4" />
            {t('forum.create.button')}
          </Button>
        </div>
      </header>

      <CategoryFilter
        categories={categoriesQuery.data ?? []}
        activeCategoryId={selectedCategory}
        onCategoryChange={handleCategoryChange}
      />

      <ThreadList
        threads={threads}
        isLoading={threadsQuery.isLoading}
        onRefresh={() => threadsQuery.refetch()}
        onThreadClick={(threadId) => navigate(`/forum/${threadId}`)}
        unreadThreadIds={unreadThreadIds}
        language={language}
        hasError={threadsQuery.isError}
      />

      <Pagination
        currentPage={threadsQuery.data?.page ?? 1}
        totalPages={threadsQuery.data?.totalPages ?? 1}
        onPageChange={setPage}
        isLoading={threadsQuery.isFetching}
      />

      <CreateThreadModal
        isOpen={isComposerOpen}
        onClose={handleCloseComposer}
        onSubmit={handleCreateThread}
        categories={categoriesQuery.data ?? []}
        isSubmitting={createThreadMutation.isPending}
        forumSettings={forumSettingsQuery.data}
      />
    </div>
  )
}

interface CategoryFilterProps {
  categories: ForumCategory[]
  activeCategoryId?: string
  onCategoryChange: (categoryId?: string) => void
}

function CategoryFilter({ categories, activeCategoryId, onCategoryChange }: CategoryFilterProps) {
  const { t } = useI18n()

  return (
    <div className="flex flex-wrap gap-2">
      <button
        type="button"
        onClick={() => onCategoryChange(undefined)}
        className={categoryButtonClasses(!activeCategoryId)}
      >
        {t('forum.categories.all')}
      </button>
      {categories.map((category) => (
        <button
          key={category.id}
          type="button"
          onClick={() => onCategoryChange(category.id)}
          className={categoryButtonClasses(activeCategoryId === category.id)}
        >
          {category.name}
        </button>
      ))}
    </div>
  )
}

function categoryButtonClasses(isActive: boolean): string {
  return [
    'rounded-full border px-4 py-1.5 text-sm transition-colors',
    isActive
      ? 'border-primary-500 bg-primary-50 text-primary-700 dark:border-primary-400 dark:bg-primary-900/40 dark:text-primary-200'
      : 'border-gray-200 bg-white text-gray-700 hover:border-primary-300 hover:text-primary-700 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-300 dark:hover:border-primary-500 dark:hover:text-primary-300',
  ].join(' ')
}

interface ThreadListProps {
  threads: ForumThreadSummary[]
  isLoading: boolean
  hasError: boolean
  onThreadClick: (threadId: string) => void
  onRefresh: () => void
  unreadThreadIds: Set<string>
  language: LanguageCode
}

function ThreadList({
  threads,
  isLoading,
  hasError,
  onThreadClick,
  onRefresh,
  unreadThreadIds,
  language,
}: ThreadListProps) {
  const { t } = useI18n()

  if (isLoading) {
    return (
      <Card className="flex min-h-[200px] items-center justify-center text-gray-500 dark:text-gray-300">
        {t('common.loading')}
      </Card>
    )
  }

  if (hasError) {
    return (
      <Card className="space-y-4 p-6 text-gray-700 dark:text-gray-200">
        <p>{t('forum.list.error')}</p>
        <Button variant="secondary" onClick={onRefresh}>
          <RefreshCcw className="mr-2 h-4 w-4" />
          {t('common.retry')}
        </Button>
      </Card>
    )
  }

  if (threads.length === 0) {
    return (
      <Card className="flex min-h-[200px] flex-col items-center justify-center gap-2 text-center text-gray-500 dark:text-gray-300">
        <h3 className="text-lg font-medium">{t('forum.list.emptyTitle')}</h3>
        <p className="text-sm">{t('forum.list.emptyDescription')}</p>
      </Card>
    )
  }

  return (
    <div className="space-y-3">
      {threads.map((thread) => {
        const hasUnread = unreadThreadIds.has(thread.id)
        return (
          <ThreadItem
            key={thread.id}
            thread={thread}
            language={language}
            hasUnread={hasUnread}
            onThreadClick={onThreadClick}
            t={t as (key: string, params?: Record<string, string>) => string}
          />
        )
      })}
    </div>
  )
}


interface PaginationProps {
  currentPage: number
  totalPages: number
  onPageChange: (page: number) => void
  isLoading: boolean
}

function Pagination({ currentPage, totalPages, onPageChange, isLoading }: PaginationProps) {
  const { t } = useI18n()

  if (totalPages <= 1) {
    return null
  }

  const handlePrevious = () => {
    if (currentPage > 1) {
      onPageChange(currentPage - 1)
    }
  }

  const handleNext = () => {
    if (currentPage < totalPages) {
      onPageChange(currentPage + 1)
    }
  }

  return (
    <div className="flex items-center justify-between gap-3 rounded-lg border border-gray-200 bg-white p-4 dark:border-gray-700 dark:bg-gray-800">
      <div className="text-sm text-gray-600 dark:text-gray-300">
        {t('forum.pagination.status', { page: String(currentPage), total: String(totalPages) })}
      </div>
      <div className="flex gap-2">
        <Button variant="secondary" onClick={handlePrevious} disabled={currentPage === 1 || isLoading}>
          {t('forum.pagination.previous')}
        </Button>
        <Button variant="secondary" onClick={handleNext} disabled={currentPage === totalPages || isLoading}>
          {t('forum.pagination.next')}
        </Button>
      </div>
    </div>
  )
}

interface CreateThreadFormState {
  categoryId: string
  title: string
  content: string
  attachments: AttachmentDraft[]
}

interface CreateThreadModalProps {
  isOpen: boolean
  onClose: () => void
  onSubmit: (values: CreateThreadFormState) => Promise<void>
  categories: ForumCategory[]
  isSubmitting: boolean
  forumSettings?: ForumSettings
}

function CreateThreadModal({ isOpen, onClose, onSubmit, categories, isSubmitting, forumSettings }: CreateThreadModalProps) {
  const { t } = useI18n()
  const { showError } = useToast()
  const [categoryId, setCategoryId] = useState<string>('')
  const [title, setTitle] = useState('')
  const [content, setContent] = useState('')
  const [attachments, setAttachments] = useState<AttachmentDraft[]>([])
  const attachmentsEnabled = forumSettings?.enableAttachments ?? true
  const maxAttachmentCount = forumSettings?.maxAttachmentCount ?? 5
  const maxAttachmentSizeMb = forumSettings?.maxAttachmentSizeMb ?? 5

  const resetForm = () => {
    setCategoryId('')
    setTitle('')
    setContent('')
    setAttachments([])
  }

  React.useEffect(() => {
    if (!attachmentsEnabled && attachments.length > 0) {
      setAttachments([])
    }
  }, [attachmentsEnabled, attachments])

  const handleClose = () => {
    if (!isSubmitting) {
      resetForm()
      onClose()
    }
  }

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (!categoryId) {
      showError(t('forum.create.validation.category'))
      return
    }

    if (!title.trim()) {
      showError(t('forum.create.validation.title'))
      return
    }

    if (!content.trim()) {
      showError(t('forum.create.validation.content'))
      return
    }

    await onSubmit({
      categoryId,
      title: title.trim(),
      content: content.trim(),
      attachments: attachmentsEnabled ? attachments : [],
    })
    resetForm()
  }

  return (
    <Modal isOpen={isOpen} onClose={handleClose} title={t('forum.create.modalTitle')} size="lg">
      <form onSubmit={handleSubmit} className="space-y-6 p-6">
        <div className="space-y-1">
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">
            {t('forum.create.fields.category')}
          </label>
          <select
            value={categoryId}
            onChange={(event) => setCategoryId(event.target.value)}
            className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100"
            disabled={isSubmitting}
            required
          >
            <option value="">{t('forum.create.fields.categoryPlaceholder')}</option>
            {categories.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </select>
        </div>

        <div className="space-y-1">
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">
            {t('forum.create.fields.title')}
          </label>
          <Input
            value={title}
            onChange={(event) => setTitle(event.target.value)}
            disabled={isSubmitting}
            maxLength={200}
            required
          />
        </div>

        <div className="space-y-1">
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">
            {t('forum.create.fields.content')}
          </label>
          <textarea
            value={content}
            onChange={(event) => setContent(event.target.value)}
            disabled={isSubmitting}
            rows={6}
            className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100"
            maxLength={4000}
            required
          />
        </div>

        {attachmentsEnabled ? (
          <AttachmentPicker
            attachments={attachments}
            onAttachmentsChange={setAttachments}
            disabled={isSubmitting}
            inputId="forum-thread-attachments"
            maxAttachments={maxAttachmentCount}
            maxAttachmentSizeMb={maxAttachmentSizeMb}
          />
        ) : (
          <p className="text-sm text-gray-500 dark:text-gray-400">{t('forum.attachments.disabled')}</p>
        )}

        <div className="flex justify-end gap-3">
          <Button type="button" variant="secondary" onClick={handleClose} disabled={isSubmitting}>
            {t('common.cancel')}
          </Button>
          <Button type="submit" variant="primary" disabled={isSubmitting}>
            {isSubmitting ? t('common.processing') : t('forum.create.submit')}
          </Button>
        </div>
      </form>
    </Modal>
  )
}

