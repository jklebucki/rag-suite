import React from 'react'
import { Bell, CheckCircle, AlertTriangle, XCircle, Info } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { DashboardNotification, NotificationSeverity, NotificationCategory } from '../types/employeeDashboard'

interface Props {
  notifications: DashboardNotification[]
}

function severityConfig(severity: NotificationSeverity) {
  switch (severity) {
    case 'success':
      return {
        icon: CheckCircle,
        iconClass: 'text-green-600 dark:text-green-400',
        bgClass: 'bg-green-50 dark:bg-green-900/20',
        badgeClass: 'bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-300',
      }
    case 'warning':
      return {
        icon: AlertTriangle,
        iconClass: 'text-amber-600 dark:text-amber-400',
        bgClass: 'bg-amber-50 dark:bg-amber-900/20',
        badgeClass: 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300',
      }
    case 'error':
      return {
        icon: XCircle,
        iconClass: 'text-red-600 dark:text-red-400',
        bgClass: 'bg-red-50 dark:bg-red-900/20',
        badgeClass: 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300',
      }
    default:
      return {
        icon: Info,
        iconClass: 'text-blue-600 dark:text-blue-400',
        bgClass: 'bg-blue-50 dark:bg-blue-900/20',
        badgeClass: 'bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300',
      }
  }
}

function categoryLabelKey(category: NotificationCategory): string {
  switch (category) {
    case 'leave':
      return 'employeeDashboard.notifications.category.leave'
    case 'payslip':
      return 'employeeDashboard.notifications.category.payslip'
    case 'medical':
      return 'employeeDashboard.notifications.category.medical'
    case 'training':
      return 'employeeDashboard.notifications.category.training'
    default:
      return 'employeeDashboard.notifications.category.general'
  }
}

function formatRelativeDate(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime()
  const minutes = Math.floor(diff / 60000)
  if (minutes < 60) return `${minutes}m ago`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours}h ago`
  const days = Math.floor(hours / 24)
  return `${days}d ago`
}

interface NotificationItemProps {
  notification: DashboardNotification
}

function NotificationItem({ notification }: NotificationItemProps) {
  const { t } = useI18n()
  const cfg = severityConfig(notification.severity)
  const Icon = cfg.icon

  return (
    <div
      className={`flex gap-3 p-3 rounded-xl border transition-colors ${
        notification.isRead
          ? 'border-gray-100 dark:border-slate-800 bg-white dark:bg-slate-900'
          : 'border-primary-100 dark:border-primary-800/40 bg-primary-50/50 dark:bg-primary-900/10'
      }`}
    >
      <div className={`p-1.5 rounded-lg flex-shrink-0 self-start ${cfg.bgClass}`}>
        <Icon className={`h-4 w-4 ${cfg.iconClass}`} />
      </div>

      <div className="flex-1 min-w-0">
        <div className="flex items-start justify-between gap-2">
          <p className={`text-sm font-medium leading-tight ${
            notification.isRead
              ? 'text-gray-700 dark:text-gray-300'
              : 'text-gray-900 dark:text-gray-100'
          }`}>
            {notification.title}
          </p>
          <span className="text-xs text-gray-400 dark:text-gray-500 flex-shrink-0">
            {formatRelativeDate(notification.date)}
          </span>
        </div>

        <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5 leading-relaxed">
          {notification.description}
        </p>

        <span className={`inline-block mt-1.5 px-2 py-0.5 rounded-full text-xs font-medium ${cfg.badgeClass}`}>
          {t(categoryLabelKey(notification.category) as Parameters<typeof t>[0])}
        </span>
      </div>

      {!notification.isRead && (
        <div className="w-2 h-2 rounded-full bg-primary-500 flex-shrink-0 mt-1.5" />
      )}
    </div>
  )
}

export function NotificationsCenter({ notifications }: Props) {
  const { t } = useI18n()
  const unreadCount = notifications.filter((n) => !n.isRead).length

  return (
    <div className="surface p-5 flex flex-col gap-3">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <div className="p-2 bg-primary-50 dark:bg-primary-900/20 rounded-lg">
            <Bell className="h-5 w-5 text-primary-600 dark:text-primary-400" />
          </div>
          <h2 className="font-semibold text-gray-900 dark:text-gray-100">
            {t('employeeDashboard.notifications.title')}
          </h2>
        </div>
        {unreadCount > 0 && (
          <span className="inline-flex items-center justify-center h-5 min-w-5 px-1.5 rounded-full bg-primary-600 text-white text-xs font-bold">
            {unreadCount}
          </span>
        )}
      </div>

      {notifications.length === 0 ? (
        <p className="text-sm text-gray-500 dark:text-gray-400 py-4 text-center">
          {t('employeeDashboard.notifications.empty')}
        </p>
      ) : (
        <div className="space-y-2 max-h-96 overflow-y-auto scrollbar-hide pr-0.5">
          {notifications.map((notif) => (
            <NotificationItem key={notif.id} notification={notif} />
          ))}
        </div>
      )}
    </div>
  )
}
