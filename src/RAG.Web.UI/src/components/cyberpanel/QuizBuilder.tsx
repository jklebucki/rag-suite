import React, { useState, useEffect } from 'react'
import { useI18n } from '@/contexts/I18nContext'
import { useQuizzes } from '@/hooks'
import { useToast } from '@/contexts/ToastContext'
import type { CreateQuizRequest, CreateQuizQuestionDto, CreateQuizOptionDto } from '@/types'
import { fileToDataUri, validateImageSize } from '@/types/cyberpanel'
import cyberPanelService from '@/services/cyberPanelService'
import { Button } from '@/components/ui'
import { Input } from '@/components/ui'
import { Textarea } from '@/components/ui'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui'
import { Trash2, Plus, Image as ImageIcon, X, Save, Eye, ChevronUp, ChevronDown } from 'lucide-react'

interface QuizBuilderProps {
  editQuizId?: string | null
  onSave?: () => void
  onCancel?: () => void
}

export default function QuizBuilder({ editQuizId, onSave, onCancel }: QuizBuilderProps) {
  const { t } = useI18n()
  const { createQuiz, updateQuiz, loading } = useQuizzes()
  const { showSuccess, showError } = useToast()

  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [isPublished, setIsPublished] = useState(false)
  const [questions, setQuestions] = useState<CreateQuizQuestionDto[]>([])
  const [preview, setPreview] = useState(false)
  const [validationErrors, setValidationErrors] = useState<string[]>([])

  // Load quiz for editing
  useEffect(() => {
    if (editQuizId) {
      loadQuizForEdit(editQuizId)
    }
  }, [editQuizId])

  const loadQuizForEdit = async (quizId: string) => {
    try {
      const exportedQuiz = await cyberPanelService.exportQuiz(quizId)
      if (exportedQuiz) {
        setTitle(exportedQuiz.title)
        setDescription(exportedQuiz.description || '')
        setIsPublished(exportedQuiz.isPublished)
        setQuestions(
          exportedQuiz.questions.map(q => ({
            id: q.id,
            text: q.text,
            imageUrl: q.imageUrl,
            points: q.points,
            options: q.options.map(o => ({
              id: o.id,
              text: o.text,
              imageUrl: o.imageUrl,
              isCorrect: o.isCorrect
            }))
          }))
        )
      }
    } catch (error) {
      showError(t('cyberpanel.errorLoading'))
    }
  }

  const addQuestion = () => {
    const newQuestion: CreateQuizQuestionDto = {
      id: null,
      text: '',
      imageUrl: null,
      points: 10,
      options: [
        { id: null, text: '', imageUrl: null, isCorrect: true },
        { id: null, text: '', imageUrl: null, isCorrect: false }
      ]
    }
    setQuestions([...questions, newQuestion])
  }

  const removeQuestion = (questionIndex: number) => {
    setQuestions(questions.filter((_, idx) => idx !== questionIndex))
  }

  const moveQuestion = (questionIndex: number, direction: 'up' | 'down') => {
    const newQuestions = [...questions]
    const targetIndex = direction === 'up' ? questionIndex - 1 : questionIndex + 1
    if (targetIndex < 0 || targetIndex >= newQuestions.length) return
    
    const temp = newQuestions[questionIndex]
    newQuestions[questionIndex] = newQuestions[targetIndex]
    newQuestions[targetIndex] = temp
    setQuestions(newQuestions)
  }

  const updateQuestion = (questionIndex: number, field: keyof CreateQuizQuestionDto, value: any) => {
    const newQuestions = [...questions]
    newQuestions[questionIndex] = { ...newQuestions[questionIndex], [field]: value }
    setQuestions(newQuestions)
  }

  const handleQuestionImageUpload = async (questionIndex: number, file: File) => {
    if (!validateImageSize(await fileToDataUri(file), 100)) {
      showError(t('cyberpanel.imageTooLarge'))
      return
    }
    const dataUri = await fileToDataUri(file)
    updateQuestion(questionIndex, 'imageUrl', dataUri)
  }

  const addOption = (questionIndex: number) => {
    const newQuestions = [...questions]
    const newOption: CreateQuizOptionDto = {
      id: null,
      text: '',
      imageUrl: null,
      isCorrect: false
    }
    newQuestions[questionIndex].options.push(newOption)
    setQuestions(newQuestions)
  }

  const removeOption = (questionIndex: number, optionIndex: number) => {
    const newQuestions = [...questions]
    newQuestions[questionIndex].options = newQuestions[questionIndex].options.filter(
      (_, idx) => idx !== optionIndex
    )
    setQuestions(newQuestions)
  }

  const updateOption = (
    questionIndex: number,
    optionIndex: number,
    field: keyof CreateQuizOptionDto,
    value: any
  ) => {
    const newQuestions = [...questions]
    newQuestions[questionIndex].options[optionIndex] = {
      ...newQuestions[questionIndex].options[optionIndex],
      [field]: value
    }
    setQuestions(newQuestions)
  }

  const handleOptionImageUpload = async (
    questionIndex: number,
    optionIndex: number,
    file: File
  ) => {
    if (!validateImageSize(await fileToDataUri(file), 100)) {
      showError(t('cyberpanel.imageTooLarge'))
      return
    }
    const dataUri = await fileToDataUri(file)
    updateOption(questionIndex, optionIndex, 'imageUrl', dataUri)
  }

  const validateQuiz = (): boolean => {
    const quizData: CreateQuizRequest = {
      title,
      description: description || null,
      isPublished,
      questions
    }

    const validation = cyberPanelService.validateQuizData(quizData)
    setValidationErrors(validation.errors)
    
    if (!validation.valid) {
      showError(t('cyberpanel.validationErrors'))
      return false
    }
    
    return true
  }

  const handleSave = async () => {
    if (!validateQuiz()) return

    const quizData: CreateQuizRequest = {
      title,
      description: description || null,
      isPublished,
      questions
    }

    try {
      if (editQuizId) {
        const result = await updateQuiz(editQuizId, quizData)
        if (result) {
          showSuccess(t('cyberpanel.quizUpdated'))
          onSave?.()
        }
      } else {
        const result = await createQuiz(quizData)
        if (result) {
          showSuccess(t('cyberpanel.quizCreated'))
          resetForm()
          onSave?.()
        }
      }
    } catch (error) {
      showError(editQuizId ? t('cyberpanel.errorUpdating') : t('cyberpanel.errorCreating'))
    }
  }

  const resetForm = () => {
    setTitle('')
    setDescription('')
    setIsPublished(false)
    setQuestions([])
    setValidationErrors([])
    setPreview(false)
  }

  if (preview) {
    return (
      <div className="max-w-4xl mx-auto">
        <div className="flex justify-between items-center mb-6">
          <h2 className="text-2xl font-bold">{t('cyberpanel.preview')}</h2>
          <Button onClick={() => setPreview(false)} variant="outline">
            <X className="w-4 h-4 mr-2" />
            {t('cyberpanel.closePreview')}
          </Button>
        </div>
        
        <Card className="mb-6">
          <CardHeader>
            <CardTitle>{title || t('cyberpanel.untitledQuiz')}</CardTitle>
            {description && <p className="text-gray-600 mt-2">{description}</p>}
          </CardHeader>
        </Card>

        {questions.map((question, qIdx) => (
          <Card key={qIdx} className="mb-4">
            <CardContent className="pt-6">
              <div className="flex justify-between items-start mb-4">
                <h3 className="text-lg font-semibold">
                  {qIdx + 1}. {question.text || t('cyberpanel.noQuestionText')}
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
              {t('common.cancel')}
            </Button>
          )}
          <Button onClick={() => setPreview(true)} variant="outline">
            <Eye className="w-4 h-4 mr-2" />
            {t('cyberpanel.preview')}
          </Button>
          <Button onClick={handleSave} disabled={loading}>
            <Save className="w-4 h-4 mr-2" />
            {t('common.save')}
          </Button>
        </div>
      </div>

      {validationErrors.length > 0 && (
        <Card className="mb-6 border-red-500">
          <CardContent className="pt-6">
            <h3 className="text-red-600 font-semibold mb-2">{t('cyberpanel.validationErrors')}</h3>
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
              onChange={(e: React.ChangeEvent<HTMLInputElement>) => setTitle(e.target.value)}
              placeholder={t('cyberpanel.titlePlaceholder')}
              maxLength={200}
            />
            <p className="text-xs text-gray-500 mt-1">{title.length}/200</p>
          </div>

          <div>
            <label className="block text-sm font-medium mb-2">{t('cyberpanel.description')}</label>
            <Textarea
              value={description}
              onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => setDescription(e.target.value)}
              placeholder={t('cyberpanel.descriptionPlaceholder')}
              maxLength={1000}
              rows={3}
            />
            <p className="text-xs text-gray-500 mt-1">{description.length}/1000</p>
          </div>

          <div className="flex items-center">
            <input
              type="checkbox"
              id="isPublished"
              checked={isPublished}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) => setIsPublished(e.target.checked)}
              className="mr-2"
              aria-label={t('cyberpanel.publishQuiz')}
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
              {t('cyberpanel.noQuestions')}
            </CardContent>
          </Card>
        )}

        {questions.map((question, qIdx) => (
          <Card key={qIdx} className="mb-4">
            <CardContent className="pt-6">
              <div className="flex justify-between items-start mb-4">
                <h4 className="text-lg font-semibold">
                  {t('cyberpanel.question')} {qIdx + 1}
                </h4>
                <div className="flex gap-2">
                  {qIdx > 0 && (
                    <Button
                      onClick={() => moveQuestion(qIdx, 'up')}
                      variant="outline"
                      size="sm"
                    >
                      <ChevronUp className="w-4 h-4" />
                    </Button>
                  )}
                  {qIdx < questions.length - 1 && (
                    <Button
                      onClick={() => moveQuestion(qIdx, 'down')}
                      variant="outline"
                      size="sm"
                    >
                      <ChevronDown className="w-4 h-4" />
                    </Button>
                  )}
                  <Button
                    onClick={() => removeQuestion(qIdx)}
                    variant="destructive"
                    size="sm"
                  >
                    <Trash2 className="w-4 h-4" />
                  </Button>
                </div>
              </div>

              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium mb-2">
                    {t('cyberpanel.questionText')}
                  </label>
                  <Textarea
                    value={question.text}
                    onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => updateQuestion(qIdx, 'text', e.target.value)}
                    placeholder={t('cyberpanel.questionTextPlaceholder')}
                    maxLength={1000}
                    rows={2}
                  />
                </div>

                <div className="flex items-center gap-4">
                  <div className="flex-1">
                    <label className="block text-sm font-medium mb-2">
                      {t('cyberpanel.points')}
                    </label>
                    <Input
                      type="number"
                      min={1}
                      max={100}
                      value={question.points}
                      onChange={(e: React.ChangeEvent<HTMLInputElement>) => updateQuestion(qIdx, 'points', parseInt(e.target.value))}
                    />
                  </div>

                  <div className="flex-1">
                    <label className="block text-sm font-medium mb-2">
                      {t('cyberpanel.questionImage')}
                    </label>
                    <div className="flex gap-2">
                      <input
                        type="file"
                        accept="image/*"
                        onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
                          const file = e.target.files?.[0]
                          if (file) handleQuestionImageUpload(qIdx, file)
                        }}
                        className="hidden"
                        id={`q-img-${qIdx}`}
                        aria-label={t('cyberpanel.questionImage')}
                      />
                      <Button 
                        type="button" 
                        variant="outline" 
                        size="sm"
                        onClick={() => document.getElementById(`q-img-${qIdx}`)?.click()}
                      >
                        <ImageIcon className="w-4 h-4 mr-2" />
                        {t('cyberpanel.uploadImage')}
                      </Button>
                      {question.imageUrl && (
                        <Button
                          type="button"
                          onClick={() => updateQuestion(qIdx, 'imageUrl', null)}
                          variant="outline"
                          size="sm"
                        >
                          <X className="w-4 h-4" />
                        </Button>
                      )}
                    </div>
                  </div>
                </div>

                {question.imageUrl && (
                  <img
                    src={question.imageUrl}
                    alt="Question preview"
                    className="max-w-sm h-auto rounded border"
                  />
                )}

                <div>
                  <div className="flex justify-between items-center mb-2">
                    <label className="text-sm font-medium">{t('cyberpanel.options')}</label>
                    <Button onClick={() => addOption(qIdx)} variant="outline" size="sm">
                      <Plus className="w-3 h-3 mr-1" />
                      {t('cyberpanel.addOption')}
                    </Button>
                  </div>

                  <div className="space-y-2">
                    {question.options.map((option, oIdx) => (
                      <div key={oIdx} className="border rounded p-3 bg-gray-50">
                        <div className="flex items-start gap-2 mb-2">
                          <input
                            type="checkbox"
                            checked={option.isCorrect}
                            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                              updateOption(qIdx, oIdx, 'isCorrect', e.target.checked)
                            }
                            className="mt-1"
                            aria-label={t('cyberpanel.correctAnswer')}
                          />
                          <Input
                            value={option.text}
                            onChange={(e: React.ChangeEvent<HTMLInputElement>) => updateOption(qIdx, oIdx, 'text', e.target.value)}
                            placeholder={`${t('cyberpanel.option')} ${oIdx + 1}`}
                            maxLength={500}
                            className="flex-1"
                          />
                          <input
                            type="file"
                            accept="image/*"
                            onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
                              const file = e.target.files?.[0]
                              if (file) handleOptionImageUpload(qIdx, oIdx, file)
                            }}
                            className="hidden"
                            id={`opt-img-${qIdx}-${oIdx}`}
                            aria-label={t('cyberpanel.optionImage')}
                          />
                          <Button 
                            type="button" 
                            variant="outline" 
                            size="sm"
                            onClick={() => document.getElementById(`opt-img-${qIdx}-${oIdx}`)?.click()}
                          >
                            <ImageIcon className="w-3 h-3" />
                          </Button>
                          <Button
                            type="button"
                            onClick={() => removeOption(qIdx, oIdx)}
                            variant="destructive"
                            size="sm"
                          >
                            <Trash2 className="w-3 h-3" />
                          </Button>
                        </div>
                        {option.imageUrl && (
                          <div className="relative">
                            <img
                              src={option.imageUrl}
                              alt="Option preview"
                              className="max-w-xs h-auto rounded"
                            />
                            <Button
                              onClick={() => updateOption(qIdx, oIdx, 'imageUrl', null)}
                              variant="outline"
                              size="sm"
                              className="absolute top-2 right-2"
                            >
                              <X className="w-3 h-3" />
                            </Button>
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  )
}
