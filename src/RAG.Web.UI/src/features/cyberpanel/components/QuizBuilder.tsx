import { useState } from 'react'
import { useI18n } from '@/shared/contexts/I18nContext'
import { useQuizBuilder } from '@/features/cyberpanel/hooks/useQuizBuilder'
import { Button, Input, Textarea, Card, CardContent, CardHeader, CardTitle } from '@/shared/ui'
import { Plus, Save, Eye, X, Download } from 'lucide-react'
import { QuestionEditor } from './QuizBuilder/QuestionEditor'

interface QuizBuilderProps {
  editQuizId?: string | null
  onSave?: () => void
  onCancel?: () => void
}

export function QuizBuilder({ editQuizId, onSave, onCancel }: QuizBuilderProps) {
  const { t } = useI18n()
  const [preview, setPreview] = useState(false)

  const {
    title,
    setTitle,
    description,
    setDescription,
    isPublished,
    setIsPublished,
    quizLanguage,
    setQuizLanguage,
    questions,
    validationErrors,
    loading,
    addQuestion,
    removeQuestion,
    moveQuestion,
    updateQuestion,
    handleQuestionImageUpload,
    addOption,
    removeOption,
    updateOption,
    handleOptionImageUpload,
    handleSave,
    handleExportQuiz,
  } = useQuizBuilder({ editQuizId, onSave })

  if (preview) {
    return (
      <div className="max-w-4xl mx-auto">
        <div className="flex justify-between items-center mb-6">
          <h2 className="text-2xl font-bold">{t('cyberpanel.preview')}</h2>
          <Button onClick={() => setPreview(false)} variant="outline">
            <X className="w-4 h-4 mr-2" />
            Close Preview
          </Button>
        </div>

        <Card className="mb-6">
          <CardHeader>
            <CardTitle>{title || 'Untitled Quiz'}</CardTitle>
            {description && <p className="text-gray-600 mt-2">{description}</p>}
          </CardHeader>
        </Card>

        {questions.map((question, qIdx) => (
          <Card key={qIdx} className="mb-4">
            <CardContent className="pt-6">
              <div className="flex justify-between items-start mb-4">
                <h3 className="text-lg font-semibold">
                  {qIdx + 1}. {question.text || 'No question text'}
                </h3>
                <span className="text-sm bg-blue-100 text-blue-800 px-2 py-1 rounded">
                  {question.points} {t('cyberpanel.points')}
                </span>
              </div>

              {question.imageUrl && (
                <img
                  src={question.imageUrl}
                  alt="Question"
                  className="max-w-full h-auto mb-4 rounded"
                />
              )}

              <div className="space-y-2">
                {question.options.map((option, oIdx) => (
                  <div
                    key={oIdx}
                    className={`p-3 rounded border ${
                      option.isCorrect ? 'border-green-500 bg-green-50' : 'border-gray-300'
                    }`}
                  >
                    {option.imageUrl && (
                      <img
                        src={option.imageUrl}
                        alt="Option"
                        className="max-w-xs h-auto mb-2 rounded"
                      />
                    )}
                    <p>{option.text}</p>
                    {option.isCorrect && (
                      <span className="text-xs text-green-600 font-semibold">
                        ✓ {t('cyberpanel.correctAnswer')}
                      </span>
                    )}
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    )
  }

  return (
    <div className="max-w-4xl mx-auto">
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold">
          {editQuizId ? t('cyberpanel.editQuiz') : t('cyberpanel.createQuiz')}
        </h2>
        <div className="flex gap-2">
          {onCancel && (
            <Button onClick={onCancel} variant="outline">
              Cancel
            </Button>
          )}
          {editQuizId && (
            <Button onClick={handleExportQuiz} variant="outline" size="sm">
              <Download className="w-4 h-4 mr-2" />
              Export
            </Button>
          )}
          <Button onClick={() => setPreview(true)} variant="outline">
            <Eye className="w-4 h-4 mr-2" />
            Preview
          </Button>
          <Button onClick={handleSave} disabled={loading}>
            <Save className="w-4 h-4 mr-2" />
            Save
          </Button>
        </div>
      </div>

      {validationErrors.length > 0 && (
        <Card className="mb-6 border-red-500">
          <CardContent className="pt-6">
            <h3 className="text-red-600 font-semibold mb-2">Validation Errors</h3>
            <ul className="list-disc list-inside space-y-1 text-sm text-red-600">
              {validationErrors.map((error, idx) => (
                <li key={idx}>{error}</li>
              ))}
            </ul>
          </CardContent>
        </Card>
      )}

      <Card className="mb-6">
        <CardHeader>
          <CardTitle>{t('cyberpanel.quizDetails')}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-2">{t('cyberpanel.title')}</label>
            <Input
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="Enter quiz title"
              maxLength={200}
            />
            <p className="text-xs text-gray-500 mt-1">{title.length}/200</p>
          </div>

          <div>
            <label className="block text-sm font-medium mb-2">{t('cyberpanel.description')}</label>
            <Textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Enter quiz description"
              maxLength={1000}
              rows={3}
            />
            <p className="text-xs text-gray-500 mt-1">{description.length}/1000</p>
          </div>

          <div>
            <label htmlFor="quizLanguage" className="block text-sm font-medium mb-2">Language</label>
            <select
              id="quizLanguage"
              value={quizLanguage}
              onChange={(e) => setQuizLanguage(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              title="Language"
            >
              <option value="en">English</option>
              <option value="pl">Polski</option>
              <option value="ro">Română</option>
              <option value="hu">Magyar</option>
              <option value="nl">Nederlands</option>
            </select>
            <p className="text-xs text-gray-500 mt-1">Select the language for this quiz</p>
          </div>

          <div className="flex items-center">
            <input
              type="checkbox"
              id="isPublished"
              checked={isPublished}
              onChange={(e) => setIsPublished(e.target.checked)}
              className="mr-2"
              aria-label="Publish quiz"
            />
            <label htmlFor="isPublished" className="text-sm font-medium">
              {t('cyberpanel.publishQuiz')}
            </label>
          </div>
        </CardContent>
      </Card>

      <div className="mb-6">
        <div className="flex justify-between items-center mb-4">
          <h3 className="text-xl font-semibold">{t('cyberpanel.questions')}</h3>
          <Button onClick={addQuestion} variant="outline">
            <Plus className="w-4 h-4 mr-2" />
            {t('cyberpanel.addQuestion')}
          </Button>
        </div>

        {questions.length === 0 && (
          <Card>
            <CardContent className="pt-6 text-center text-gray-500">
              No questions added yet. Click &quot;Add Question&quot; to get started.
            </CardContent>
          </Card>
        )}

        <div className="space-y-4">
          {questions.map((question, qIdx) => (
            <QuestionEditor
              key={qIdx}
              question={question}
              questionIndex={qIdx}
              totalQuestions={questions.length}
              onUpdate={(field, value) => updateQuestion(qIdx, field, value)}
              onRemove={() => removeQuestion(qIdx)}
              onMove={(direction) => moveQuestion(qIdx, direction)}
              onImageUpload={(file) => handleQuestionImageUpload(qIdx, file)}
              onAddOption={() => addOption(qIdx)}
              onRemoveOption={(optionIndex) => removeOption(qIdx, optionIndex)}
              onUpdateOption={(optionIndex, field, value) =>
                updateOption(qIdx, optionIndex, field, value)
              }
              onOptionImageUpload={(optionIndex, file) =>
                handleOptionImageUpload(qIdx, optionIndex, file)
              }
            />
          ))}
        </div>
      </div>
    </div>
  )
}
