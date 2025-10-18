import React, { useEffect, useState } from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import { Info } from 'lucide-react'
import { useI18n } from '@/contexts/I18nContext'

export default function About() {
  const { language } = useI18n()
  const [content, setContent] = useState<string>('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const loadContent = async () => {
      try {
        setLoading(true)
        setError(null)

        const response = await fetch(`/assets/about/about.${language}.md`)
        if (!response.ok) {
          // Fallback to English if the language file doesn't exist
          const fallbackResponse = await fetch('/assets/about/about.en.md')
          if (!fallbackResponse.ok) {
            throw new Error('Failed to load about content')
          }
          const text = await fallbackResponse.text()
          setContent(text)
        } else {
          const text = await response.text()
          setContent(text)
        }
      } catch (err) {
        console.error('Failed to load about content:', err)
        setError('Failed to load application information')
      } finally {
        setLoading(false)
      }
    }

    loadContent()
  }, [language])

  if (loading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center space-x-3">
          <div className="p-2 bg-blue-100 rounded-lg">
            <Info className="h-6 w-6 text-blue-600" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-gray-900">App Info</h1>
            <p className="text-gray-600">Loading application information...</p>
          </div>
        </div>
        <div className="flex items-center justify-center p-8">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="space-y-6">
        <div className="flex items-center space-x-3">
          <div className="p-2 bg-red-100 rounded-lg">
            <Info className="h-6 w-6 text-red-600" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-gray-900">App Info</h1>
            <p className="text-gray-600">Application information</p>
          </div>
        </div>
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <p className="text-red-800">{error}</p>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center space-x-3">
        <div className="p-2 bg-blue-100 rounded-lg">
          <Info className="h-6 w-6 text-blue-600" />
        </div>
        <div>
          <h1 className="text-2xl font-bold text-gray-900">App Info</h1>
          <p className="text-gray-600">Application information</p>
        </div>
      </div>

      <div className="bg-white shadow rounded-lg p-6">
        <div className="prose prose-gray max-w-none">
          <ReactMarkdown
            remarkPlugins={[remarkGfm]}
            components={{
              h1: ({ children }) => (
                <h1 className="text-3xl font-bold text-gray-900 mb-4">{children}</h1>
              ),
              h2: ({ children }) => (
                <h2 className="text-2xl font-semibold text-gray-900 mt-8 mb-4">{children}</h2>
              ),
              h3: ({ children }) => (
                <h3 className="text-xl font-medium text-gray-900 mt-6 mb-3">{children}</h3>
              ),
              p: ({ children }) => (
                <p className="text-gray-700 mb-4 leading-relaxed">{children}</p>
              ),
              ul: ({ children }) => (
                <ul className="list-disc list-inside text-gray-700 mb-4 space-y-1">{children}</ul>
              ),
              ol: ({ children }) => (
                <ol className="list-decimal list-inside text-gray-700 mb-4 space-y-1">{children}</ol>
              ),
              a: ({ href, children }) => (
                <a
                  href={href}
                  className="text-blue-600 hover:text-blue-800 underline"
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  {children}
                </a>
              ),
              code: ({ children }) => (
                <code className="bg-gray-100 px-2 py-1 rounded text-sm font-mono text-gray-800">
                  {children}
                </code>
              ),
              pre: ({ children }) => (
                <pre className="bg-gray-100 p-4 rounded-lg overflow-x-auto text-sm font-mono text-gray-800 mb-4">
                  {children}
                </pre>
              ),
              blockquote: ({ children }) => (
                <blockquote className="border-l-4 border-gray-300 pl-4 italic text-gray-600 mb-4">
                  {children}
                </blockquote>
              ),
            }}
          >
            {content}
          </ReactMarkdown>
        </div>
      </div>
    </div>
  )
}
