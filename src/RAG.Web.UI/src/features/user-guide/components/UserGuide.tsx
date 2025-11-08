import { useI18n } from '@/shared/contexts/I18nContext'

/**
 * UserGuide component displays a PDF user manual in an embedded viewer.
 * The PDF is served from /public/assets/guides/user-guide-{lang}.pdf
 */
export function UserGuide() {
  const { t, language } = useI18n()
  const pdfUrl = `/assets/guides/user-guide-${language}.pdf`

  return (
    <div className="h-full w-full">
      <div className="mb-4">
        <h1 className="text-3xl font-bold text-gray-900">
          {t('nav.user_guide')}
        </h1>
        <p className="mt-2 text-sm text-gray-600">
          {t('userguide.description')}
        </p>
      </div>

      {/* PDF viewer - using native browser PDF rendering via iframe */}
      <div className="relative w-full h-[calc(100vh-220px)] min-h-[600px]">
        <iframe
          src={pdfUrl}
          className="w-full h-full border border-gray-300 rounded-lg shadow-sm"
          title={t('nav.user_guide')}
        >
          <p className="p-4 text-gray-600">
            {t('userguide.browser_not_supported')}
            <a
              href={pdfUrl}
              download
              className="text-blue-600 hover:underline ml-2"
            >
              {t('userguide.download_pdf')}
            </a>
          </p>
        </iframe>
      </div>

      {/* Download link for convenience */}
      <div className="mt-4 text-center">
        <a
          href={pdfUrl}
          download
          className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
        >
          <svg
            className="mr-2 h-4 w-4"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
            />
          </svg>
          {t('userguide.download_pdf')}
        </a>
      </div>
    </div>
  )
}

