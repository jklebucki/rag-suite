export type DocumentCategoryId = 'hr' | 'tax' | 'company'

export type DocumentStatus = 'available' | 'new' | 'updated' | 'archived'

export type DocumentAuditAction = 'download' | 'preview'

export interface DocumentCategory {
  id: DocumentCategoryId
  nameKey: `employeeDashboard.documents.category.${DocumentCategoryId}`
  descriptionKey: `employeeDashboard.documents.category.${DocumentCategoryId}.description`
}

export interface EmployeeDocument {
  id: string
  name: string
  categoryId: DocumentCategoryId
  addedAt: string
  version: string
  status: DocumentStatus
  description: string
  owner: string
  fileName: string
  accessLevel: 'employee' | 'company' | 'restricted'
}

export interface DocumentDownloadLog {
  id: string
  downloadedAt: string
  documentName: string
  categoryId: DocumentCategoryId
  user: string
  action: DocumentAuditAction
}

export interface DocumentsPageData {
  categories: DocumentCategory[]
  documents: EmployeeDocument[]
  downloadLogs: DocumentDownloadLog[]
  canViewDocuments: boolean
  canDownloadDocuments: boolean
}
