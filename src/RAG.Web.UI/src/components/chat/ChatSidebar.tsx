import React from 'react'
import { Plus, Trash2 } from 'lucide-react'
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
  return (
    <div className="w-80 border-r border-gray-200 flex flex-col">
      <div className="p-4 border-b border-gray-200">
        <button
          onClick={onNewSession}
          disabled={isCreatingSession}
          className="w-full btn-primary flex items-center justify-center gap-2"
        >
          <Plus className="h-4 w-4" />
          New Chat
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
                  {session.title || 'New Chat'}
                </p>
                <p className="text-xs text-gray-500">
                  {new Date(session.updatedAt).toLocaleDateString()}
                </p>
              </div>
              <button
                onClick={(e) => {
                  e.stopPropagation()
                  onDeleteSession(session.id)
                }}
                className="opacity-0 group-hover:opacity-100 p-1 rounded hover:bg-red-100 text-red-600 transition-opacity"
                title="Delete chat session"
              >
                <Trash2 className="h-4 w-4" />
              </button>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
