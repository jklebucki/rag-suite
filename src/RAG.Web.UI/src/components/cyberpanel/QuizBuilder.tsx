import React from 'react'
import { useI18n } from '@/contexts/I18nContext'

export default function QuizBuilder() {
  const { t } = useI18n()
  
  return (
    <div className="max-w-4xl">
      <h3 className="text-xl md:text-2xl font-bold mb-3 md:mb-4">{t('cyberpanel.builder')}</h3>
      <p className="text-sm md:text-base text-gray-600">Narzędzie do tworzenia quizów (dostępne tylko dla Admin).</p>
    </div>
  )
}
