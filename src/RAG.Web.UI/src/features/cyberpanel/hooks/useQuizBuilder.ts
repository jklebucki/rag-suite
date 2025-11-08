import { useState, useEffect, useCallback } from 'react'
import { useI18n } from '@/shared/contexts/I18nContext'
import { useQuizzes } from '@/features/cyberpanel/hooks/useQuizzes'
import { useToast } from '@/shared/contexts/ToastContext'
import type { CreateQuizRequest, CreateQuizQuestionDto, CreateQuizOptionDto } from '@/features/cyberpanel/types/quiz'
import { fileToDataUri, validateImageSize } from '@/features/cyberpanel/types/cyberpanel'
import cyberPanelService from '@/features/cyberpanel/services/cyberPanelService'
import { logger } from '@/utils/logger'

interface UseQuizBuilderProps {
  editQuizId?: string | null
  onSave?: () => void
}

export function useQuizBuilder({ editQuizId, onSave }: UseQuizBuilderProps) {
  const { t, language } = useI18n()
  const { createQuiz, updateQuiz, loading, exportQuiz } = useQuizzes()
  const { showSuccess, showError } = useToast()

  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [isPublished, setIsPublished] = useState(false)
  const [quizLanguage, setQuizLanguage] = useState<string>(language)
  const [questions, setQuestions] = useState<CreateQuizQuestionDto[]>([])
  const [validationErrors, setValidationErrors] = useState<string[]>([])

  const loadQuizForEdit = useCallback(async (quizId: string) => {
    try {
      const exportedQuiz = await cyberPanelService.exportQuiz(quizId)
      if (exportedQuiz) {
        setTitle(exportedQuiz.title)
        setDescription(exportedQuiz.description || '')
        setIsPublished(exportedQuiz.isPublished)
        setQuizLanguage(exportedQuiz.language || language)
        setQuestions(
          exportedQuiz.questions.map((q) => ({
            id: q.id,
            text: q.text,
            imageUrl: q.imageUrl,
            points: q.points,
            options: q.options.map((o) => ({
              id: o.id,
              text: o.text,
              imageUrl: o.imageUrl,
              isCorrect: o.isCorrect,
            })),
          }))
        )
      }
    } catch (error) {
      logger.error('Failed to load quiz for editing', error)
      showError(t('cyberpanel.errorLoading'))
    }
  }, [language, showError, t])

  // Load quiz for editing
  useEffect(() => {
    if (editQuizId) {
      void loadQuizForEdit(editQuizId)
    }
  }, [editQuizId, loadQuizForEdit])

  // Question operations
  const addQuestion = () => {
    const newQuestion: CreateQuizQuestionDto = {
      id: null,
      text: '',
      imageUrl: null,
      points: 10,
      options: [
        { id: null, text: '', imageUrl: null, isCorrect: true },
        { id: null, text: '', imageUrl: null, isCorrect: false },
      ],
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

  const updateQuestion = <K extends keyof CreateQuizQuestionDto>(
    questionIndex: number,
    field: K,
    value: CreateQuizQuestionDto[K]
  ) => {
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

  // Option operations
  const addOption = (questionIndex: number) => {
    const newQuestions = [...questions]
    const newOption: CreateQuizOptionDto = {
      id: null,
      text: '',
      imageUrl: null,
      isCorrect: false,
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

  const updateOption = <K extends keyof CreateQuizOptionDto>(
    questionIndex: number,
    optionIndex: number,
    field: K,
    value: CreateQuizOptionDto[K]
  ) => {
    const newQuestions = [...questions]
    newQuestions[questionIndex].options[optionIndex] = {
      ...newQuestions[questionIndex].options[optionIndex],
      [field]: value,
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

  // Validation
  const validateQuiz = (): boolean => {
    const quizData: CreateQuizRequest = {
      title,
      description: description || null,
      isPublished,
      questions,
      language: quizLanguage,
    }

    const validation = cyberPanelService.validateQuizData(quizData)
    setValidationErrors(validation.errors)

    if (!validation.valid) {
      showError(t('cyberpanel.validationErrors'))
      return false
    }

    return true
  }

  // Save/Update
  const handleSave = async () => {
    if (!validateQuiz()) return

    const quizData: CreateQuizRequest = {
      title,
      description: description || null,
      isPublished,
      questions,
      language: quizLanguage,
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
      logger.error('Failed to save quiz', error)
      showError(editQuizId ? t('cyberpanel.errorUpdating') : t('cyberpanel.errorCreating'))
    }
  }

  // Reset
  const resetForm = () => {
    setTitle('')
    setDescription('')
    setIsPublished(false)
    setQuizLanguage(language)
    setQuestions([])
    setValidationErrors([])
  }

  // Export
  const handleExportQuiz = async () => {
    if (!editQuizId) {
      showError('Please save the quiz first before exporting')
      return
    }

    const result = await exportQuiz(editQuizId)
    if (result) {
      const blob = new Blob([JSON.stringify(result, null, 2)], { type: 'application/json' })
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `${title.replace(/[^a-z0-9]/gi, '_').toLowerCase()}_${
        new Date().toISOString().split('T')[0]
      }.json`
      document.body.appendChild(a)
      a.click()
      document.body.removeChild(a)
      URL.revokeObjectURL(url)
      showSuccess(t('cyberpanel.exportSuccess'))
    } else {
      showError(t('cyberpanel.importError'))
    }
  }

  return {
    // State
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

    // Question operations
    addQuestion,
    removeQuestion,
    moveQuestion,
    updateQuestion,
    handleQuestionImageUpload,

    // Option operations
    addOption,
    removeOption,
    updateOption,
    handleOptionImageUpload,

    // Actions
    handleSave,
    resetForm,
    handleExportQuiz,
  }
}
