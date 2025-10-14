import React from 'react'
import { useI18n } from '@/contexts/I18nContext'

export default function QuizResults() {
  const { t } = useI18n()
  
  return (
    <div className="max-w-4xl">
      <h3 className="text-xl md:text-2xl font-bold mb-3 md:mb-4">{t('cyberpanel.results')}</h3>
      <p className="text-sm md:text-base text-gray-600">Wyniki i raporty (Admin, PowerUser).</p>
    </div>
  )
}
