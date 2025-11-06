import React, { useEffect, useState, useMemo } from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import {
  Info, Brain, Search, BarChart3, Rocket, CheckCircle2, Star, Users,
  Activity
} from 'lucide-react'
import { useI18n } from '@/contexts/I18nContext'
import {
  getSectionContentById,
  getSectionTitleById,
  getSubtitleById,
  getSection,
  getTop,
  getSubsections
} from '@/utils/markdownParser'
import { logger } from '@/utils/logger'

export function About() {
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
        logger.error('Failed to load about content:', err)
        setError('Failed to load application information')
      } finally {
        setLoading(false)
      }
    }
    loadContent()
  }, [language])

  const { top, sections } = useMemo(() => {
    const topData = getTop(content)
    const h1 = topData.h1
    const tagline = topData.tagline
    
    return {
      top: { h1, tagline },
      sections: {
        aboutApp: {
          title: getSectionTitleById(content, 'about-app') || 'O APLIKACJI',
          content: getSectionContentById(content, 'about-app') || getSection(content, 'O APLIKACJI'),
          subtitle: getSubtitleById(content, 'subtitle-about-app') || ''
        },
        aboutProject: {
          title: getSectionTitleById(content, 'about-project') || 'O PROJEKCIE',
          content: getSectionContentById(content, 'about-project') || getSection(content, 'O PROJEKCIE'),
          subtitle: getSubtitleById(content, 'subtitle-about-project') || ''
        },
        keyFeatures: {
          title: getSectionTitleById(content, 'key-features') || 'KLUCZOWE FUNKCJE',
          content: getSectionContentById(content, 'key-features') || getSection(content, 'KLUCZOWE FUNKCJE'),
          subtitle: getSubtitleById(content, 'subtitle-key-features') || 'Funkcje, które usprawniają pracę, automatyzują procesy i centralizują wiedzę w jednym miejscu'
        },
        roadmap: {
          title: getSectionTitleById(content, 'roadmap') || 'ROADMAP',
          content: getSectionContentById(content, 'roadmap') || getSection(content, 'ROADMAP'),
          subtitle: getSubtitleById(content, 'subtitle-roadmap') || 'Strategiczna ścieżka rozwoju funkcji AI, bezpieczeństwa i analizy wiedzy w organizacji'
        },
        benefits: {
          title: getSectionTitleById(content, 'benefits') || 'BENEFITY DLA FIRMY',
          content: getSectionContentById(content, 'benefits') || getSection(content, 'BENEFITY DLA FIRMY'),
          subtitle: getSubtitleById(content, 'subtitle-benefits') || 'AI, która porządkuje wiedzę, skraca procesy i wzmacnia efektywność całej organizacji'
        },
        authors: {
          title: getSectionTitleById(content, 'authors') || 'AUTORZY PROJEKTU',
          content: getSectionContentById(content, 'authors') || getSection(content, 'AUTORZY PROJEKTU'),
          subtitle: getSubtitleById(content, 'subtitle-authors') || 'Osoby odpowiedzialne za rozwój i utrzymanie RAG Suite'
        }
      }
    }
  }, [content])

  const featureCards = useMemo(() => getSubsections(sections.keyFeatures.content), [sections.keyFeatures.content])
  const roadmapCols = useMemo(() => getSubsections(sections.roadmap.content), [sections.roadmap.content])

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
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500" />
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
      {/* HERO */}
      <div className="bg-white shadow rounded-xl p-6">
        <div className="flex items-center gap-2 text-sm">
          <span className="inline-flex items-center gap-2 px-3 py-1 rounded-full border">
            <Info className="h-4 w-4" />
            <span>{getSubtitleById(content, 'subtitle-hero-badge') || 'Twój inteligentny asystent AI Jan'}</span>
          </span>
        </div>
        <h1 className="text-3xl font-bold text-gray-900 mt-4">{top.h1}</h1>
        {top.tagline && <p className="text-gray-700 mt-2">{top.tagline}</p>}
      </div>

      {/* O APLIKACJI / O PROJEKCIE */}
      <section className="grid md:grid-cols-2 gap-4">
        <Card title={sections.aboutApp.title} subtitle={sections.aboutApp.subtitle}>
          <Markdown>{sections.aboutApp.content}</Markdown>
        </Card>
        <Card title={sections.aboutProject.title} subtitle={sections.aboutProject.subtitle}>
          <Markdown>{sections.aboutProject.content}</Markdown>
        </Card>
      </section>

      {/* KLUCZOWE FUNKCJE */}
      <Card title={sections.keyFeatures.title} subtitle={sections.keyFeatures.subtitle}>
        <div className="grid md:grid-cols-3 gap-4">
          {featureCards.map((f, i) => (
            <div key={f.id || i} className="border rounded-xl p-5">
              <div className="flex items-center justify-center gap-2 mb-3">
                {(f.id === 'chat' || i === 0) && <Brain className="h-5 w-5" />}
                {(f.id === 'search' || i === 1) && <Search className="h-5 w-5" />}
                {(f.id === 'analytics' || i === 2) && <BarChart3 className="h-5 w-5" />}
                <h3 className="font-semibold text-center">{f.title}</h3>
              </div>
              <Markdown small>{f.body}</Markdown>
            </div>
          ))}
        </div>
      </Card>

      {/* ROADMAPA */}
      <Card title={sections.roadmap.title} subtitle={sections.roadmap.subtitle}>
        <div className="grid md:grid-cols-3 gap-4">
          {roadmapCols.map((col, idx) => (
            <div key={col.id || idx} className="border rounded-xl p-5">
              <div className="flex items-center justify-center gap-2 mb-3">
                {(col.id === 'done' || idx === 0) && <CheckCircle2 className="h-5 w-5" />}
                {(col.id === 'inprogress' || idx === 1) && <Rocket className="h-5 w-5" />}
                {(col.id === 'plan' || idx === 2) && <Star className="h-5 w-5" />}
                <h3 className="font-semibold text-center">{col.title}</h3>
              </div>
              <Markdown small>{col.body}</Markdown>
            </div>
          ))}
        </div>
      </Card>

      {/* BENEFITY */}
      <Card title={sections.benefits.title} subtitle={sections.benefits.subtitle}>
        <div className="grid md:grid-cols-3 gap-4">
          <BenefitsList md={sections.benefits.content} />
        </div>
      </Card>

      {/* AUTORZY */}
      <Card title={sections.authors.title} subtitle={sections.authors.subtitle}>
        <AuthorsList md={sections.authors.content} />
      </Card>
    </div>
  )
}

