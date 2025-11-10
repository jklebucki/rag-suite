import React, { useEffect, useMemo, useState } from 'react'
import { Loader2, MessageSquare, AlertCircle, ChevronDown, Image as ImageIcon, FileText } from 'lucide-react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useI18n } from '@/shared/contexts/I18nContext'
import { useToast } from '@/shared/contexts/ToastContext'
import feedbackService from '@/features/feedback/services/feedback.service'
import type { FeedbackItem } from '@/features/feedback/types/feedback'
import { Modal } from '@/shared/components/ui/Modal'

interface MyFeedbackModalProps {
  isOpen: boolean
  onClose: () => void
}

export function MyFeedbackModal({ isOpen, onClose }: MyFeedbackModalProps) {
  const { t } = useI18n()
  const { showError } = useToast()
  const queryClient = useQueryClient()
  const [expandedIds, setExpandedIds] = useState<Set<string>>(new Set())

  const {
    data: feedback = [],
    isLoading,
    isError,
    refetch
  } = useQuery({
    queryKey: ['my-feedback'],
    queryFn: feedbackService.getMyFeedback,
    enabled: isOpen,
    initialData: () => queryClient.getQueryData<FeedbackItem[]>(['my-feedback']) ?? [],
    staleTime: 60_000,
    refetchOnWindowFocus: false
  })

  useEffect(() => {
    if (isOpen) {
      void refetch()
    } else {
      setExpandedIds(new Set())
    }
  }, [isOpen, refetch])

  const acknowledgeMutation = useMutation({
    mutationFn: (id: string) => feedbackService.acknowledgeFeedbackResponse(id),
    onSuccess: (updated) => {
      queryClient.setQueryData<FeedbackItem[]>(['my-feedback'], (prev = []) =>
        prev.map((item) => (item.id === updated.id ? updated : item))
      )
    },
    onError: () => {
      showError(t('feedback.my.error_acknowledge'))
    }
  })

  const handleToggle = (item: FeedbackItem) => {
    setExpandedIds((prev) => {
      const next = new Set(prev)
      if (next.has(item.id)) {
        next.delete(item.id)
      } else {
        next.add(item.id)
        if (item.respondedAt && !item.responseViewedAt) {
          acknowledgeMutation.mutate(item.id)
        }
      }
      return next
    })
  }

  const hasItems = feedback.length > 0

  const content = useMemo(() => {
    if (isLoading) {
      return (
        <div className="flex flex-col items-center justify-center py-16 text-center text-gray-600 dark:text-gray-300">
          <Loader2 className="h-8 w-8 animate-spin text-primary-500 mb-3" />
          <p>{t('feedback.my.loading')}</p>
        </div>
      )
    }

    if (isError) {
      return (
        <div className="flex flex-col items-center justify-center py-16 text-center text-red-600 dark:text-red-400">
          <AlertCircle className="h-10 w-10 mb-3" />
          <p>{t('feedback.my.error')}</p>
        </div>
      )
    }

    if (!hasItems) {
      return (
        <div className="flex flex-col items-center justify-center py-16 text-center text-gray-600 dark:text-gray-300 space-y-3">
          <MessageSquare className="h-10 w-10 text-primary-500" />
          <div>
            <p className="text-lg font-semibold text-gray-900 dark:text-gray-100">{t('feedback.my.empty.title')}</p>
            <p className="text-sm text-gray-600 dark:text-gray-300 mt-1">{t('feedback.my.empty.description')}</p>
          </div>
        </div>
      )
    }

    return (
      <div className="space-y-4">
        {feedback.map((item) => {
          const isExpanded = expandedIds.has(item.id)
          const isResponded = Boolean(item.respondedAt)
          const isUnread = isResponded && !item.responseViewedAt
          const createdLabel = t('feedback.my.submitted_at', { date: formatDate(item.createdAt) })
          const respondedLabel = item.respondedAt ? t('feedback.my.responded_at', { date: formatDate(item.respondedAt) }) : null

          return (
            <div
              key={item.id}
              className={`rounded-xl border p-4 transition-colors ${
                isUnread
                  ? 'border-primary-300 bg-primary-50/60 dark:border-primary-700 dark:bg-primary-900/20'
                  : 'border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800/70'
              }`}
            >
              <button
                onClick={() => handleToggle(item)}
                className="w-full flex items-center justify-between gap-3 text-left"
              >
                <div className="space-y-1">
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-semibold text-gray-900 dark:text-gray-100">
                      {item.subject}
                    </span>
                    <span
                      className={`inline-flex items-center gap-1 px-2 py-0.5 text-xs font-medium rounded-full ${
                        isResponded
                          ? 'bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-300'
                          : 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300'
                      }`}
                    >
                      {isResponded ? t('feedback.my.status.responded') : t('feedback.my.status.pending')}
                    </span>
                    {isUnread && (
                      <span className="inline-flex items-center px-2 py-0.5 text-xs font-semibold rounded-full bg-primary-500 text-white">
                        {t('feedback.my.status.unread_badge')}
                      </span>
                    )}
                  </div>
                  <div className="text-xs text-gray-500 dark:text-gray-400">
                    {createdLabel}
                    {respondedLabel && (
                      <span className="ml-2 text-gray-500 dark:text-gray-400">{respondedLabel}</span>
                    )}
                  </div>
                </div>
                <ChevronDown
                  className={`h-4 w-4 text-gray-500 dark:text-gray-400 transition-transform ${isExpanded ? 'rotate-180' : ''}`}
                />
              </button>

              {isExpanded && (
                <div className="mt-4 space-y-4 text-sm text-gray-700 dark:text-gray-200">
                  <div>
                    <p className="font-medium text-gray-900 dark:text-gray-100 mb-1">
                      {t('feedback.my.message_label')}
                    </p>
                    <div className="rounded-lg border border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900/60 p-3 whitespace-pre-wrap">
                      {item.message}
                    </div>
                  </div>

                  {item.attachments.length > 0 && (
                    <div>
                      <p className="font-medium text-gray-900 dark:text-gray-100 mb-2">
                        {t('feedback.my.attachments')}
                      </p>
                      <div className="grid gap-3 sm:grid-cols-2">
                        {item.attachments.map((attachment) => {
                          const dataUrl = `data:${attachment.contentType};base64,${attachment.dataBase64}`
                          const isImage = attachment.contentType.startsWith('image/')

                          return (
                            <div
                              key={attachment.id}
                              className="rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900/80 p-3 space-y-2"
                            >
                              <div className="flex items-center gap-2 text-xs text-gray-600 dark:text-gray-300 break-all">
                                {isImage ? <ImageIcon className="h-4 w-4" /> : <FileText className="h-4 w-4" />}
                                <span>{attachment.fileName}</span>
                              </div>
                              {isImage && (
                                <img
                                  src={dataUrl}
                                  alt={attachment.fileName}
                                  className="max-h-64 w-full rounded-md object-contain border border-gray-200 dark:border-gray-700"
                                />
                              )}
                              <a
                                href={dataUrl}
                                download={attachment.fileName}
                                className="inline-flex items-center gap-1 text-xs font-medium text-primary-600 hover:text-primary-500 dark:text-primary-300 dark:hover:text-primary-200"
                              >
                                {t('feedback.my.download')}
                              </a>
                            </div>
                          )
                        })}
                      </div>
                    </div>
                  )}

                  {isResponded && item.response && (
                    <div>
                      <p className="font-medium text-gray-900 dark:text-gray-100 mb-1">
                        {t('feedback.my.response_label')}
                      </p>
                      <div className="rounded-lg border border-blue-200 dark:border-blue-900/40 bg-blue-50 dark:bg-blue-900/20 p-3 whitespace-pre-wrap text-blue-900 dark:text-blue-100">
                        {item.response}
                      </div>
                    </div>
                  )}
                </div>
              )}
            </div>
          )
        })}
      </div>
    )
  }, [expandedIds, feedback, hasItems, isError, isLoading, t])

  if (!isOpen) {
    return null
  }

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={
        <div className="flex flex-col gap-1">
          <h3 className="text-lg sm:text-xl font-semibold text-gray-900 dark:text-gray-100">
            {t('feedback.my.title')}
          </h3>
          <p className="text-sm text-gray-600 dark:text-gray-300">
            {t('feedback.my.subtitle')}
          </p>
        </div>
      }
      size="xl"
    >
      <div className="p-6">
        {content}
      </div>
    </Modal>
  )
}

function formatDate(date: string) {
  return new Date(date).toLocaleString()
}

