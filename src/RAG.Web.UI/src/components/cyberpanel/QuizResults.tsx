import React, { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useI18n } from '@/contexts/I18nContext'
import apiClient from '@/services/api'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Trophy, Calendar, CheckCircle, XCircle, ArrowRight } from 'lucide-react'
import type { QuizAttemptDto } from '@/types'

export default function QuizResults() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const [attempts, setAttempts] = useState<QuizAttemptDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetchAttempts()
  }, [])

  const fetchAttempts = async () => {
    setLoading(true)
    setError(null)
    try {
      const response = await apiClient.listQuizAttempts()
      setAttempts(response.attempts)
    } catch (err: any) {
      setError(err.message || 'Failed to load quiz results')
    } finally {
      setLoading(false)
    }
  }

  const handleViewQuiz = (attemptId: string) => {
    navigate(`/cyberpanel/attempts/${attemptId}`)
  }

  const getScoreColor = (percentage: number) => {
    if (percentage >= 80) return 'text-green-600'
    if (percentage >= 60) return 'text-yellow-600'
    return 'text-red-600'
  }

  const getScoreBgColor = (percentage: number) => {
    if (percentage >= 80) return 'bg-green-50 border-green-200'
    if (percentage >= 60) return 'bg-yellow-50 border-yellow-200'
    return 'bg-red-50 border-red-200'
  }

  if (loading) {
    return (
      <div className="max-w-6xl">
        <div className="text-center py-8">
          <p className="text-gray-600">{t('common.loading')}</p>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="max-w-6xl">
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-4">
          <p className="text-red-800">{error}</p>
        </div>
      </div>
    )
  }

  return (
    <div className="max-w-6xl">
      <div className="flex justify-between items-center mb-6">
        <h3 className="text-xl md:text-2xl font-bold">{t('cyberpanel.results')}</h3>
      </div>

      {attempts.length === 0 && (
        <Card>
          <CardContent className="py-12 text-center">
            <Trophy className="w-16 h-16 text-gray-400 mx-auto mb-4" />
            <p className="text-gray-600 text-lg mb-2">{t('cyberpanel.noResultsYet')}</p>
            <p className="text-gray-500 text-sm mb-6">{t('cyberpanel.takeFirstQuiz')}</p>
            <Button onClick={() => navigate('/cyberpanel/quizzes')} variant="primary">
              {t('cyberpanel.browseQuizzes')}
            </Button>
          </CardContent>
        </Card>
      )}

      {attempts.length > 0 && (
        <div className="space-y-4">
          {attempts.map((attempt) => {
            const submittedDate = new Date(attempt.submittedAt)
            const percentage = attempt.percentageScore

            return (
              <Card key={attempt.id}>
                <CardHeader>
                  <div className="flex justify-between items-start gap-4">
                    <div className="flex-1">
                      <CardTitle className="text-lg mb-2">{attempt.quizTitle}</CardTitle>
                      <div className="flex flex-wrap items-center gap-4 text-sm text-gray-500">
                        <div className="flex items-center gap-1">
                          <Calendar className="w-4 h-4" />
                          <span>{submittedDate.toLocaleDateString()}</span>
                          <span className="text-gray-400">
                            {submittedDate.toLocaleTimeString([], { 
                              hour: '2-digit', 
                              minute: '2-digit' 
                            })}
                          </span>
                        </div>
                        <span>•</span>
                        <span>{attempt.userName}</span>
                        <span>•</span>
                        <span>{attempt.questionCount} {t('cyberpanel.questions')}</span>
                      </div>
                    </div>

                    <Button
                      onClick={() => handleViewQuiz(attempt.id)}
                      variant="outline"
                      size="sm"
                      title={t('cyberpanel.viewDetails')}
                    >
                      <ArrowRight className="w-4 h-4" />
                    </Button>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                    <div className={`text-center p-4 rounded-lg border-2 ${getScoreBgColor(percentage)}`}>
                      <p className={`text-2xl font-bold ${getScoreColor(percentage)}`}>
                        {percentage.toFixed(1)}%
                      </p>
                      <p className="text-sm text-gray-600">{t('cyberpanel.score')}</p>
                    </div>

                    <div className="text-center p-4 bg-gray-50 rounded-lg">
                      <p className="text-2xl font-bold text-gray-700">
                        {attempt.score}/{attempt.maxScore}
                      </p>
                      <p className="text-sm text-gray-600">{t('cyberpanel.points')}</p>
                    </div>

                    <div className="text-center p-4 bg-green-50 rounded-lg">
                      <div className="flex items-center justify-center gap-1 mb-1">
                        <CheckCircle className="w-5 h-5 text-green-600" />
                        <p className="text-2xl font-bold text-green-600">{attempt.correctAnswers}</p>
                      </div>
                      <p className="text-sm text-gray-600">{t('cyberpanel.correct')}</p>
                    </div>

                    <div className="text-center p-4 bg-red-50 rounded-lg">
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
            )
          })}
        </div>
      )}
    </div>
  )
}

