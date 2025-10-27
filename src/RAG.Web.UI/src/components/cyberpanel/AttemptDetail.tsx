import React, { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useI18n } from '@/contexts/I18nContext'
import apiClient from '@/services/api'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { ArrowLeft, CheckCircle, XCircle, User, Calendar, Trophy } from 'lucide-react'
import type { AttemptDetailDto } from '@/types'

export default function AttemptDetail() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { t } = useI18n()
  const [attempt, setAttempt] = useState<AttemptDetailDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (id) {
      fetchAttemptDetail(id)
    }
  }, [id])

  const fetchAttemptDetail = async (attemptId: string) => {
    setLoading(true)
    setError(null)
    try {
      const response = await apiClient.getAttemptById(attemptId)
      setAttempt(response.attempt)
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to load attempt details')
    } finally {
      setLoading(false)
    }
  }

  const handleBack = () => {
    navigate('/cyberpanel/results')
  }

  if (loading) {
    return (
      <div className="max-w-4xl">
        <div className="text-center py-8">
          <p className="text-gray-600">{t('common.loading')}</p>
        </div>
      </div>
    )
  }

  if (error || !attempt) {
    return (
      <div className="max-w-4xl">
        <Button onClick={handleBack} variant="outline" className="mb-4">
          <ArrowLeft className="w-4 h-4 mr-2" />
          {t('cyberpanel.backToResults')}
        </Button>
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <p className="text-red-800">{error || t('common.error')}</p>
        </div>
      </div>
    )
  }

  const submittedDate = new Date(attempt.submittedAt)
  const percentage = attempt.percentageScore

  const getScoreColor = (pct: number) => {
    if (pct >= 80) return 'text-green-600'
    if (pct >= 60) return 'text-yellow-600'
    return 'text-red-600'
  }

  const getScoreBgColor = (pct: number) => {
    if (pct >= 80) return 'bg-green-50 border-green-200'
    if (pct >= 60) return 'bg-yellow-50 border-yellow-200'
    return 'bg-red-50 border-red-200'
  }

  return (
    <div className="max-w-4xl">
      <Button onClick={handleBack} variant="outline" className="mb-4">
        <ArrowLeft className="w-4 h-4 mr-2" />
        {t('cyberpanel.backToResults')}
      </Button>

      {/* Header Card */}
      <Card className="mb-6">
        <CardHeader>
          <CardTitle className="text-2xl">{attempt.quizTitle}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
            <div className="flex items-center gap-2 text-gray-600">
              <User className="w-4 h-4" />
              <div>
                <p className="text-sm font-medium">{t('cyberpanel.userName')}</p>
                <p className="text-sm">{attempt.userName}</p>
                {attempt.userEmail && <p className="text-xs text-gray-500">{attempt.userEmail}</p>}
              </div>
            </div>
            <div className="flex items-center gap-2 text-gray-600">
              <Calendar className="w-4 h-4" />
              <div>
                <p className="text-sm font-medium">{t('cyberpanel.submittedAt')}</p>
                <p className="text-sm">{submittedDate.toLocaleDateString()}</p>
                <p className="text-xs text-gray-500">
                  {submittedDate.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                </p>
              </div>
            </div>
            <div className="flex items-center gap-2 text-gray-600">
              <Trophy className="w-4 h-4" />
              <div>
                <p className="text-sm font-medium">{t('cyberpanel.score')}</p>
                <p className={`text-xl font-bold ${getScoreColor(percentage)}`}>
                  {percentage.toFixed(1)}%
                </p>
                <p className="text-xs text-gray-500">
                  {attempt.score}/{attempt.maxScore} {t('cyberpanel.points')}
                </p>
              </div>
            </div>
          </div>

          {/* Score Summary */}
          <div className="grid grid-cols-3 gap-4">
            <div className={`text-center p-4 rounded-lg border-2 ${getScoreBgColor(percentage)}`}>
              <p className={`text-2xl font-bold ${getScoreColor(percentage)}`}>
                {percentage.toFixed(1)}%
              </p>
              <p className="text-sm text-gray-600">{t('cyberpanel.percentage')}</p>
            </div>
            <div className="text-center p-4 bg-green-50 rounded-lg border border-green-200">
              <div className="flex items-center justify-center gap-1 mb-1">
                <CheckCircle className="w-5 h-5 text-green-600" />
                <p className="text-2xl font-bold text-green-600">{attempt.correctAnswers}</p>
              </div>
              <p className="text-sm text-gray-600">{t('cyberpanel.correct')}</p>
            </div>
            <div className="text-center p-4 bg-red-50 rounded-lg border border-red-200">
              <div className="flex items-center justify-center gap-1 mb-1">
                <XCircle className="w-5 h-5 text-red-600" />
                <p className="text-2xl font-bold text-red-600">
                  {attempt.questionCount - attempt.correctAnswers}
                </p>
              </div>
              <p className="text-sm text-gray-600">{t('cyberpanel.incorrect')}</p>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Questions with Answers */}
      <div className="space-y-4">
        <h3 className="text-lg font-semibold mb-2">{t('cyberpanel.answerDetails')}</h3>
        {attempt.questions.map((question, index) => (
          <Card key={question.questionId} className={question.isCorrect ? 'border-green-200' : 'border-red-200'}>
            <CardHeader className={question.isCorrect ? 'bg-green-50' : 'bg-red-50'}>
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-2">
                    {question.isCorrect ? (
                      <CheckCircle className="w-5 h-5 text-green-600" />
                    ) : (
                      <XCircle className="w-5 h-5 text-red-600" />
                    )}
                    <span className="text-sm font-medium text-gray-600">
                      {t('cyberpanel.question')} {index + 1}/{attempt.questionCount}
                    </span>
                  </div>
                  <CardTitle className="text-base">{question.questionText}</CardTitle>
                  {question.questionImageUrl && (
                    <img
                      src={question.questionImageUrl}
                      alt="Question"
                      className="mt-2 max-w-sm rounded"
                    />
                  )}
                </div>
                <div className="text-right ml-4">
                  <p className="text-sm text-gray-600">
                    {question.pointsAwarded}/{question.points} {t('cyberpanel.points')}
                  </p>
                </div>
              </div>
            </CardHeader>
            <CardContent className="pt-4">
              <div className="space-y-2">
                {question.options.map((option) => {
                  const isSelected = question.selectedOptionIds.includes(option.id)
                  const isCorrectOption = option.isCorrect

                  let borderColor = 'border-gray-200'
                  let bgColor = 'bg-white'
                  let icon = null
                  let label = null

                  if (isSelected && isCorrectOption) {
                    // User selected correct answer
                    borderColor = 'border-green-300'
                    bgColor = 'bg-green-50'
                    icon = <CheckCircle className="w-5 h-5 text-green-600" />
                    label = <p className="text-xs text-green-600 mt-1 font-medium">{t('cyberpanel.yourCorrectAnswer')}</p>
                  } else if (isSelected && !isCorrectOption) {
                    // User selected wrong answer
                    borderColor = 'border-red-300'
                    bgColor = 'bg-red-50'
                    icon = <XCircle className="w-5 h-5 text-red-600" />
                    label = <p className="text-xs text-red-600 mt-1 font-medium">{t('cyberpanel.yourAnswer')}</p>
                  } else if (!isSelected && isCorrectOption) {
                    // Correct answer not selected by user
                    borderColor = 'border-green-200'
                    bgColor = 'bg-green-50/50'
                    icon = <CheckCircle className="w-5 h-5 text-green-500" />
                    label = <p className="text-xs text-green-600 mt-1">{t('cyberpanel.correctAnswerWas')}</p>
                  } else {
                    // Neutral option (not selected, not correct)
                    borderColor = 'border-gray-200'
                    bgColor = 'bg-gray-50'
                    icon = null
                  }

                  return (
                    <div
                      key={option.id}
                      className={`flex items-start gap-3 p-3 rounded border ${borderColor} ${bgColor}`}
                    >
                      <div className="w-5 h-5 flex items-center justify-center flex-shrink-0">
                        {icon}
                      </div>
                      <div className="flex-1">
                        <p className="text-sm">{option.text}</p>
                        {option.imageUrl && (
                          <img src={option.imageUrl} alt="Option" className="mt-2 max-w-xs rounded" />
                        )}
                        {label}
                      </div>
                    </div>
                  )
                })}
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  )
}
