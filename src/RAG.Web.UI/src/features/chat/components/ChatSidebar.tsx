import React, { useState, useRef, useEffect } from 'react'
import { Plus, Trash2, Menu } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import { formatDateTime } from '@/utils/date'
import type { ChatSession } from '@/features/chat/types/chat'

interface ChatSidebarProps {
  sessions: ChatSession[]
  currentSessionId: string | null
  onNewSession: () => void
  onSelectSession: (sessionId: string) => void
  onDeleteSession: (sessionId: string) => void
  isCreatingSession: boolean
}

export function ChatSidebar({
  sessions,
  currentSessionId,
  onNewSession,
  onSelectSession,
  onDeleteSession,
  isCreatingSession,
}: ChatSidebarProps) {
  const { t, language } = useI18n()
  const [isMenuOpen, setIsMenuOpen] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)

  // Close menu when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setIsMenuOpen(false)
      }
    }

    if (isMenuOpen) {
      document.addEventListener('mousedown', handleClickOutside)
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [isMenuOpen])

  // Helper function to check if title is a default conversation title
  const isDefaultTitle = (title: string | null | undefined): boolean => {
    if (!title) return true
    // Check for common default titles that should be localized
    const defaultTitles = [
      'New Conversation',
      'Nowa rozmowa',
      'Conversație nouă',
      'Új beszélgetés',
      'Nieuw gesprek'
    ]
    return defaultTitles.includes(title)
  }

  return (
    <>
      {/* Mobile topbar: hamburger menu with sessions list */}
      <div className="md:hidden bg-white dark:bg-slate-900 border-b border-gray-200 dark:border-slate-800 transition-colors">
        <div className="flex items-center justify-between p-3">
          <button
            onClick={onNewSession}
            disabled={isCreatingSession}
            className="p-2 rounded-md hover:bg-gray-100 active:bg-gray-200 dark:hover:bg-slate-800 dark:active:bg-slate-700 transition-colors shrink-0 disabled:opacity-50"
            aria-label={t('chat.new_session')}
            title={t('chat.new_session')}
          >
            <Plus className="h-5 w-5 text-gray-700 dark:text-gray-200" />
          </button>

          <div className="relative" ref={menuRef}>
            <button
              onClick={() => setIsMenuOpen(!isMenuOpen)}
              className="p-2 rounded-md hover:bg-gray-100 active:bg-gray-200 dark:hover:bg-slate-800 dark:active:bg-slate-700 transition-colors"
              aria-label="Sessions menu"
              title="Sessions menu"
            >
              <Menu className="h-5 w-5 text-gray-700 dark:text-gray-200" />
            </button>

            {isMenuOpen && (
              <div className="absolute right-0 top-full mt-1 w-64 bg-white dark:bg-slate-900 border border-gray-200 dark:border-slate-800 rounded-xl shadow-lg z-50 max-h-80 overflow-y-auto">
                <div className="p-2">
                  {sessions.length > 0 ? (
                    sessions.map((session) => (
                      <div
                        key={session.id}
                        role="button"
                        tabIndex={0}
                        className={`group flex items-center justify-between p-3 rounded-lg cursor-pointer transition-colors w-full text-left ${
                          currentSessionId === session.id
                            ? 'bg-primary-50 border border-primary-200 dark:bg-primary-900/20 dark:border-primary-500/50'
                            : 'hover:bg-gray-50 dark:hover:bg-slate-800 border border-transparent'
                        }`}
                        onClick={() => {
                          onSelectSession(session.id)
                          setIsMenuOpen(false)
                        }}
                        onKeyDown={(e) => {
                          if (e.key === 'Enter' || e.key === ' ') {
                            e.preventDefault()
                            onSelectSession(session.id)
                            setIsMenuOpen(false)
                          }
                        }}
                      >
                        <div className="flex-1 min-w-0">
                          <p className="text-sm font-medium text-gray-900 dark:text-gray-100 truncate"
                            title={isDefaultTitle(session.title)
                              ? t('chat.new_conversation')
                              : session.title
                            }
                          >
                            {isDefaultTitle(session.title)
                              ? t('chat.new_conversation')
                              : session.title
                            }
                          </p>
                          <p className="text-xs text-gray-500 dark:text-gray-400">
                            {formatDateTime(session.updatedAt, language)}
                          </p>
                        </div>
                        <button
                          onClick={(e) => {
                            e.stopPropagation()
                            onDeleteSession(session.id)
                          }}
                          className="p-1 rounded hover:bg-red-100 dark:hover:bg-red-900/30 text-red-600 dark:text-red-400 transition-colors"
                          title={t('common.delete')}
                        >
                          <Trash2 className="h-4 w-4" />
                        </button>
                      </div>
                    ))
                  ) : (
                    <div className="p-3 text-center">
                      <span className="text-sm text-gray-500 dark:text-gray-400">
                        {t('chat.no_sessions')}
                      </span>
                    </div>
                  )}
                </div>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Desktop / tablet vertical sidebar */}
      <div className="hidden md:flex w-80 border-r border-gray-200 dark:border-slate-800 flex-col bg-white dark:bg-slate-950 transition-colors">
        <div className="p-4 border-b border-gray-200 dark:border-slate-800">
          <button
            onClick={onNewSession}
            disabled={isCreatingSession}
            className="w-full btn-primary flex items-center justify-center gap-2 disabled:opacity-50"
          >
            <Plus className="h-4 w-4" />
            {t('chat.new_session')}
          </button>
        </div>

        <div className="flex-1 overflow-y-auto scrollbar-hide">
          <div className="p-4 space-y-2">
            {sessions.map((session) => (
              <div
                key={session.id}
                role="button"
                tabIndex={0}
                className={`group flex items-center justify-between p-3 rounded-lg cursor-pointer transition-colors w-full text-left border ${
                  currentSessionId === session.id
                    ? 'bg-primary-50 border-primary-200 dark:bg-primary-900/20 dark:border-primary-500/50'
                    : 'border-transparent hover:bg-gray-50 dark:hover:bg-slate-900 dark:border-slate-900'
                }`}
                onClick={() => onSelectSession(session.id)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault()
                    onSelectSession(session.id)
                  }
                }}
              >
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">
                    {isDefaultTitle(session.title)
                      ? t('chat.new_conversation')
                      : session.title
                    }
                  </p>
                  <p className="text-xs text-gray-500 dark:text-gray-400">
                    {formatDateTime(session.updatedAt, language)}
                  </p>
                </div>
                <button
                  onClick={(e) => {
                    e.stopPropagation()
                    onDeleteSession(session.id)
                  }}
                  className="opacity-0 group-hover:opacity-100 p-1 rounded hover:bg-red-100 dark:hover:bg-red-900/30 text-red-600 dark:text-red-400 transition-opacity"
                  title={t('common.delete')}
                >
                  <Trash2 className="h-4 w-4" />
                </button>
              </div>
            ))}
          </div>
        </div>
      </div>
    </>
  )
}
