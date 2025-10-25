import { useState, useCallback } from 'react'
import cyberPanelService from '@/services/cyberPanelService'
import type {
  ListQuizzesResponse,
  GetQuizResponse,
  CreateQuizRequest,
  CreateQuizResponse,
  ExportQuizResponse,
  ImportQuizRequest,
  ImportQuizResponse,
  SubmitAttemptRequest,
  SubmitAttemptResponse
} from '@/types'

interface UseQuizzesReturn {
  quizzes: ListQuizzesResponse | null
  loading: boolean
  error: string | null
  fetchQuizzes: () => Promise<void>
  createQuiz: (quiz: CreateQuizRequest) => Promise<CreateQuizResponse | null>
  updateQuiz: (quizId: string, quiz: CreateQuizRequest) => Promise<CreateQuizResponse | null>
  deleteQuiz: (quizId: string) => Promise<boolean>
  exportQuiz: (quizId: string) => Promise<ExportQuizResponse | null>
  importQuiz: (request: ImportQuizRequest) => Promise<ImportQuizResponse | null>
  importFromFile: (file: File) => Promise<ImportQuizResponse | null>
  cloneQuiz: (quizId: string, newTitle: string) => Promise<ImportQuizResponse | null>
}

/**
 * Hook for managing quizzes list and CRUD operations
 */
export function useQuizzes(): UseQuizzesReturn {
  const [quizzes, setQuizzes] = useState<ListQuizzesResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const fetchQuizzes = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await cyberPanelService.listQuizzes()
      setQuizzes(data)
    } catch (err: any) {
      setError(err.message || 'Failed to fetch quizzes')
      console.error('Error fetching quizzes:', err)
    } finally {
      setLoading(false)
    }
  }, [])

  const createQuiz = useCallback(async (quiz: CreateQuizRequest): Promise<CreateQuizResponse | null> => {
    setLoading(true)
    setError(null)
    try {
      const result = await cyberPanelService.createQuiz(quiz)
      await fetchQuizzes() // Refresh list
      return result
    } catch (err: any) {
      setError(err.response?.data?.title || err.message || 'Failed to create quiz')
      console.error('Error creating quiz:', err)
      return null
    } finally {
      setLoading(false)
    }
  }, [fetchQuizzes])

  const updateQuiz = useCallback(async (quizId: string, quiz: CreateQuizRequest): Promise<CreateQuizResponse | null> => {
    setLoading(true)
    setError(null)
    try {
      const result = await cyberPanelService.updateQuiz(quizId, quiz)
      await fetchQuizzes() // Refresh list
      return result
    } catch (err: any) {
      setError(err.response?.data?.title || err.message || 'Failed to update quiz')
      console.error('Error updating quiz:', err)
      return null
    } finally {
      setLoading(false)
    }
  }, [fetchQuizzes])

  const deleteQuiz = useCallback(async (quizId: string): Promise<boolean> => {
    setLoading(true)
    setError(null)
    try {
      await cyberPanelService.deleteQuiz(quizId)
      await fetchQuizzes() // Refresh list
      return true
    } catch (err: any) {
      setError(err.message || 'Failed to delete quiz')
      console.error('Error deleting quiz:', err)
      return false
    } finally {
      setLoading(false)
    }
  }, [fetchQuizzes])

  const exportQuiz = useCallback(async (quizId: string): Promise<ExportQuizResponse | null> => {
    setLoading(true)
    setError(null)
    try {
      const result = await cyberPanelService.exportQuiz(quizId)
      return result
    } catch (err: any) {
      setError(err.message || 'Failed to export quiz')
      console.error('Error exporting quiz:', err)
      return null
    } finally {
      setLoading(false)
    }
  }, [])

  const importQuiz = useCallback(async (request: ImportQuizRequest): Promise<ImportQuizResponse | null> => {
    setLoading(true)
    setError(null)
    try {
      const result = await cyberPanelService.importQuiz(request)
      await fetchQuizzes() // Refresh list
      return result
    } catch (err: any) {
      setError(err.response?.data?.title || err.message || 'Failed to import quiz')
      console.error('Error importing quiz:', err)
      return null
    } finally {
      setLoading(false)
    }
  }, [fetchQuizzes])

  const importFromFile = useCallback(async (file: File): Promise<ImportQuizResponse | null> => {
    setLoading(true)
    setError(null)
    try {
      const result = await cyberPanelService.importQuizFromFile(file)
      await fetchQuizzes() // Refresh list
      return result
    } catch (err: any) {
      setError(err.response?.data?.title || err.message || 'Failed to import quiz from file')
      console.error('Error importing quiz from file:', err)
      return null
    } finally {
      setLoading(false)
    }
  }, [fetchQuizzes])

  const cloneQuiz = useCallback(async (quizId: string, newTitle: string): Promise<ImportQuizResponse | null> => {
    setLoading(true)
    setError(null)
    try {
      const result = await cyberPanelService.cloneQuiz(quizId, newTitle)
      await fetchQuizzes() // Refresh list
      return result
    } catch (err: any) {
      setError(err.message || 'Failed to clone quiz')
      console.error('Error cloning quiz:', err)
      return null
    } finally {
      setLoading(false)
    }
  }, [fetchQuizzes])

  return {
    quizzes,
    loading,
    error,
    fetchQuizzes,
    createQuiz,
    updateQuiz,
    deleteQuiz,
    exportQuiz,
    importQuiz,
    importFromFile,
    cloneQuiz
  }
}

interface UseQuizTakingReturn {
  quiz: GetQuizResponse | null
  loading: boolean
  error: string | null
  result: SubmitAttemptResponse | null
  fetchQuiz: (quizId: string) => Promise<void>
  submitAttempt: (request: SubmitAttemptRequest) => Promise<SubmitAttemptResponse | null>
  resetQuiz: () => void
}

/**
 * Hook for taking a quiz
 */
export function useQuizTaking(): UseQuizTakingReturn {
  const [quiz, setQuiz] = useState<GetQuizResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<SubmitAttemptResponse | null>(null)

  const fetchQuiz = useCallback(async (quizId: string) => {
    setLoading(true)
    setError(null)
    setResult(null)
    try {
      const data = await cyberPanelService.getQuizForTaking(quizId)
      setQuiz(data)
    } catch (err: any) {
      setError(err.message || 'Failed to fetch quiz')
      console.error('Error fetching quiz:', err)
    } finally {
      setLoading(false)
    }
  }, [])

  const submitAttempt = useCallback(async (request: SubmitAttemptRequest): Promise<SubmitAttemptResponse | null> => {
    setLoading(true)
    setError(null)
    try {
      const attemptResult = await cyberPanelService.submitAttempt(request)
      setResult(attemptResult)
      return attemptResult
    } catch (err: any) {
      setError(err.message || 'Failed to submit quiz')
      console.error('Error submitting quiz:', err)
      return null
    } finally {
      setLoading(false)
    }
  }, [])

  const resetQuiz = useCallback(() => {
    setQuiz(null)
    setResult(null)
    setError(null)
  }, [])

  return {
    quiz,
    loading,
    error,
    result,
    fetchQuiz,
    submitAttempt,
    resetQuiz
  }
}
