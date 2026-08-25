import React from 'react'
import {
  ArrowRight, BookOpenCheck, Brain, Check, CheckCircle2, Compass, Database,
  FileCheck2, Languages, Lightbulb, Network, Search, ShieldCheck, Sparkles,
  Target, Users,
} from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import { TranslationKeys } from '@/shared/types/i18n'

type TranslationKey = keyof TranslationKeys
type Icon = React.ComponentType<{ className?: string }>

interface CardContent {
  title: TranslationKey
  description: TranslationKey
  icon: Icon
}

const capabilities: (CardContent & { points: TranslationKey[] })[] = [
  {
    title: 'about.capabilities.ask.title', description: 'about.capabilities.ask.description',
    points: ['about.capabilities.ask.point1', 'about.capabilities.ask.point2'], icon: Brain,
  },
  {
    title: 'about.capabilities.find.title', description: 'about.capabilities.find.description',
    points: ['about.capabilities.find.point1', 'about.capabilities.find.point2'], icon: Search,
  },
  {
    title: 'about.capabilities.grow.title', description: 'about.capabilities.grow.description',
    points: ['about.capabilities.grow.point1', 'about.capabilities.grow.point2'], icon: BookOpenCheck,
  },
]

const roadmap: (CardContent & { items: TranslationKey[]; accent: string })[] = [
  {
    title: 'about.roadmap.available.title', description: 'about.roadmap.available.description',
    items: ['about.roadmap.available.item1', 'about.roadmap.available.item2', 'about.roadmap.available.item3'],
    icon: CheckCircle2,
    accent: 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950/50 dark:text-emerald-300',
  },
  {
    title: 'about.roadmap.now.title', description: 'about.roadmap.now.description',
    items: ['about.roadmap.now.item1', 'about.roadmap.now.item2', 'about.roadmap.now.item3'],
    icon: Sparkles,
    accent: 'bg-primary-50 text-primary-700 dark:bg-primary-950/60 dark:text-primary-300',
  },
  {
    title: 'about.roadmap.direction.title', description: 'about.roadmap.direction.description',
    items: ['about.roadmap.direction.item1', 'about.roadmap.direction.item2', 'about.roadmap.direction.item3'],
    icon: Compass,
    accent: 'bg-violet-50 text-violet-700 dark:bg-violet-950/50 dark:text-violet-300',
  },
]

const benefits: CardContent[] = [
  { title: 'about.value.questions.title', description: 'about.value.questions.description', icon: Users },
  { title: 'about.value.sources.title', description: 'about.value.sources.description', icon: FileCheck2 },
  { title: 'about.value.onboarding.title', description: 'about.value.onboarding.description', icon: Lightbulb },
  { title: 'about.value.retention.title', description: 'about.value.retention.description', icon: Database },
  { title: 'about.value.quality.title', description: 'about.value.quality.description', icon: Target },
]

const authors = [
  ['about.authors.person1.name', 'about.authors.person1.role'],
  ['about.authors.person2.name', 'about.authors.person2.role'],
  ['about.authors.person3.name', 'about.authors.person3.role'],
] as const satisfies ReadonlyArray<readonly [TranslationKey, TranslationKey]>

