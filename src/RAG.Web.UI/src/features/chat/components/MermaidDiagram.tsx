import { useEffect, useId, useState } from 'react'
import { Modal } from '@/shared/components/ui/Modal'

interface MermaidDiagramProps {
  chart: string
}

export function MermaidDiagram({ chart }: MermaidDiagramProps) {
  const reactId = useId()
  const [svg, setSvg] = useState('')
  const [error, setError] = useState(false)
  const [isExpanded, setIsExpanded] = useState(false)

  useEffect(() => {
    let cancelled = false

    async function renderDiagram() {
      try {
        setError(false)

        const { default: mermaid } = await import('mermaid')
        const isDark = document.documentElement.classList.contains('dark')

        mermaid.initialize({
          startOnLoad: false,
          securityLevel: 'strict',
          theme: isDark ? 'dark' : 'default',
          suppressErrorRendering: true,
          flowchart: {
            htmlLabels: false,
          },
        })

        const id = `mermaid-${reactId.replace(/:/g, '')}`
        const result = await mermaid.render(id, chart)

        if (!cancelled) {
          setSvg(result.svg)
        }
      } catch {
        if (!cancelled) {
          setSvg('')
          setError(true)
        }
      }
    }

    void renderDiagram()

    return () => {
      cancelled = true
    }
  }, [chart, reactId])

  if (error) {
    return (
      <div className="my-3 overflow-hidden rounded-md border border-red-200 dark:border-red-900">
        <div className="bg-red-50 px-3 py-2 text-xs text-red-700 dark:bg-red-950/40 dark:text-red-300">
          Unable to render Mermaid diagram.
        </div>
        <pre className="m-0 overflow-x-auto bg-gray-900 p-3 text-xs text-gray-100">
          <code>{chart}</code>
        </pre>
      </div>
    )
  }

  if (!svg) {
    return (
      <div className="my-3 rounded-md border border-gray-200 px-3 py-6 text-center text-xs text-gray-500 dark:border-slate-700 dark:text-slate-400">
        Rendering diagram…
      </div>
    )
  }

  const diagramClasses = 'w-full overflow-auto [&_svg]:!h-auto [&_svg]:!w-full [&_svg]:!max-w-none'

  return (
    <>
      <button
        type="button"
        className="my-3 block w-full cursor-zoom-in rounded-md border border-gray-200 bg-white p-3 text-left transition-shadow hover:shadow-md focus:outline-none focus:ring-2 focus:ring-primary-500 dark:border-slate-700 dark:bg-slate-950"
        onClick={() => setIsExpanded(true)}
        aria-label="Open Mermaid diagram in a larger view"
      >
        <div
          className={diagramClasses}
          role="img"
          aria-label="Mermaid diagram"
          // Mermaid returns sanitized SVG when securityLevel is set to strict.
          dangerouslySetInnerHTML={{ __html: svg }}
        />
      </button>

      <Modal
        isOpen={isExpanded}
        onClose={() => setIsExpanded(false)}
        title="Mermaid diagram"
        size="screen"
      >
        <div className="flex h-full w-full items-center justify-center overflow-auto bg-white p-4 dark:bg-slate-950 sm:p-6">
          <div
            className={diagramClasses}
            role="img"
            aria-label="Expanded Mermaid diagram"
            // Mermaid returns sanitized SVG when securityLevel is set to strict.
            dangerouslySetInnerHTML={{ __html: svg }}
          />
        </div>
      </Modal>
    </>
  )
}
