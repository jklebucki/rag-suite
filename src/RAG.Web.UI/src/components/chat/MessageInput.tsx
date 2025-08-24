import React from 'react'
import { Send } from 'lucide-react'

interface MessageInputProps {
  message: string
  onMessageChange: (message: string) => void
  onSendMessage: (e: React.FormEvent) => void
  isSending: boolean
}

export function MessageInput({
  message,
  onMessageChange,
  onSendMessage,
  isSending,
}: MessageInputProps) {
  return (
    <div className="border-t border-gray-200 p-4">
      <form onSubmit={onSendMessage} className="flex gap-3">
        <input
          type="text"
          value={message}
          onChange={(e) => onMessageChange(e.target.value)}
          placeholder="Ask me anything about your knowledge base..."
          className="flex-1 border border-gray-300 rounded-lg px-4 py-3 focus:ring-2 focus:ring-primary-500 focus:border-transparent"
          disabled={isSending}
        />
        <button
          type="submit"
          disabled={!message.trim() || isSending}
          className="btn-primary disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
        >
          <Send className="h-4 w-4" />
          Send
        </button>
      </form>
    </div>
  )
}
