import React from 'react'
import { useParams } from 'react-router-dom'
import { useI18n } from '@/contexts/I18nContext'

export default function QuizDetail() {
  const { id } = useParams()
  const { t } = useI18n()

  return (
    <div className="max-w-4xl">
      <h3 className="text-xl md:text-2xl font-bold mb-3 md:mb-4">
        {t('cyberpanel.quizzes')}: {id}
      </h3>
      <p className="text-sm md:text-base text-gray-600">Szczegóły quizu / podejmowanie quizu.</p>
    </div>
  )
}
