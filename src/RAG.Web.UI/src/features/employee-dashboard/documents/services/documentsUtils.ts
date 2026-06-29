import type { TranslationKeys } from '@/shared/types/i18n'
import type {
  DocumentAuditAction,
  DocumentCategory,
  DocumentCategoryId,
  DocumentStatus,
} from '../types/documentsTypes'

type Translate = (key: keyof TranslationKeys, params?: Record<string, string> | string[]) => string

export function formatDocumentDate(value: string, locale = 'pl-PL'): string {
  return new Intl.DateTimeFormat(locale, {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(new Date(value))
}

export function formatDocumentDateTime(value: string, locale = 'pl-PL'): string {
  return new Intl.DateTimeFormat(locale, {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}

export function documentStatusLabel(status: DocumentStatus, t: Translate): string {
  const labels: Record<DocumentStatus, keyof TranslationKeys> = {
    available: 'employeeDashboard.documents.status.available',
    new: 'employeeDashboard.documents.status.new',
    updated: 'employeeDashboard.documents.status.updated',
    archived: 'employeeDashboard.documents.status.archived',
  }

  return t(labels[status])
}

export function documentAuditActionLabel(action: DocumentAuditAction, t: Translate): string {
  const labels: Record<DocumentAuditAction, keyof TranslationKeys> = {
    download: 'employeeDashboard.documents.audit.action.download',
    preview: 'employeeDashboard.documents.audit.action.preview',
  }

  return t(labels[action])
}

export function getCategoryName(categoryId: DocumentCategoryId, allCategories: DocumentCategory[], t: Translate): string {
  const category = allCategories.find((item) => item.id === categoryId)
  return category ? t(category.nameKey) : t('employeeDashboard.documents.category.unknown')
}

export function getCategoryDescription(category: DocumentCategory, t: Translate): string {
  return t(category.descriptionKey)
}
