import { apiHttpClient } from '@/shared/services/api/httpClients'
import type { ApiResponse } from '@/shared/types/api'
import type {
  CreateQuizRequest,
  CreateQuizResponse,
  ExportQuizResponse,
  GetAttemptByIdResponse,
  GetQuizResponse,
  ImportQuizRequest,
  ImportQuizResponse,
  ListAttemptsResponse,
  ListQuizzesResponse,
  SubmitAttemptRequest,
  SubmitAttemptResponse,
} from '@/features/cyberpanel/types/quiz'
import { downloadQuizJson, readQuizJsonFile } from '@/features/cyberpanel/types/cyberpanel'

class CyberPanelService {
  async listQuizzes(language?: string): Promise<ListQuizzesResponse> {
    const params = language ? { language } : {}
    const response = await apiHttpClient.get<ListQuizzesResponse>('/cyberpanel/quizzes', { params })
    return response.data
  }

  async getQuizForTaking(quizId: string): Promise<GetQuizResponse> {
    const response = await apiHttpClient.get<ApiResponse<GetQuizResponse>>(`/cyberpanel/quizzes/${quizId}`)
    return response.data.data
  }

  async createQuiz(quiz: CreateQuizRequest): Promise<CreateQuizResponse> {
    const response = await apiHttpClient.post<CreateQuizResponse>('/cyberpanel/quizzes', quiz)
    return response.data
  }

  async updateQuiz(quizId: string, quiz: CreateQuizRequest): Promise<CreateQuizResponse> {
    const response = await apiHttpClient.put<CreateQuizResponse>(`/cyberpanel/quizzes/${quizId}`, quiz)
    return response.data
  }

  async deleteQuiz(quizId: string): Promise<void> {
    await apiHttpClient.delete(`/cyberpanel/quizzes/${quizId}`)
  }

  async exportQuiz(quizId: string): Promise<ExportQuizResponse> {
    const response = await apiHttpClient.get<ExportQuizResponse>(`/cyberpanel/quizzes/${quizId}/export`)
    return response.data
  }

  async exportAndDownloadQuiz(quizId: string, filename?: string): Promise<void> {
    const exportedQuiz = await this.exportQuiz(quizId)
    downloadQuizJson(exportedQuiz, filename)
  }

  async importQuiz(request: ImportQuizRequest): Promise<ImportQuizResponse> {
    const response = await apiHttpClient.post<ImportQuizResponse>('/cyberpanel/quizzes/import', request)
    return response.data
  }

  async importQuizFromFile(file: File): Promise<ImportQuizResponse> {
    const quizData = await readQuizJsonFile(file)
    return this.importQuiz(quizData)
  }

  async submitAttempt(request: SubmitAttemptRequest): Promise<SubmitAttemptResponse> {
    const response = await apiHttpClient.post<SubmitAttemptResponse>(
      `/cyberpanel/quizzes/${request.quizId}/attempts`,
      request
    )
    return response.data
  }

  async deleteAttempt(attemptId: string): Promise<void> {
    await apiHttpClient.delete(`/cyberpanel/quizzes/attempts/${attemptId}`)
  }

  async listQuizAttempts(): Promise<ListAttemptsResponse> {
    const response = await apiHttpClient.get<ListAttemptsResponse>('/cyberpanel/quizzes/attempts')
    return response.data
  }

  async getAttemptById(attemptId: string): Promise<GetAttemptByIdResponse> {
    const response = await apiHttpClient.get<GetAttemptByIdResponse>(`/cyberpanel/quizzes/attempts/${attemptId}`)
    return response.data
  }

  async cloneQuiz(quizId: string, newTitle: string): Promise<ImportQuizResponse> {
    const exportedQuiz = await this.exportQuiz(quizId)

    const importRequest: ImportQuizRequest = {
      title: newTitle,
      description: exportedQuiz.description,
      isPublished: false,
      questions: exportedQuiz.questions.map((q) => ({
        text: q.text,
        imageUrl: q.imageUrl,
        points: q.points,
        options: q.options.map((o) => ({
          text: o.text,
          imageUrl: o.imageUrl,
          isCorrect: o.isCorrect,
        })),
      })),
      createNew: true,
      overwriteQuizId: null,
    }

    return this.importQuiz(importRequest)
  }

  async getQuizStatistics(quizId: string): Promise<{
    totalQuestions: number
    totalPoints: number
    averagePointsPerQuestion: number
    questionsWithImages: number
    totalOptions: number
  }> {
    const exportedQuiz = await this.exportQuiz(quizId)

    const totalQuestions = exportedQuiz.questions.length
    const totalPoints = exportedQuiz.questions.reduce((sum, q) => sum + q.points, 0)
    const questionsWithImages = exportedQuiz.questions.filter((q) => q.imageUrl).length
    const totalOptions = exportedQuiz.questions.reduce((sum, q) => sum + q.options.length, 0)

    return {
      totalQuestions,
      totalPoints,
      averagePointsPerQuestion: totalPoints / totalQuestions || 0,
      questionsWithImages,
      totalOptions,
    }
  }

  validateQuizData(quiz: CreateQuizRequest): { valid: boolean; errors: string[] } {
    const errors: string[] = []

    if (!quiz.title || quiz.title.trim().length === 0) {
      errors.push('Quiz title is required')
    }
    if (quiz.title && quiz.title.length > 200) {
      errors.push('Title cannot exceed 200 characters')
    }

    if (quiz.description && quiz.description.length > 1000) {
      errors.push('Description cannot exceed 1000 characters')
    }

    if (!quiz.questions || quiz.questions.length === 0) {
      errors.push('Quiz must have at least one question')
    }
    if (quiz.questions && quiz.questions.length > 100) {
      errors.push('Quiz cannot have more than 100 questions')
    }

    quiz.questions.forEach((question, qIndex) => {
      if (!question.text || question.text.trim().length === 0) {
        errors.push(`Question ${qIndex + 1}: Text is required`)
      }
      if (question.text && question.text.length > 1000) {
        errors.push(`Question ${qIndex + 1}: Text cannot exceed 1000 characters`)
      }
      if (question.points < 1 || question.points > 100) {
        errors.push(`Question ${qIndex + 1}: Points must be between 1 and 100`)
      }
      if (!question.options || question.options.length < 2) {
        errors.push(`Question ${qIndex + 1}: Must have at least 2 options`)
      }
      if (question.options && question.options.length > 10) {
        errors.push(`Question ${qIndex + 1}: Cannot have more than 10 options`)
      }

      const hasCorrectAnswer = question.options.some((o) => o.isCorrect)
      if (!hasCorrectAnswer) {
        errors.push(`Question ${qIndex + 1}: Must have at least one correct answer`)
      }

      question.options.forEach((option, oIndex) => {
        if (!option.text || option.text.trim().length === 0) {
          errors.push(`Question ${qIndex + 1}, Option ${oIndex + 1}: Text is required`)
        }
        if (option.text && option.text.length > 500) {
          errors.push(`Question ${qIndex + 1}, Option ${oIndex + 1}: Text cannot exceed 500 characters`)
        }
      })
    })

    return {
      valid: errors.length === 0,
      errors,
    }
  }
}

export const cyberPanelService = new CyberPanelService()
export default cyberPanelService
