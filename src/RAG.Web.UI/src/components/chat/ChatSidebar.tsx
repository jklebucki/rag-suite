import React, { useState, useRef, useEffect } from 'react'
import { Plus, Trash2, Menu } from 'lucide-react'
import { useI18n } from '@/contexts/I18nContext'
import { formatDateTime } from '@/utils/date'
import type { ChatSession } from '@/types'

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
      <div className="md:hidden bg-white border-b border-gray-200">
        <div className="flex items-center justify-between p-3">
          <button
            onClick={onNewSession}
            disabled={isCreatingSession}
            className="p-2 rounded-md hover:bg-gray-100 active:bg-gray-200 transition-colors shrink-0"
            aria-label={t('chat.new_session')}
            title={t('chat.new_session')}
          >
            <Plus className="h-5 w-5 text-gray-700" />
          </button>

          <div className="relative" ref={menuRef}>
            <button
              onClick={() => setIsMenuOpen(!isMenuOpen)}
              className="p-2 rounded-md hover:bg-gray-100 active:bg-gray-200 transition-colors"
              aria-label="Sessions menu"
              title="Sessions menu"
            >
              <Menu className="h-5 w-5 text-gray-700" />
            </button>

            {isMenuOpen && (
              <div className="absolute right-0 top-full mt-1 w-64 bg-white border border-gray-200 rounded-lg shadow-lg z-50 max-h-80 overflow-y-auto">
                <div className="p-2">
                  {sessions.length > 0 ? (
                    sessions.map((session) => (
                      <div
                        key={session.id}
                        className={`group flex items-center justify-between p-3 rounded-lg cursor-pointer transition-colors ${
                          currentSessionId === session.id
                            ? 'bg-primary-50 border border-primary-200'
                            : 'hover:bg-gray-50'
                        }`}
                        onClick={() => {
                          onSelectSession(session.id)
                          setIsMenuOpen(false)
                        }}
                      >
                        <div className="flex-1 min-w-0">
                          <p className="text-sm font-medium text-gray-900 truncate">
                            {isDefaultTitle(session.title) 
                              ? t('chat.new_conversation')
                              : session.title
                            }
                          </p>
                          <p className="text-xs text-gray-500">
                            {formatDateTime(session.updatedAt, language)}
                          </p>
                        </div>
                        <button
                          onClick={(e) => {
                            e.stopPropagation()
                            onDeleteSession(session.id)
                          }}
                          className="p-1 rounded hover:bg-red-100 text-red-600 transition-colors"
                          title={t('common.delete')}
                        >
                          <Trash2 className="h-4 w-4" />
                        </button>
                      </div>
                    ))
                  ) : (
                    <div className="p-3 text-center">
                      <span className="text-sm text-gray-500">
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
      <div className="hidden md:flex w-80 border-r border-gray-200 flex-col">
        <div className="p-4 border-b border-gray-200">
          <button
            onClick={onNewSession}
            disabled={isCreatingSession}
            className="w-full btn-primary flex items-center justify-center gap-2"
          >
            <Plus className="h-4 w-4" />
            {t('chat.new_session')}
          </button>
        </div>

        <div className="flex-1 overflow-y-auto">
          <div className="p-4 space-y-2">
            {sessions.map((session) => (
              <div
                key={session.id}
                className={`group flex items-center justify-between p-3 rounded-lg cursor-pointer transition-colors ${
                  currentSessionId === session.id
                    ? 'bg-primary-50 border border-primary-200'
                    : 'hover:bg-gray-50'
                }`}
                onClick={() => onSelectSession(session.id)}
              >
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-gray-900 truncate">
                    {isDefaultTitle(session.title) 
                      ? t('chat.new_conversation')
                      : session.title
                    }
                  </p>
                  <p className="text-xs text-gray-500">
                    {formatDateTime(session.updatedAt, language)}
                  </p>
                </div>
                <button
                  onClick={(e) => {
                    e.stopPropagation()
                    onDeleteSession(session.id)
                  }}
                  className="opacity-0 group-hover:opacity-100 p-1 rounded hover:bg-red-100 text-red-600 transition-opacity"
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
