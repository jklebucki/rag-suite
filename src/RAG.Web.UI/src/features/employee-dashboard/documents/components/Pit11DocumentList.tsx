import { Download, FileText, Inbox } from 'lucide-react'
import { Button } from '@/shared/components/ui'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { Pit11Document } from '../types/documentsTypes'

interface Pit11DocumentListProps {
  documents: Pit11Document[]
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat('pl-PL', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(new Date(value))
}

export function Pit11DocumentList({ documents }: Pit11DocumentListProps) {
  const { t } = useI18n()

  return (
    <div className="surface overflow-hidden">
      {documents.length === 0 ? (
        <div className="flex flex-col items-center justify-center px-6 py-12 text-center">
          <Inbox className="h-10 w-10 text-gray-400 dark:text-gray-500" />
          <p className="mt-3 text-sm text-gray-500 dark:text-gray-400">
            {t('employeeDashboard.pit11.empty')}
          </p>
        </div>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full min-w-[640px] text-sm">
            <thead>
              <tr className="border-b border-gray-100 bg-gray-50 dark:border-slate-800 dark:bg-slate-900/50">
                <th className="px-5 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                  {t('employeeDashboard.pit11.documentName')}
                </th>
                <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                  {t('employeeDashboard.pit11.taxYear')}
                </th>
                <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                  {t('employeeDashboard.pit11.generatedAt')}
                </th>
                <th className="px-5 py-3 text-right text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                  {t('employeeDashboard.pit11.actions')}
                </th>
              </tr>
            </thead>
            <tbody>
              {documents.map((document) => (
                  <tr
                    key={document.id}
                    className="border-b border-gray-50 last:border-0 dark:border-slate-800"
                  >
                    <td className="px-5 py-4">
                      <div className="flex items-center gap-3">
                        <div className="rounded-lg bg-primary-50 p-2 dark:bg-primary-900/20">
                          <FileText className="h-5 w-5 text-primary-600 dark:text-primary-400" />
                        </div>
                        <span className="font-medium text-gray-900 dark:text-gray-100">
                          {document.name}
                        </span>
                      </div>
                    </td>
                    <td className="px-4 py-4 tabular-nums text-gray-600 dark:text-gray-300">
                      {document.taxYear}
                    </td>
                    <td className="px-4 py-4 tabular-nums text-gray-600 dark:text-gray-300">
                      {formatDate(document.generatedAt)}
                    </td>
                    <td className="px-5 py-4 text-right">
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        disabled
                      >
                        <Download className="mr-2 h-4 w-4" />
                        {t('employeeDashboard.pit11.download')}
                      </Button>
                    </td>
                  </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
