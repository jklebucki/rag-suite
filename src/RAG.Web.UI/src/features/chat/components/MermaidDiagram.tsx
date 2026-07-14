import { useEffect, useId, useState } from 'react'

interface MermaidDiagramProps {
  chart: string
}

export function MermaidDiagram({ chart }: MermaidDiagramProps) {
  const reactId = useId()
  const [svg, setSvg] = useState('')
  const [error, setError] = useState(false)

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

  return (
    <div
      className="my-3 overflow-x-auto rounded-md border border-gray-200 bg-white p-3 dark:border-slate-700 dark:bg-slate-950"
      role="img"
      aria-label="Mermaid diagram"
      // Mermaid returns sanitized SVG when securityLevel is set to strict.
      dangerouslySetInnerHTML={{ __html: svg }}
    />
  )
}
