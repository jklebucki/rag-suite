import React, { useState, useEffect, useRef } from 'react'
import { useI18n } from '@/contexts/I18nContext'
import { useToast } from '@/contexts'
import { useQuizzes } from '@/hooks/useQuizzes'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { ConfirmModal } from '@/components/ui'
import QuizBuilder from './QuizBuilder'
import { Download, Upload, Plus, Edit, Trash2, Eye } from 'lucide-react'
import type { QuizListItem } from '@/types'

export default function Quizzes() {
  const { t } = useI18n()
  const { showSuccess, showError } = useToast()
  const {
    quizzes,
    loading,
    error,
    fetchQuizzes,
    deleteQuiz,
    exportQuiz,
    importFromFile
  } = useQuizzes()

  const [showBuilder, setShowBuilder] = useState(false)
  const [editQuizId, setEditQuizId] = useState<string | null>(null)
  const [deleteConfirmQuiz, setDeleteConfirmQuiz] = useState<QuizListItem | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    fetchQuizzes()
  }, [fetchQuizzes])

  const handleNewQuiz = () => {
    setEditQuizId(null)
    setShowBuilder(true)
  }

  const handleEditQuiz = (quizId: string) => {
    setEditQuizId(quizId)
    setShowBuilder(true)
  }

  const handleDeleteQuiz = async () => {
    if (!deleteConfirmQuiz) return

    const success = await deleteQuiz(deleteConfirmQuiz.id)
    if (success) {
      showSuccess(t('cyberpanel.deleteSuccess'))
    } else {
      showError(t('cyberpanel.deleteError'))
    }
    setDeleteConfirmQuiz(null)
  }

  const handleExportQuiz = async (quizId: string, title: string) => {
    const result = await exportQuiz(quizId)
    if (result) {
      // Trigger download
      const blob = new Blob([JSON.stringify(result, null, 2)], { type: 'application/json' })
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `${title.replace(/[^a-z0-9]/gi, '_').toLowerCase()}_${new Date().toISOString().split('T')[0]}.json`
      document.body.appendChild(a)
      a.click()
      document.body.removeChild(a)
      URL.revokeObjectURL(url)
      showSuccess(t('cyberpanel.exportSuccess'))
    } else {
      showError(t('cyberpanel.importError'))
    }
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
      } else {
        showError(t('cyberpanel.importError'))
      }
    } catch (err) {
      showError(t('cyberpanel.importError'))
    }

    // Reset file input
    if (fileInputRef.current) {
      fileInputRef.current.value = ''
    }
  }

  const handleBuilderClose = () => {
    setShowBuilder(false)
    setEditQuizId(null)
    fetchQuizzes()
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
        <h3 className="text-xl md:text-2xl font-bold">{t('cyberpanel.quizzes')}</h3>
        
        <div className="flex gap-2 flex-wrap">
          <Button onClick={handleImportClick} variant="outline" size="sm">
            <Upload className="w-4 h-4 mr-2" />
            {t('cyberpanel.importQuiz')}
          </Button>
          <Button onClick={handleNewQuiz} variant="primary" size="sm">
            <Plus className="w-4 h-4 mr-2" />
            {t('cyberpanel.newQuiz')}
          </Button>
        </div>

        <input
          ref={fileInputRef}
          type="file"
          accept=".json,application/json"
          onChange={handleFileSelected}
          className="hidden"
          aria-label={t('cyberpanel.selectJsonFile')}
        />
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

      {quizzes && quizzes.quizzes.length === 0 && (
        <Card>
          <CardContent className="py-12 text-center">
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
          {quizzes.quizzes.map((quiz) => (
            <Card key={quiz.id}>
              <CardHeader>
                <div className="flex justify-between items-start gap-4">
                  <div className="flex-1">
                    <CardTitle className="text-lg mb-2">{quiz.title}</CardTitle>
                    {quiz.description && (
                      <p className="text-sm text-gray-600 mb-2">{quiz.description}</p>
                    )}
                    <div className="flex items-center gap-3 text-sm text-gray-500">
                      <span>{t('cyberpanel.questionsCount', [quiz.questionCount.toString()])}</span>
                      <span>•</span>
                      <span className={quiz.isPublished ? 'text-green-600' : 'text-yellow-600'}>
                        {quiz.isPublished ? t('cyberpanel.published') : t('cyberpanel.draft')}
                      </span>
                      <span>•</span>
                      <span>{new Date(quiz.createdAt).toLocaleDateString()}</span>
                    </div>
                  </div>

                  <div className="flex gap-2">
                    <Button
                      onClick={() => handleExportQuiz(quiz.id, quiz.title)}
                      variant="outline"
                      size="sm"
                      title={t('cyberpanel.exportQuiz')}
                    >
                      <Download className="w-4 h-4" />
                    </Button>
                    <Button
                      onClick={() => handleEditQuiz(quiz.id)}
                      variant="outline"
                      size="sm"
                      title={t('cyberpanel.edit')}
                    >
                      <Edit className="w-4 h-4" />
                    </Button>
                    <Button
                      onClick={() => setDeleteConfirmQuiz(quiz)}
                      variant="destructive"
                      size="sm"
                      title={t('cyberpanel.delete')}
                    >
                      <Trash2 className="w-4 h-4" />
                    </Button>
                  </div>
                </div>
              </CardHeader>
            </Card>
          ))}
        </div>
      )}

      <ConfirmModal
        isOpen={!!deleteConfirmQuiz}
        onClose={() => setDeleteConfirmQuiz(null)}
        onConfirm={handleDeleteQuiz}
        title={t('cyberpanel.delete')}
        message={t('cyberpanel.confirmDelete')}
      />
    </div>
  )
}
