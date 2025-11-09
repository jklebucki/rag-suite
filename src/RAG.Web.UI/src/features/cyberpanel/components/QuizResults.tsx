import React, { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useI18n } from '@/shared/contexts/I18nContext'
import { useAuth } from '@/shared/contexts/AuthContext'
import cyberPanelService from '@/features/cyberpanel/services/cyberPanel.service'
import { Button } from '@/shared/components/ui/Button'
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/Card'
import { Trophy, Calendar, CheckCircle, XCircle, ArrowRight, Trash2 } from 'lucide-react'
import { DeleteConfirmationModal } from '@/shared/components/common/DeleteConfirmationModal'
import type { QuizAttemptDto } from '@/features/cyberpanel/types/quiz'

export function QuizResults() {
  const { t } = useI18n()
  const { user } = useAuth()
  const navigate = useNavigate()
  const [attempts, setAttempts] = useState<QuizAttemptDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [deleteModal, setDeleteModal] = useState<{
    isOpen: boolean
    attemptId: string | null
    quizTitle: string
    userName: string
  }>({
    isOpen: false,
    attemptId: null,
    quizTitle: '',
    userName: '',
  })
  const [deleting, setDeleting] = useState(false)

  const isAdminOrPowerUser = user?.roles?.includes('Admin') || user?.roles?.includes('PowerUser')

  useEffect(() => {
    fetchAttempts()
  }, [])

  const fetchAttempts = async () => {
    setLoading(true)
    setError(null)
    try {
      const response = await cyberPanelService.listQuizAttempts()
      setAttempts(response.attempts)
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to load quiz results')
    } finally {
      setLoading(false)
    }
  }

  const handleViewQuiz = (attemptId: string) => {
    navigate(`/cyberpanel/attempts/${attemptId}`)
  }

  const handleDeleteClick = (attempt: QuizAttemptDto) => {
    setDeleteModal({
      isOpen: true,
      attemptId: attempt.id,
      quizTitle: attempt.quizTitle,
      userName: attempt.userName,
    })
  }

  const handleDeleteConfirm = async () => {
    if (!deleteModal.attemptId) return

    setDeleting(true)
    try {
      await cyberPanelService.deleteAttempt(deleteModal.attemptId)
      setAttempts(attempts.filter((a) => a.id !== deleteModal.attemptId))
      setDeleteModal({ isOpen: false, attemptId: null, quizTitle: '', userName: '' })
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to delete attempt')
    } finally {
      setDeleting(false)
    }
  }

  const handleDeleteCancel = () => {
    setDeleteModal({ isOpen: false, attemptId: null, quizTitle: '', userName: '' })
  }

  const getScoreColor = (percentage: number) => {
    if (percentage >= 80) return 'text-green-600 dark:text-green-300'
    if (percentage >= 60) return 'text-yellow-600 dark:text-yellow-300'
    return 'text-red-600 dark:text-red-300'
  }

  const getScoreBgColor = (percentage: number) => {
    if (percentage >= 80) return 'bg-green-50 border-green-200 dark:bg-green-900/20 dark:border-green-500/50'
    if (percentage >= 60) return 'bg-yellow-50 border-yellow-200 dark:bg-yellow-900/20 dark:border-yellow-500/50'
    return 'bg-red-50 border-red-200 dark:bg-red-900/20 dark:border-red-600/60'
  }

  if (loading) {
    return (
      <div className="max-w-6xl text-gray-900 dark:text-gray-100">
        <div className="text-center py-8">
          <p className="text-gray-600 dark:text-gray-400">{t('common.loading')}</p>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="max-w-6xl text-gray-900 dark:text-gray-100">
        <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-700 rounded-2xl p-4 mb-4">
          <p className="text-red-800 dark:text-red-300">{error}</p>
        </div>
      </div>
    )
  }

  return (
    <div className="max-w-6xl text-gray-900 dark:text-gray-100">
      <div className="flex justify-between items-center mb-6">
        <h3 className="text-xl md:text-2xl font-bold">{t('cyberpanel.results')}</h3>
      </div>

      {attempts.length === 0 && (
        <Card>
          <CardContent className="py-12 text-center">
            <Trophy className="w-16 h-16 text-gray-400 dark:text-slate-600 mx-auto mb-4" />
            <p className="text-gray-600 dark:text-gray-300 text-lg mb-2">{t('cyberpanel.noResultsYet')}</p>
            <p className="text-gray-500 dark:text-gray-400 text-sm mb-6">{t('cyberpanel.takeFirstQuiz')}</p>
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
                      <div className="flex flex-wrap items-center gap-4 text-sm text-gray-500 dark:text-gray-400">
                        <div className="flex items-center gap-1">
                          <Calendar className="w-4 h-4 text-gray-400 dark:text-gray-500" />
                          <span>{submittedDate.toLocaleDateString()}</span>
                          <span className="text-gray-400 dark:text-gray-500">
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

                    <div className="flex gap-2">
                      <Button
                        onClick={() => handleViewQuiz(attempt.id)}
                        variant="outline"
                        size="sm"
                        title={t('cyberpanel.viewDetails')}
                      >
                        <ArrowRight className="w-4 h-4" />
                      </Button>
                      {isAdminOrPowerUser && (
                        <Button
                          onClick={() => handleDeleteClick(attempt)}
                          variant="outline"
                          size="sm"
                          title="Delete attempt"
                          className="text-red-600 hover:text-red-700 hover:bg-red-50 dark:text-red-400 dark:hover:text-red-300 dark:hover:bg-red-900/30"
                        >
                          <Trash2 className="w-4 h-4" />
                        </Button>
                      )}
                    </div>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                    <div className={`text-center p-4 rounded-lg border-2 ${getScoreBgColor(percentage)}`}>
                      <p className={`text-2xl font-bold ${getScoreColor(percentage)}`}>
                        {percentage.toFixed(1)}%
                      </p>
                      <p className="text-sm text-gray-600 dark:text-gray-300">{t('cyberpanel.score')}</p>
                    </div>

                    <div className="text-center p-4 bg-gray-50 dark:bg-slate-900 rounded-lg border border-gray-200 dark:border-slate-800">
                      <p className="text-2xl font-bold text-gray-700 dark:text-gray-200">
                        {attempt.score}/{attempt.maxScore}
                      </p>
                      <p className="text-sm text-gray-600 dark:text-gray-300">{t('cyberpanel.points')}</p>
                    </div>

                    <div className="text-center p-4 bg-green-50 dark:bg-green-900/20 rounded-lg border border-green-200 dark:border-green-500/50">
                      <div className="flex items-center justify-center gap-1 mb-1">
                        <CheckCircle className="w-5 h-5 text-green-600 dark:text-green-300" />
                        <p className="text-2xl font-bold text-green-600 dark:text-green-300">{attempt.correctAnswers}</p>
                      </div>
                      <p className="text-sm text-gray-600 dark:text-gray-300">{t('cyberpanel.correct')}</p>
                    </div>

                    <div className="text-center p-4 bg-red-50 dark:bg-red-900/20 rounded-lg border border-red-200 dark:border-red-600/60">
                      <div className="flex items-center justify-center gap-1 mb-1">
                        <XCircle className="w-5 h-5 text-red-600 dark:text-red-300" />
                        <p className="text-2xl font-bold text-red-600 dark:text-red-300">
                          {attempt.questionCount - attempt.correctAnswers}
                        </p>
                      </div>
                      <p className="text-sm text-gray-600 dark:text-gray-300">{t('cyberpanel.incorrect')}</p>
                    </div>
                  </div>
                </CardContent>
              </Card>
            )
          })}
        </div>
      )}

      <DeleteConfirmationModal
        isOpen={deleteModal.isOpen}
        onClose={handleDeleteCancel}
        onConfirm={handleDeleteConfirm}
        title="Delete Quiz Attempt"
        message="Are you sure you want to delete this quiz attempt? This will permanently remove the result."
        itemName={deleteModal.quizTitle}
        details={[
          { label: 'User', value: deleteModal.userName },
          { label: 'Quiz', value: deleteModal.quizTitle },
        ]}
        isLoading={deleting}
        confirmText="Delete"
        cancelText="Cancel"
        deletingText="Deleting..."
        warningText="This action cannot be undone"
      />
    </div>
  )
}
