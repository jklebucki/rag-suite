import React from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter'
import { vscDarkPlus, vs } from 'react-syntax-highlighter/dist/esm/styles/prism'
import type { Components } from 'react-markdown'

interface MarkdownMessageProps {
  content: string
  isUserMessage?: boolean
}

export function MarkdownMessage({ content, isUserMessage = false }: MarkdownMessageProps) {
  const components: Components = {
    // Headings
    h1: ({ node, ...props }) => <h1 className="text-xl md:text-2xl font-bold mt-4 mb-2" {...props} />,
    h2: ({ node, ...props }) => <h2 className="text-lg md:text-xl font-bold mt-3 mb-2" {...props} />,
    h3: ({ node, ...props }) => <h3 className="text-base md:text-lg font-semibold mt-3 mb-1" {...props} />,
    h4: ({ node, ...props }) => <h4 className="text-sm md:text-base font-semibold mt-2 mb-1" {...props} />,
    h5: ({ node, ...props }) => <h5 className="text-sm font-semibold mt-2 mb-1" {...props} />,
    h6: ({ node, ...props }) => <h6 className="text-xs md:text-sm font-semibold mt-2 mb-1" {...props} />,

    // Paragraphs
    p: ({ node, ...props }) => <p className="mb-2 last:mb-0 leading-relaxed" {...props} />,

    // Lists
    ul: ({ node, ...props }) => <ul className="list-disc list-inside mb-2 space-y-1" {...props} />,
    ol: ({ node, ...props }) => <ol className="list-decimal list-inside mb-2 space-y-1" {...props} />,
    li: ({ node, ...props }) => <li className="ml-2" {...props} />,

    // Code blocks with syntax highlighting
    code: ({ node, className, children, ...props }) => {
      const match = /language-(\w+)/.exec(className || '')
      const language = match ? match[1] : ''
      const isInline = !className
      const codeString = String(children).replace(/\n$/, '')

      return isInline ? (
        <code
          className={`px-1.5 py-0.5 rounded text-xs md:text-sm font-mono ${
            isUserMessage
              ? 'bg-blue-600/30 text-blue-100'
              : 'bg-gray-200 text-blue-600'
          }`}
          {...props}
        >
          {children}
        </code>
      ) : (
        <div className="my-3 rounded-md overflow-hidden">
          {language && (
            <div
              className={`text-xs px-3 py-1.5 font-medium flex items-center justify-between ${
                isUserMessage ? 'bg-blue-600/40 text-blue-100' : 'bg-gray-700 text-gray-300'
              }`}
            >
              <span>{language.toUpperCase()}</span>
              <button
                onClick={() => {
                  navigator.clipboard.writeText(codeString)
                }}
                className="hover:bg-white/10 px-2 py-0.5 rounded text-[10px] transition-colors"
                title="Copy code"
              >
                Copy
              </button>
            </div>
          )}
          <SyntaxHighlighter
            language={language || 'text'}
            style={vscDarkPlus as any}
            customStyle={{
              margin: 0,
              borderRadius: language ? '0 0 0.375rem 0.375rem' : '0.375rem',
              fontSize: '0.75rem',
              lineHeight: '1.5',
            }}
            codeTagProps={{
              style: {
                fontSize: '0.75rem',
                fontFamily: 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace',
              },
            }}
            showLineNumbers={codeString.split('\n').length > 5}
            wrapLines={true}
            wrapLongLines={false}
          >
            {codeString}
          </SyntaxHighlighter>
        </div>
      )
    },

    // Links
    a: ({ node, ...props }) => (
      <a
        className={`underline hover:no-underline ${isUserMessage ? 'text-blue-100' : 'text-blue-600'}`}
        target="_blank"
        rel="noopener noreferrer"
        {...props}
      />
    ),

    // Blockquotes
    blockquote: ({ node, ...props }) => (
      <blockquote
        className={`border-l-4 pl-4 py-1 my-2 italic ${
          isUserMessage ? 'border-blue-300 text-blue-100' : 'border-gray-300 text-gray-600'
        }`}
        {...props}
      />
    ),

    // Tables
    table: ({ node, ...props }) => (
      <div className="overflow-x-auto my-3">
        <table className="min-w-full border-collapse border border-gray-300" {...props} />
      </div>
    ),
    th: ({ node, ...props }) => (
      <th
        className={`border border-gray-300 px-3 py-2 text-left font-semibold ${
          isUserMessage ? 'bg-blue-600/20' : 'bg-gray-100'
        }`}
        {...props}
      />
    ),
    td: ({ node, ...props }) => <td className="border border-gray-300 px-3 py-2" {...props} />,

    // Horizontal rule
    hr: ({ node, ...props }) => (
      <hr className={`my-4 ${isUserMessage ? 'border-blue-300' : 'border-gray-300'}`} {...props} />
    ),

    // Strong/Bold
    strong: ({ node, ...props }) => <strong className="font-bold" {...props} />,

    // Emphasis/Italic
    em: ({ node, ...props }) => <em className="italic" {...props} />,
  }

  return (
    <div
      className={`prose prose-sm md:prose-base max-w-none ${
        isUserMessage
          ? 'prose-invert prose-headings:text-white prose-p:text-white prose-strong:text-white prose-code:text-blue-100 prose-pre:bg-blue-600/20'
          : 'prose-headings:text-gray-900 prose-p:text-gray-700 prose-code:text-blue-600 prose-pre:bg-gray-800'
      }`}
    >
      <ReactMarkdown remarkPlugins={[remarkGfm]} components={components}>
        {content}
      </ReactMarkdown>
    </div>
  )
}