export function About() {
  const { t } = useI18n()

  return (
    <div className="mx-auto max-w-7xl space-y-8 pb-6 sm:space-y-10 lg:space-y-12">
      <section className="surface relative isolate overflow-hidden p-6 sm:p-8 lg:p-10">
        <div className="absolute -right-24 -top-28 -z-10 h-72 w-72 rounded-full bg-primary-100/70 blur-3xl dark:bg-primary-900/20" />
        <div className="grid items-end gap-8 xl:grid-cols-[minmax(0,1.5fr)_minmax(280px,0.7fr)]">
          <div className="max-w-3xl">
            <p className="mb-5 inline-flex items-center gap-2 rounded-full border border-primary-200 bg-primary-50 px-3 py-1.5 text-sm font-medium text-primary-700 dark:border-primary-800 dark:bg-primary-950/60 dark:text-primary-300">
              <ShieldCheck className="h-4 w-4" />
              {t('about.hero.eyebrow')}
            </p>
            <h1 className="text-3xl font-bold tracking-tight text-gray-950 dark:text-white sm:text-4xl lg:text-5xl">{t('app.title')}</h1>
            <p className="mt-4 max-w-2xl text-xl font-medium leading-snug text-gray-800 dark:text-gray-200 sm:text-2xl">
              {t('about.hero.headline')}
            </p>
            <p className="mt-5 max-w-2xl text-base leading-7 text-gray-600 dark:text-gray-300 sm:text-lg">
              {t('about.hero.description')}
            </p>
          </div>
          <div className="grid gap-2 sm:grid-cols-3 xl:grid-cols-1">
            <HeroFact icon={Network} label={t('about.hero.fact.rag')} />
            <HeroFact icon={FileCheck2} label={t('about.hero.fact.sources')} />
            <HeroFact icon={Languages} label={t('about.hero.fact.languages')} />
          </div>
        </div>
      </section>

      <section aria-labelledby="capabilities-title">
        <SectionHeading eyebrow={t('about.capabilities.eyebrow')} title={t('about.capabilities.title')}
          description={t('about.capabilities.description')} id="capabilities-title" />
        <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {capabilities.map(({ title, description, points, icon: Icon }) => (
            <article key={title} className="surface group flex min-h-full flex-col p-5 transition duration-200 hover:-translate-y-0.5 hover:border-primary-200 hover:shadow-md dark:hover:border-primary-800 sm:p-6">
              <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-primary-50 text-primary-700 dark:bg-primary-950/60 dark:text-primary-300">
                <Icon className="h-5 w-5" />
              </div>
              <h3 className="mt-5 text-lg font-semibold text-gray-950 dark:text-white">{t(title)}</h3>
              <p className="mt-2 leading-6 text-gray-600 dark:text-gray-300">{t(description)}</p>
              <ul className="mt-5 space-y-3 border-t border-gray-200 pt-5 dark:border-slate-800">
                {points.map(point => (
                  <li key={point} className="flex gap-3 text-sm leading-5 text-gray-700 dark:text-gray-300">
                    <Check className="mt-0.5 h-4 w-4 shrink-0 text-primary-600 dark:text-primary-400" />
                    <span>{t(point)}</span>
                  </li>
                ))}
              </ul>
            </article>
          ))}
        </div>

        <div className="surface-muted mt-4 grid gap-2 p-4 sm:grid-cols-[1fr_auto_1fr_auto_1fr_auto_1fr] sm:items-center sm:px-6"
          aria-label={t('about.flow.ariaLabel')}>
          {([['about.flow.question', Brain], ['about.flow.information', Search],
            ['about.flow.knowledge', BookOpenCheck], ['about.flow.processes', Network]] as const)
            .map(([label, Icon], index) => (
              <React.Fragment key={label}>
                <div className="flex items-center gap-3 rounded-xl px-3 py-2 text-sm font-medium text-gray-800 dark:text-gray-200">
                  <Icon className="h-4 w-4 shrink-0 text-primary-600 dark:text-primary-400" />
                  <span>{t(label)}</span>
                </div>
                {index < 3 && <ArrowRight className="hidden h-4 w-4 text-gray-400 sm:block dark:text-slate-500" />}
              </React.Fragment>
            ))}
        </div>
      </section>

      <section aria-labelledby="roadmap-title">
        <SectionHeading eyebrow={t('about.roadmap.eyebrow')} title={t('about.roadmap.title')}
          description={t('about.roadmap.description')} id="roadmap-title" />
        <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {roadmap.map(({ title, description, items, icon: Icon, accent }) => (
            <article key={title} className="surface flex min-h-full flex-col p-5 sm:p-6">
              <div className={`flex h-10 w-10 items-center justify-center rounded-xl ${accent}`}><Icon className="h-5 w-5" /></div>
              <h3 className="mt-4 text-lg font-semibold text-gray-950 dark:text-white">{t(title)}</h3>
              <p className="mt-2 text-sm leading-6 text-gray-600 dark:text-gray-300">{t(description)}</p>
              <ul className="mt-5 space-y-3">
                {items.map(item => (
                  <li key={item} className="flex gap-3 text-sm leading-5 text-gray-700 dark:text-gray-300">
                    <span className="mt-2 h-1.5 w-1.5 shrink-0 rounded-full bg-gray-400 dark:bg-slate-500" />
                    <span>{t(item)}</span>
                  </li>
                ))}
              </ul>
            </article>
          ))}
        </div>
      </section>

      <section aria-labelledby="value-title">
        <SectionHeading eyebrow={t('about.value.eyebrow')} title={t('about.value.title')}
          description={t('about.value.description')} id="value-title" />
        <div className="mt-6 grid gap-4 xl:grid-cols-[minmax(280px,0.8fr)_minmax(0,1.5fr)]">
          <article className="surface relative overflow-hidden border-primary-200 bg-primary-50/50 p-6 dark:border-primary-900 dark:bg-primary-950/20 sm:p-7">
            <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-primary-100 text-primary-700 dark:bg-primary-900/70 dark:text-primary-300"><Network className="h-5 w-5" /></div>
            <h3 className="mt-5 text-xl font-semibold text-gray-950 dark:text-white">{t('about.value.primary.title')}</h3>
            <p className="mt-3 leading-7 text-gray-700 dark:text-gray-300">{t('about.value.primary.description')}</p>
          </article>
          <div className="grid gap-4 sm:grid-cols-2">
            {benefits.map(({ title, description, icon: Icon }, index) => (
              <article key={title} className={`surface p-5 ${index === benefits.length - 1 ? 'sm:col-span-2' : ''}`}>
                <div className="flex items-start gap-4">
                  <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-gray-100 text-gray-700 dark:bg-slate-800 dark:text-gray-300"><Icon className="h-4 w-4" /></div>
                  <div>
                    <h3 className="font-semibold text-gray-950 dark:text-white">{t(title)}</h3>
                    <p className="mt-1 text-sm leading-6 text-gray-600 dark:text-gray-300">{t(description)}</p>
                  </div>
                </div>
              </article>
            ))}
          </div>
        </div>
      </section>

      <footer className="surface-muted p-5 sm:p-6">
        <div className="grid gap-5 xl:grid-cols-[minmax(220px,0.6fr)_minmax(0,1.4fr)] xl:items-center">
          <div>
            <h2 className="font-semibold text-gray-950 dark:text-white">{t('about.authors.title')}</h2>
            <p className="mt-1 text-sm text-gray-600 dark:text-gray-300">{t('about.authors.description')}</p>
          </div>
          <div className="grid gap-3 sm:grid-cols-3">
            {authors.map(([name, role]) => (
              <div key={name} className="flex min-w-0 items-center gap-3">
                <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-white text-sm font-semibold text-primary-700 shadow-sm dark:bg-slate-900 dark:text-primary-300">{getInitials(t(name))}</div>
                <div className="min-w-0">
                  <p className="truncate text-sm font-medium text-gray-900 dark:text-gray-100">{t(name)}</p>
                  <p className="text-xs leading-5 text-gray-500 dark:text-gray-400">{t(role)}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </footer>
    </div>
  )
}

function HeroFact({ icon: Icon, label }: { icon: Icon; label: string }) {
  return (
    <div className="flex items-center gap-3 rounded-xl border border-gray-200 bg-white/70 px-4 py-3 text-sm font-medium text-gray-700 shadow-sm backdrop-blur-sm dark:border-slate-800 dark:bg-slate-900/70 dark:text-gray-200">
      <Icon className="h-4 w-4 shrink-0 text-primary-600 dark:text-primary-400" /><span>{label}</span>
    </div>
  )
}

function SectionHeading({ eyebrow, title, description, id }: { eyebrow: string; title: string; description: string; id: string }) {
  return (
    <div className="max-w-3xl">
      <p className="text-sm font-semibold uppercase tracking-wider text-primary-700 dark:text-primary-300">{eyebrow}</p>
      <h2 id={id} className="mt-2 text-2xl font-bold tracking-tight text-gray-950 dark:text-white sm:text-3xl">{title}</h2>
      <p className="mt-3 leading-7 text-gray-600 dark:text-gray-300">{description}</p>
    </div>
  )
}

function getInitials(name: string) {
  return name.split(/\s+/).slice(0, 2).map(part => part[0]).join('').toUpperCase()
}
