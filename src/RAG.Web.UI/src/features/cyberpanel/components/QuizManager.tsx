import React, { useState, useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { useI18n } from '@/shared/contexts/I18nContext'
import { useToast } from '@/shared/contexts'
import { useAuth } from '@/shared/contexts/AuthContext'
import { useQuizzes } from '@/features/cyberpanel/hooks/useQuizzes'
import { Button } from '@/shared/components/ui/Button'
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/Card'
import { DeleteConfirmationModal } from '@/shared/components/common/DeleteConfirmationModal'
import { QuizBuilder } from './QuizBuilder'
import { Plus, Edit, Trash2, Eye, EyeOff, Calendar, FileText, Upload } from 'lucide-react'
import type { QuizListItem } from '@/features/cyberpanel/types/quiz'

export function QuizManager() {
  const { t } = useI18n()
  const navigate = useNavigate()
  const { showSuccess, showError } = useToast()
  const { user } = useAuth()
  const {
    quizzes,
    loading,
    error,
    fetchQuizzes,
    deleteQuiz: deleteQuizHook,
    importFromFile
  } = useQuizzes()

  const [showBuilder, setShowBuilder] = useState(false)
  const [editQuizId, setEditQuizId] = useState<string | null>(null)
  const [deleteModal, setDeleteModal] = useState<{
    isOpen: boolean
    quiz: QuizListItem | null
  }>({
    isOpen: false,
    quiz: null,
  })
  const [deleting, setDeleting] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)

  // Check authorization
  const isAdminOrPowerUser = user?.roles?.includes('Admin') || user?.roles?.includes('PowerUser')

  useEffect(() => {
    if (!isAdminOrPowerUser) {
      navigate('/cyberpanel/quizzes')
      return
    }
    fetchQuizzes()
  }, [fetchQuizzes, isAdminOrPowerUser, navigate])

  const handleNewQuiz = () => {
    setEditQuizId(null)
    setShowBuilder(true)
  }

  const handleEditQuiz = (quizId: string) => {
    setEditQuizId(quizId)
    setShowBuilder(true)
  }

  const handleDeleteClick = (quiz: QuizListItem) => {
    setDeleteModal({
      isOpen: true,
      quiz,
    })
  }

  const handleDeleteConfirm = async () => {
    if (!deleteModal.quiz) return

    setDeleting(true)
    try {
      const success = await deleteQuizHook(deleteModal.quiz.id)
      if (success) {
        showSuccess('Quiz deleted successfully')
        fetchQuizzes()
      } else {
        showError('Failed to delete quiz')
      }
      setDeleteModal({ isOpen: false, quiz: null })
    } catch {
      showError('Failed to delete quiz')
    } finally {
      setDeleting(false)
    }
  }

  const handleDeleteCancel = () => {
    setDeleteModal({ isOpen: false, quiz: null })
  }

  const handleBuilderClose = () => {
    setShowBuilder(false)
    setEditQuizId(null)
    fetchQuizzes()
  }

  const handleImportClick = () => {
    fileInputRef.current?.click()
  }

  const handleFileSelected = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    if (!file) return

    try {
      const result = await importFromFile(file)
      if (result) {
        showSuccess(t('cyberpanel.importSuccess'))
        fetchQuizzes() // Refresh the quiz list
      } else {
        showError(t('cyberpanel.importError'))
      }
    } catch {
      showError(t('cyberpanel.importError'))
    }

    // Reset file input
    if (fileInputRef.current) {
      fileInputRef.current.value = ''
    }
  }

  if (!isAdminOrPowerUser) {
    return null
  }

  if (showBuilder) {
    return (
      <div className="max-w-6xl">
        <QuizBuilder
          editQuizId={editQuizId}
          onSave={handleBuilderClose}
          onCancel={handleBuilderClose}
        />
      </div>
    )
  }

  return (
    <div className="max-w-6xl">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-6">
        <div>
          <h3 className="text-xl md:text-2xl font-bold">Quiz Manager</h3>
          <p className="text-sm text-gray-600 mt-1">Manage all quizzes - create, edit, delete</p>
        </div>
        
        <div className="flex gap-2">
          <Button onClick={handleImportClick} variant="outline" size="sm">
            <Upload className="w-4 h-4 mr-2" />
            {t('cyberpanel.importQuiz')}
          </Button>
          <Button onClick={handleNewQuiz} variant="primary" size="sm">
            <Plus className="w-4 h-4 mr-2" />
            {t('cyberpanel.newQuiz')}
          </Button>
        </div>
      </div>

      <input
        ref={fileInputRef}
        type="file"
        accept=".json,application/json"
        onChange={handleFileSelected}
        className="hidden"
        aria-label={t('cyberpanel.selectJsonFile')}
      />

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

      {quizzes && quizzes.quizzes.length === 0 && (
        <Card>
          <CardContent className="py-12 text-center">
            <FileText className="w-16 h-16 text-gray-400 mx-auto mb-4" />
            <p className="text-gray-600 text-lg mb-2">{t('cyberpanel.noQuizzesYet')}</p>
            <p className="text-gray-500 text-sm mb-6">{t('cyberpanel.createFirstQuiz')}</p>
            <Button onClick={handleNewQuiz} variant="primary">
              <Plus className="w-4 h-4 mr-2" />
              {t('cyberpanel.newQuiz')}
            </Button>
          </CardContent>
        </Card>
      )}

      {quizzes && quizzes.quizzes.length > 0 && (
        <div className="space-y-4">
          {quizzes.quizzes.map((quiz: QuizListItem) => {
            const createdDate = new Date(quiz.createdAt)
            
            return (
              <Card key={quiz.id}>
                <CardHeader>
                  <div className="flex justify-between items-start gap-4">
                    <div className="flex-1">
                      <div className="flex items-center gap-3 mb-2">
                        <CardTitle className="text-lg">{quiz.title}</CardTitle>
                        {quiz.isPublished ? (
                          <span className="inline-flex items-center gap-1 px-2 py-1 bg-green-100 text-green-700 text-xs font-medium rounded-full">
                            <Eye className="w-3 h-3" />
                            Published
                          </span>
                        ) : (
                          <span className="inline-flex items-center gap-1 px-2 py-1 bg-gray-100 text-gray-600 text-xs font-medium rounded-full">
                            <EyeOff className="w-3 h-3" />
                            Draft
                          </span>
                        )}
                        <span>{quiz.language ? quiz.language.toUpperCase() : 'N/A'}</span>
                      </div>
                      
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

                    <div className="flex gap-2">
                      <Button
                        onClick={() => handleEditQuiz(quiz.id)}
                        variant="outline"
                        size="sm"
                        title="Edit quiz"
                      >
                        <Edit className="w-4 h-4" />
                      </Button>
                      <Button
                        onClick={() => handleDeleteClick(quiz)}
                        variant="outline"
                        size="sm"
                        title="Delete quiz"
                        className="text-red-600 hover:text-red-700 hover:bg-red-50"
                      >
                        <Trash2 className="w-4 h-4" />
                      </Button>
                    </div>
                  </div>
                </CardHeader>
              </Card>
            )
          })}
        </div>
      )}

      <DeleteConfirmationModal
        isOpen={deleteModal.isOpen}
        onClose={handleDeleteCancel}
        onConfirm={handleDeleteConfirm}
        title="Delete Quiz"
        message="Are you sure you want to delete this quiz? This will permanently remove the quiz and all associated attempts."
        itemName={deleteModal.quiz?.title || ''}
        details={deleteModal.quiz ? [
          { label: 'Questions', value: deleteModal.quiz.questionCount },
          { label: 'Status', value: deleteModal.quiz.isPublished ? 'Published' : 'Draft' },
        ] : []}
        isLoading={deleting}
        confirmText="Delete"
        cancelText="Cancel"
        deletingText="Deleting..."
        warningText="This action cannot be undone. All quiz attempts will also be deleted."
      />
    </div>
  )
}
