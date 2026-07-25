import { Download, FileText } from 'lucide-react'
import { useState } from 'react'
import { downloadChatDocumentExport, getChatDocumentMarkdown } from '@/features/chat/services/chat.service'
import type { ChatDocumentSummary } from '@/features/chat/types/chat'
import { MarkdownMessage } from './MarkdownMessage'

export function OcrDocumentResult({ document, sessionId }: { document: ChatDocumentSummary; sessionId: string }) {
  const [isExpanded, setIsExpanded] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const [markdown, setMarkdown] = useState<string | null>(null)
  const [loadFailed, setLoadFailed] = useState(false)

  const handleToggleMarkdown = async () => {
    const nextExpandedState = !isExpanded
    setIsExpanded(nextExpandedState)
    if (!nextExpandedState || markdown != null || isLoading) {
      return
    }

    setIsLoading(true)
    setLoadFailed(false)
    try
    {
      setMarkdown(await getChatDocumentMarkdown(sessionId, document.id))
    }
    catch
    {
      setLoadFailed(true)
    }
    finally
    {
      setIsLoading(false)
    }
  }

  const handleDownload = async (format: 'txt' | 'docx') => {
    const blob = await downloadChatDocumentExport(sessionId, document, format)
    const objectUrl = URL.createObjectURL(blob)
    const anchor = window.document.createElement('a')
    anchor.href = objectUrl
    anchor.download = `${stripExtension(document.fileName)}.${format}`
    window.document.body.appendChild(anchor)
    anchor.click()
    anchor.remove()
    URL.revokeObjectURL(objectUrl)
  }

  return (
    <section className="mt-4 rounded-xl border border-indigo-200 bg-indigo-50/60 p-3 dark:border-indigo-900/70 dark:bg-indigo-950/20" data-testid="ocr-document-result">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex min-w-0 items-center gap-2 text-sm font-medium text-indigo-950 dark:text-indigo-100">
          <FileText className="h-4 w-4 shrink-0" aria-hidden="true" />
          <span className="truncate" title={document.fileName}>{document.fileName}</span>
          {document.pageCount != null && <span className="shrink-0 text-xs font-normal text-indigo-700 dark:text-indigo-300">{document.pageCount} str.</span>}
        </div>
        <div className="flex items-center gap-1">
          <button
            type="button"
            onClick={() => void handleToggleMarkdown()}
            className="rounded-md px-2 py-1 text-xs font-medium text-indigo-800 transition-colors hover:bg-indigo-100 dark:text-indigo-200 dark:hover:bg-indigo-900/60"
          >
            {isExpanded ? 'Ukryj Markdown' : 'Pokaż Markdown'}
          </button>
          <button
            type="button"
            onClick={() => void handleDownload('txt')}
            className="rounded-md p-1 text-indigo-800 transition-colors hover:bg-indigo-100 dark:text-indigo-200 dark:hover:bg-indigo-900/60"
            aria-label={`Pobierz ${document.fileName} jako TXT`}
            title="Pobierz TXT"
          >
            <Download className="h-4 w-4" aria-hidden="true" />
          </button>
          <button
            type="button"
            onClick={() => void handleDownload('docx')}
            className="rounded-md p-1 text-indigo-800 transition-colors hover:bg-indigo-100 dark:text-indigo-200 dark:hover:bg-indigo-900/60"
            aria-label={`Pobierz ${document.fileName} jako DOCX`}
            title="Pobierz DOCX"
          >
            <Download className="h-4 w-4" aria-hidden="true" />
          </button>
        </div>
      </div>
      {isExpanded && (
        <div className="mt-3 min-w-0 rounded-lg border border-indigo-100 bg-white p-3 dark:border-indigo-900/70 dark:bg-slate-950">
          {isLoading && <p className="text-sm text-indigo-800 dark:text-indigo-200">Wczytywanie Markdown…</p>}
          {loadFailed && <p className="text-sm text-red-700 dark:text-red-300">Nie udało się wczytać wyniku OCR.</p>}
          {markdown != null && <MarkdownMessage content={markdown} />}
        </div>
      )}
    </section>
  )
}

function stripExtension(fileName: string): string {
  const extensionIndex = fileName.lastIndexOf('.')
  return extensionIndex > 0 ? fileName.slice(0, extensionIndex) : fileName
}