// ---- Komponenty pomocnicze ----

function Card({
  title, subtitle, children,
}: { title: string; subtitle?: string; children: React.ReactNode }) {
  return (
    <section className="bg-white shadow rounded-xl p-6">
      <div className="mb-4">
        <h2 className="text-xl font-bold text-gray-900">{title}</h2>
        {subtitle && <p className="text-gray-600 text-sm mt-1">{subtitle}</p>}
      </div>
      {children}
    </section>
  )
}

function Markdown({ children, small }: { children: string; small?: boolean }) {
  return (
    <div className={small ? 'prose prose-sm max-w-none' : 'prose max-w-none'}>
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        components={{
          h1: ({ children }) => <h1 className="text-2xl font-bold text-gray-900 mb-4">{children}</h1>,
          h2: ({ children }) => <h2 className="text-xl font-semibold text-gray-900 mt-6 mb-3">{children}</h2>,
          h3: ({ children }) => <h3 className="text-lg font-medium text-gray-900 mt-4 mb-2 text-center">{children}</h3>,
          p: ({ children }) => <p className="text-gray-700 mb-3 leading-relaxed">{children}</p>,
          ul: ({ children }) => <ul className="list-disc list-inside text-gray-700 space-y-1">{children}</ul>,
          ol: ({ children }) => <ol className="list-decimal list-inside text-gray-700 space-y-1">{children}</ol>,
          a: ({ href, children }) => (
            <a href={href as string} target="_blank" rel="noopener noreferrer" className="text-blue-600 underline">
              {children}
            </a>
          ),
          code: ({ children }) => <code className="bg-gray-100 px-1.5 py-0.5 rounded text-sm">{children}</code>,
          pre: ({ children }) => <pre className="bg-gray-100 p-3 rounded-lg overflow-x-auto text-sm mb-3">{children}</pre>,
          blockquote: ({ children }) => <blockquote className="border-l-4 border-gray-300 pl-3 italic text-gray-600 mb-3">{children}</blockquote>,
          table: ({ children }) => <div className="overflow-x-auto"><table className="w-full text-left border-collapse">{children}</table></div>,
          th: ({ children }) => <th className="border-b py-2 pr-4 font-semibold">{children}</th>,
          td: ({ children }) => <td className="border-b py-2 pr-4">{children}</td>,
        }}
      >
        {children}
      </ReactMarkdown>
    </div>
  )
}

function BenefitsList({ md }: { md: string }) {
  // Expects list: - **Title**: description
  const items = (md.match(/^\s*-\s+\*\*(.+?)\*\*:\s+(.+)$/gmi) || []).map(line => {
    const m = line.match(/^\s*-\s+\*\*(.+?)\*\*:\s+(.+)$/i)
    return m ? { title: m[1].trim(), desc: m[2].trim() } : null
  }).filter(Boolean) as {title:string;desc:string}[]

  if (items.length === 0) return <Markdown>{md}</Markdown>

  return (
    <>
      {items.map((it, i) => (
        <div key={i} className="border rounded-xl p-5">
          <div className="flex items-center justify-center gap-2 mb-2">
            <Activity className="h-5 w-5" />
            <h3 className="font-semibold text-center">{it.title}</h3>
          </div>
          <p className="text-gray-700">{it.desc}</p>
        </div>
      ))}
    </>
  )
}

function AuthorsList({ md }: { md: string }) {
  // Expects list: - **Name** — Role
  const people = (md.match(/^\s*-\s+\*\*(.+?)\*\*\s+—\s+(.+)$/gmi) || []).map(line => {
    const m = line.match(/^\s*-\s+\*\*(.+?)\*\*\s+—\s+(.+)$/i)
    return m ? { name: m[1].trim(), role: m[2].trim() } : null
  }).filter(Boolean) as {name:string;role:string}[]

  if (people.length === 0) return <Markdown>{md}</Markdown>

  return (
    <div className="grid md:grid-cols-3 gap-4">
      {people.map((p, i) => (
        <div key={i} className="border rounded-xl p-5 flex items-center gap-3">
          <div className="h-10 w-10 rounded-full bg-gray-100 flex items-center justify-center font-semibold">
            {initials(p.name)}
          </div>
          <div>
            <div className="font-semibold flex items-center gap-2">
              <Users className="h-4 w-4" />
              {p.name}
            </div>
            <div className="text-gray-600 text-sm">{p.role}</div>
          </div>
        </div>
      ))}
    </div>
  )
}

// ---- utils ----
function initials(name: string) {
  const parts = name.trim().split(/\s+/).slice(0, 2)
  return parts.map(p => p[0]?.toUpperCase() ?? '').join('')
}
