import React from 'react'
import { Link } from 'react-router-dom'
import { useI18n } from '@/contexts/I18nContext'

export function LandingPage() {
  const { t } = useI18n()

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-start justify-center">
      <div className="max-w-4xl mx-auto px-4 py-16 text-center">
        {/* Logo */}
        <div className="mb-8">
          <img
            src="/logo-citronex-2x.png"
            alt={t('landing.logo_alt')}
            className="mx-auto h-24 w-auto"
          />
        </div>

        {/* Main Heading */}
        <h1 className="text-4xl md:text-6xl font-bold text-gray-900 mb-6">
          {t('landing.title_prefix')} <span className="text-indigo-600">{t('landing.title_brand')}</span>
        </h1>

        {/* Subtitle */}
        <p className="text-xl md:text-2xl text-gray-600 mb-12 max-w-2xl mx-auto">
          {t('landing.subtitle')}
        </p>

        {/* Call to Action Buttons */}
        <div className="flex flex-col sm:flex-row gap-4 justify-center">
          <Link
            to="/guide"
            className="bg-indigo-600 hover:bg-indigo-700 text-white font-semibold py-3 px-8 rounded-lg transition duration-300"
          >
            {t('landing.cta.get_started')}
          </Link>
          <Link
            to="/about"
            className="bg-white hover:bg-gray-50 text-indigo-600 font-semibold py-3 px-8 rounded-lg border border-indigo-600 transition duration-300"
          >
            {t('landing.cta.learn_more')}
          </Link>
        </div>

        {/* Features Preview */}
        <div className="mt-16 grid md:grid-cols-3 gap-8">
          <div className="bg-white p-6 rounded-lg shadow-md">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">{t('landing.features.ingestion.title')}</h3>
            <p className="text-gray-600">{t('landing.features.ingestion.desc')}</p>
          </div>
          <div className="bg-white p-6 rounded-lg shadow-md">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">{t('landing.features.search.title')}</h3>
            <p className="text-gray-600">{t('landing.features.search.desc')}</p>
          </div>
          <div className="bg-white p-6 rounded-lg shadow-md">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">{t('landing.features.chat.title')}</h3>
            <p className="text-gray-600">{t('landing.features.chat.desc')}</p>
          </div>
        </div>
      </div>
    </div>
  )
}