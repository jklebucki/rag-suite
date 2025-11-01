import { useI18n } from '@/contexts/I18nContext'

export default function AddressBook() {
  const { t } = useI18n()

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-7xl mx-auto">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">
          {t('nav.addressBook')}
        </h1>
        <p className="text-gray-600 mb-6">
          Coming soon...
        </p>
        <div className="bg-white rounded-lg shadow p-8">
          <p className="text-gray-500 text-center py-12">
            {t('common.loading')}
          </p>
        </div>
      </div>
    </div>
  )
}
