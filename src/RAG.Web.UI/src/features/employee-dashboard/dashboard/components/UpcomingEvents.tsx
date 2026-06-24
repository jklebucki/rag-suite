import React from 'react'
import { CalendarClock, Stethoscope, ShieldCheck, Plane, Star, MapPin } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { UpcomingEvent, UpcomingEventType } from '../types/employeeDashboard'

interface Props {
  events: UpcomingEvent[]
}

function eventConfig(type: UpcomingEventType) {
  switch (type) {
    case 'leave':
      return {
        icon: Plane,
        iconClass: 'text-green-600 dark:text-green-400',
        bgClass: 'bg-green-50 dark:bg-green-900/20',
        barClass: 'bg-green-500',
      }
    case 'medical':
      return {
        icon: Stethoscope,
        iconClass: 'text-red-600 dark:text-red-400',
        bgClass: 'bg-red-50 dark:bg-red-900/20',
        barClass: 'bg-red-500',
      }
    case 'bhp_training':
      return {
        icon: ShieldCheck,
        iconClass: 'text-amber-600 dark:text-amber-400',
        bgClass: 'bg-amber-50 dark:bg-amber-900/20',
        barClass: 'bg-amber-500',
      }
    case 'delegation':
      return {
        icon: MapPin,
        iconClass: 'text-blue-600 dark:text-blue-400',
        bgClass: 'bg-blue-50 dark:bg-blue-900/20',
        barClass: 'bg-blue-500',
      }
    default:
      return {
        icon: Star,
        iconClass: 'text-purple-600 dark:text-purple-400',
        bgClass: 'bg-purple-50 dark:bg-purple-900/20',
        barClass: 'bg-purple-500',
      }
  }
}

function formatEventDate(startDate: string, endDate?: string): string {
  const start = new Date(startDate)
  const opts: Intl.DateTimeFormatOptions = { day: '2-digit', month: 'short', year: 'numeric' }

  if (!endDate || endDate === startDate) {
    return start.toLocaleDateString(undefined, opts)
  }

  const end = new Date(endDate)
  return `${start.toLocaleDateString(undefined, opts)} – ${end.toLocaleDateString(undefined, opts)}`
}

function daysUntil(iso: string): number {
  return Math.ceil((new Date(iso).getTime() - Date.now()) / 86400000)
}

interface EventCardProps {
  event: UpcomingEvent
}

function EventCard({ event }: EventCardProps) {
  const { t } = useI18n()
  const cfg = eventConfig(event.type)
  const Icon = cfg.icon
  const days = daysUntil(event.startDate)
  const isToday = days <= 0
  const isSoon = days > 0 && days <= 7

  return (
    <div className="flex gap-3 p-3 rounded-xl border border-gray-100 dark:border-slate-800 bg-white dark:bg-slate-900 transition-colors">
      <div className={`p-2 rounded-lg flex-shrink-0 self-start ${cfg.bgClass}`}>
        <Icon className={`h-4 w-4 ${cfg.iconClass}`} />
      </div>

      <div className="flex-1 min-w-0">
        <div className="flex items-start justify-between gap-2">
          <p className="text-sm font-semibold text-gray-900 dark:text-gray-100 leading-tight">
            {event.title}
          </p>
          <span
            className={`text-xs font-medium px-2 py-0.5 rounded-full flex-shrink-0 ${
              isToday
                ? 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300'
                : isSoon
                ? 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300'
                : 'bg-gray-100 text-gray-600 dark:bg-slate-800 dark:text-gray-400'
            }`}
          >
            {isToday
              ? t('employeeDashboard.events.today')
              : `${days} ${t('employeeDashboard.events.daysLeft')}`}
          </span>
        </div>

        <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
          {formatEventDate(event.startDate, event.endDate)}
        </p>

        {event.description && (
          <p className="text-xs text-gray-400 dark:text-gray-500 mt-1 leading-relaxed">
            {event.description}
          </p>
        )}
      </div>
    </div>
  )
}

export function UpcomingEvents({ events }: Props) {
  const { t } = useI18n()

  const sorted = [...events].sort(
    (a, b) => new Date(a.startDate).getTime() - new Date(b.startDate).getTime()
  )

  return (
    <div className="surface p-5 flex flex-col gap-3">
      <div className="flex items-center gap-2">
        <div className="p-2 bg-primary-50 dark:bg-primary-900/20 rounded-lg">
          <CalendarClock className="h-5 w-5 text-primary-600 dark:text-primary-400" />
        </div>
        <h2 className="font-semibold text-gray-900 dark:text-gray-100">
          {t('employeeDashboard.events.title')}
        </h2>
        <span className="ml-auto text-xs text-gray-500 dark:text-gray-400">
          {sorted.length} {t('employeeDashboard.events.eventsCount')}
        </span>
      </div>

      {sorted.length === 0 ? (
        <p className="text-sm text-gray-500 dark:text-gray-400 py-4 text-center">
          {t('employeeDashboard.events.empty')}
        </p>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-2">
          {sorted.map((event) => (
            <EventCard key={event.id} event={event} />
          ))}
        </div>
      )}
    </div>
  )
}
