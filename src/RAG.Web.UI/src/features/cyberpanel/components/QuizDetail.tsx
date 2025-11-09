import React, { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useI18n } from '@/shared/contexts/I18nContext'
import { useToast } from '@/shared/contexts'
import { useQuizTaking } from '@/features/cyberpanel/hooks/useQuizzes'
import { Button } from '@/shared/components/ui/Button'
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/Card'
import { ArrowLeft, CheckCircle, XCircle, AlertCircle } from 'lucide-react'
import type { SubmitAttemptResponse } from '@/features/cyberpanel/types/quiz'

export function QuizDetail() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { t } = useI18n()
  const { showError } = useToast()
  const { quiz, loading, error, fetchQuiz, submitAttempt } = useQuizTaking()

  const [started, setStarted] = useState(false)
  const [answers, setAnswers] = useState<Record<string, string[]>>({})
  const [submitting, setSubmitting] = useState(false)
  const [result, setResult] = useState<SubmitAttemptResponse | null>(null)

  useEffect(() => {
    if (id) {
      fetchQuiz(id)
    }
  }, [id, fetchQuiz])

  const handleStartQuiz = () => {
    setStarted(true)
    setAnswers({})
    setResult(null)
  }

  const handleOptionToggle = (questionId: string, optionId: string) => {
    setAnswers((prev) => {
      const currentAnswers = prev[questionId] || []
      const isSelected = currentAnswers.includes(optionId)

      if (isSelected) {
        return {
          ...prev,
          [questionId]: currentAnswers.filter((id) => id !== optionId)
        }
      } else {
        return {
          ...prev,
          [questionId]: [...currentAnswers, optionId]
        }
      }
    })
  }

  const handleSubmit = async () => {
    if (!quiz || !id) return

    // Validate all questions have answers
    const unansweredQuestions = quiz.questions.filter(
      (q) => !answers[q.id] || answers[q.id].length === 0
    )

    if (unansweredQuestions.length > 0) {
      showError(t('cyberpanel.pleaseSelectAnswer'))
      return
    }

    setSubmitting(true)
    try {
      const submitResult = await submitAttempt({
        quizId: id,
        answers: Object.entries(answers).map(([questionId, selectedOptionIds]) => ({
          questionId,
          selectedOptionIds
        }))
      })

      if (submitResult) {
        setResult(submitResult)
      }
    } catch {
      showError(t('common.error'))
    } finally {
      setSubmitting(false)
    }
  }

  const handleRetake = () => {
    setStarted(false)
    setAnswers({})
    setResult(null)
  }

  const handleBackToQuizzes = () => {
    navigate('/cyberpanel/quizzes')
  }

  if (loading) {
    return (
      <div className="max-w-4xl text-gray-900 dark:text-gray-100">
        <div className="flex items-center gap-2 mb-6">
          <Button onClick={handleBackToQuizzes} variant="outline" size="sm">
            <ArrowLeft className="w-4 h-4 mr-2" />
            {t('cyberpanel.backToQuizzes')}
          </Button>
        </div>
        <Card>
          <CardContent className="py-12 text-center">
            <p className="text-gray-600 dark:text-gray-400">{t('cyberpanel.loadingQuiz')}</p>
          </CardContent>
        </Card>
      </div>
    )
  }

  if (error || !quiz) {
    return (
      <div className="max-w-4xl text-gray-900 dark:text-gray-100">
        <div className="flex items-center gap-2 mb-6">
          <Button onClick={handleBackToQuizzes} variant="outline" size="sm">
            <ArrowLeft className="w-4 h-4 mr-2" />
            {t('cyberpanel.backToQuizzes')}
          </Button>
        </div>
        <Card>
          <CardContent className="py-12 text-center">
            <AlertCircle className="w-12 h-12 text-red-500 dark:text-red-300 mx-auto mb-4" />
            <p className="text-red-600 dark:text-red-300 text-lg">{error || t('cyberpanel.quizNotFound')}</p>
          </CardContent>
        </Card>
      </div>
    )
  }

  // Results view
  if (result) {
    const correctCount = result.perQuestionResults.filter((a) => a.correct).length
    const incorrectCount = result.perQuestionResults.filter((a) => !a.correct).length
    const percentageScore = (result.score / result.maxScore) * 100

    return (
      <div className="max-w-4xl text-gray-900 dark:text-gray-100">
        <div className="flex items-center gap-2 mb-6">
          <Button onClick={handleBackToQuizzes} variant="outline" size="sm">
            <ArrowLeft className="w-4 h-4 mr-2" />
            {t('cyberpanel.backToQuizzes')}
          </Button>
        </div>

        <Card className="mb-6">
          <CardHeader>
            <CardTitle>{t('cyberpanel.quizResults')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
              <div className="text-center p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg border border-blue-200 dark:border-blue-500/50">
                <p className="text-2xl font-bold text-blue-600 dark:text-blue-300">{percentageScore.toFixed(1)}%</p>
                <p className="text-sm text-gray-600 dark:text-gray-300">{t('cyberpanel.percentage')}</p>
              </div>
              <div className="text-center p-4 bg-green-50 dark:bg-green-900/20 rounded-lg border border-green-200 dark:border-green-500/50">
                <p className="text-2xl font-bold text-green-600 dark:text-green-300">{correctCount}</p>
                <p className="text-sm text-gray-600 dark:text-gray-300">{t('cyberpanel.correctAnswers')}</p>
              </div>
              <div className="text-center p-4 bg-red-50 dark:bg-red-900/20 rounded-lg border border-red-200 dark:border-red-600/60">
                <p className="text-2xl font-bold text-red-600 dark:text-red-300">{incorrectCount}</p>
                <p className="text-sm text-gray-600 dark:text-gray-300">{t('cyberpanel.incorrectAnswers')}</p>
              </div>
              <div className="text-center p-4 bg-purple-50 dark:bg-purple-900/20 rounded-lg border border-purple-200 dark:border-purple-500/50">
                <p className="text-2xl font-bold text-purple-600 dark:text-purple-300">
                  {result.score}/{result.maxScore}
                </p>
                <p className="text-sm text-gray-600 dark:text-gray-300">{t('cyberpanel.earnedPoints')}</p>
              </div>
            </div>

            <div className="flex gap-2">
              <Button onClick={handleRetake} variant="primary">
                {t('cyberpanel.retakeQuiz')}
              </Button>
              <Button onClick={handleBackToQuizzes} variant="outline">
                {t('cyberpanel.backToQuizzes')}
              </Button>
            </div>
          </CardContent>
        </Card>

        {/* Answer details */}
        <div className="space-y-4">
          {quiz.questions.map((question, qIdx) => {
            const answerResult = result.perQuestionResults.find((a) => a.questionId === question.id)
            const userAnswers = answers[question.id] || []
            if (!answerResult) return null

            return (
              <Card key={question.id}>
                <CardHeader>
                  <div className="flex items-start gap-3">
                    {answerResult.correct ? (
                      <CheckCircle className="w-6 h-6 text-green-600 dark:text-green-300 flex-shrink-0 mt-1" />
                    ) : (
                      <XCircle className="w-6 h-6 text-red-600 dark:text-red-300 flex-shrink-0 mt-1" />
                    )}
                    <div className="flex-1">
                      <CardTitle className="text-lg mb-2">
                        {t('cyberpanel.questionNumber', [(qIdx + 1).toString()])}
                      </CardTitle>
                      <p className="text-gray-700 dark:text-gray-300 mb-2">{question.text}</p>
                      {question.imageUrl && (
                        <img
                          src={question.imageUrl}
                          alt="Question"
                          className="max-w-full h-auto rounded-lg mb-2"
                        />
                      )}
                      <div className="flex items-center gap-4 text-sm">
                        <span className={answerResult.correct ? 'text-green-600 dark:text-green-300' : 'text-red-600 dark:text-red-300'}>
                          {answerResult.correct ? t('cyberpanel.correct') : t('cyberpanel.incorrect')}
                        </span>
                        <span className="text-gray-600 dark:text-gray-400">
                          {answerResult.pointsAwarded}/{answerResult.maxPoints} {t('cyberpanel.points')}
                        </span>
                      </div>
                    </div>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="space-y-2">
                    {question.options.map((option) => {
                      const wasSelected = userAnswers.includes(option.id)
                      
                      return (
                        <div
                          key={option.id}
                          className={`p-3 rounded-lg border-2 transition-colors ${
                            wasSelected
                              ? answerResult.correct
                                ? 'border-green-500 bg-green-50 dark:border-green-500/50 dark:bg-green-900/20'
                                : 'border-red-500 bg-red-50 dark:border-red-600/60 dark:bg-red-900/20'
                              : 'border-gray-200 bg-gray-50 dark:border-slate-700 dark:bg-slate-900'
                          }`}
                        >
                          <div className="flex items-start gap-3">
                            <div className="flex-1">
                              <p className="text-gray-800 dark:text-gray-200">{option.text}</p>
                              {option.imageUrl && (
                                <img
                                  src={option.imageUrl}
                                  alt="Option"
                                  className="max-w-xs h-auto rounded-lg mt-2"
                                />
                              )}
                            </div>
                            {wasSelected && (
                              <span className="text-xs font-medium px-2 py-1 rounded bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300">
                                {t('cyberpanel.yourAnswers')}
                              </span>
                            )}
                          </div>
                        </div>
                      )
                    })}
                  </div>
                </CardContent>
              </Card>
            )
          })}
        </div>
      </div>
    )
  }

  // Quiz intro view (before starting)
  if (!started) {
    return (
      <div className="max-w-4xl text-gray-900 dark:text-gray-100">
        <div className="flex items-center gap-2 mb-6">
          <Button onClick={handleBackToQuizzes} variant="outline" size="sm">
            <ArrowLeft className="w-4 h-4 mr-2" />
            {t('cyberpanel.backToQuizzes')}
          </Button>
        </div>

        <Card>
          <CardHeader>
            <CardTitle className="text-2xl">{quiz.title}</CardTitle>
          </CardHeader>
          <CardContent>
            {quiz.description && (
              <p className="text-gray-700 dark:text-gray-300 mb-6">{quiz.description}</p>
            )}

            <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-500/50 rounded-lg p-4 mb-6">
              <h4 className="font-semibold text-blue-900 dark:text-blue-200 mb-2">Quiz Information</h4>
              <ul className="space-y-1 text-sm text-blue-800 dark:text-blue-200">
                <li>• {quiz.questions.length} {t('cyberpanel.questions')}</li>
                <li>• {quiz.questions.reduce((sum, q) => sum + q.points, 0)} {t('cyberpanel.totalPoints')}</li>
              </ul>
            </div>

            <Button onClick={handleStartQuiz} variant="primary" size="lg">
              {t('cyberpanel.startQuiz')}
            </Button>
          </CardContent>
        </Card>
      </div>
    )
  }

  // Quiz taking view
  return (
    <div className="max-w-4xl text-gray-900 dark:text-gray-100">
      <div className="flex items-center justify-between mb-6">
        <Button onClick={handleBackToQuizzes} variant="outline" size="sm">
          <ArrowLeft className="w-4 h-4 mr-2" />
          {t('cyberpanel.backToQuizzes')}
        </Button>
        <h3 className="text-xl font-bold">{quiz.title}</h3>
      </div>

      <div className="space-y-6">
        {quiz.questions.map((question, qIdx) => (
          <Card key={question.id}>
            <CardHeader>
              <CardTitle className="text-lg">
                {t('cyberpanel.questionNumber', [(qIdx + 1).toString()])} ({question.points} {t('cyberpanel.points')})
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-gray-800 dark:text-gray-200 mb-4">{question.text}</p>
              {question.imageUrl && (
                <img
                  src={question.imageUrl}
                  alt="Question"
                  className="max-w-full h-auto rounded-lg mb-4"
                />
              )}

              <p className="text-sm text-gray-600 dark:text-gray-400 mb-3">{t('cyberpanel.selectAnswers')}</p>

              <div className="space-y-2">
                {question.options.map((option) => {
                  const isSelected = answers[question.id]?.includes(option.id) || false

                  return (
                    <label
                      key={option.id}
                      className={`block p-3 rounded-lg border-2 cursor-pointer transition-all ${
                        isSelected
                          ? 'border-blue-500 bg-blue-50 dark:border-blue-500/50 dark:bg-blue-900/20'
                          : 'border-gray-300 dark:border-slate-700 bg-white dark:bg-slate-900 hover:border-blue-300 hover:bg-blue-50 dark:hover:border-blue-500/50 dark:hover:bg-blue-900/20'
                      }`}
                    >
                      <div className="flex items-start gap-3">
                        <input
                          type="checkbox"
                          checked={isSelected}
                          onChange={() => handleOptionToggle(question.id, option.id)}
                          className="form-checkbox h-5 w-9"
                          aria-label={option.text}
                        />
                        <div className="flex-1">
                          <p className="text-gray-800 dark:text-gray-200">{option.text}</p>
                          {option.imageUrl && (
                            <img
                              src={option.imageUrl}
                              alt="Option"
                              className="max-w-xs h-auto rounded-lg mt-2"
                            />
                          )}
                        </div>
                      </div>
                    </label>
                  )
                })}
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      <Card className="mt-6">
        <CardContent className="py-6">
          <div className="flex justify-center">
            <Button
              onClick={handleSubmit}
              disabled={submitting}
              variant="primary"
              size="lg"
            >
              {submitting ? t('cyberpanel.submitting') : t('cyberpanel.submitAnswers')}
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
