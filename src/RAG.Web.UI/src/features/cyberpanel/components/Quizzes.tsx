import React, { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useI18n } from '@/shared/contexts/I18nContext'
import { useQuizzes } from '@/features/cyberpanel/hooks/useQuizzes'
import { Button } from '@/shared/components/ui/Button'
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/Card'
import { Play, FileText, Calendar } from 'lucide-react'
import type { QuizListItem } from '@/features/cyberpanel/types/quiz'

export function Quizzes() {
  const { t, language } = useI18n()
  const navigate = useNavigate()
  const {
    quizzes,
    loading,
    error,
    fetchQuizzes
  } = useQuizzes()

  useEffect(() => {
    fetchQuizzes(language)
  }, [fetchQuizzes, language])

  const handleStartQuiz = (quizId: string) => {
    navigate(`/cyberpanel/quizzes/${quizId}`)
  }

  // Filter only published quizzes
  const publishedQuizzes = quizzes?.quizzes.filter((q: QuizListItem) => q.isPublished) || []


  return (
    <div className="max-w-6xl">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-6">
        <div>
          <h3 className="text-xl md:text-2xl font-bold">{t('cyberpanel.quizzes')}</h3>
          <p className="text-sm text-gray-600 mt-1">Take published quizzes</p>
        </div>
      </div>

      {loading && !quizzes && (
        <div className="text-center py-8">
          <p className="text-gray-600">{t('common.loading')}</p>
        </div>
      )}

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-4">
          <p className="text-red-800">{error}</p>
        </div>
      )}

      {publishedQuizzes.length === 0 && !loading && (
        <Card>
          <CardContent className="py-12 text-center">
            <FileText className="w-16 h-16 text-gray-400 mx-auto mb-4" />
            <p className="text-gray-600 text-lg mb-2">No quizzes available</p>
            <p className="text-gray-500 text-sm">Check back later for new quizzes</p>
          </CardContent>
        </Card>
      )}

      {publishedQuizzes.length > 0 && (
        <div className="space-y-4">
          {publishedQuizzes.map((quiz: QuizListItem) => {
            const createdDate = new Date(quiz.createdAt)
            
            return (
              <Card key={quiz.id}>
                <CardHeader>
                  <div className="flex justify-between items-start gap-4">
                    <div className="flex-1">
                      <CardTitle className="text-lg mb-2">{quiz.title}</CardTitle>
                      {quiz.description && (
                        <p className="text-sm text-gray-600 mb-3">{quiz.description}</p>
                      )}
                      <div className="flex flex-wrap items-center gap-4 text-sm text-gray-500">
                        <div className="flex items-center gap-1">
                          <Calendar className="w-4 h-4" />
                          <span>{createdDate.toLocaleDateString()}</span>
                        </div>
                        <span>•</span>
                        <span>{quiz.questionCount} {t('cyberpanel.questions')}</span>
                      </div>
                    </div>

                    <Button
                      onClick={() => handleStartQuiz(quiz.id)}
                      variant="primary"
                      size="sm"
                      title="Start quiz"
                    >
                      <Play className="w-4 h-4 mr-2" />
                      Start Quiz
                    </Button>
                  </div>
                </CardHeader>
              </Card>
            )
          })}
        </div>
      )}
    </div>
  )
}
