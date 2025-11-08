import apiClient from '@/shared/services/api'
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
import { downloadQuizJson, readQuizJsonFile } from '@/features/cyberpanel/types/cyberpanel'

/**
 * CyberPanel Quiz Service
 * High-level service for quiz operations with additional utilities
 */
class CyberPanelService {
  /**
   * List all quizzes
   * @param language Optional language filter for published quizzes
   */
  async listQuizzes(language?: string): Promise<ListQuizzesResponse> {
    return apiClient.listQuizzes(language)
  }

  /**
   * Get quiz for taking (without correct answers)
   */
  async getQuizForTaking(quizId: string): Promise<GetQuizResponse> {
    return apiClient.getQuiz(quizId)
  }

  /**
   * Create new quiz
   */
  async createQuiz(quiz: CreateQuizRequest): Promise<CreateQuizResponse> {
    return apiClient.createQuiz(quiz)
  }

  /**
   * Update existing quiz
   */
  async updateQuiz(quizId: string, quiz: CreateQuizRequest): Promise<CreateQuizResponse> {
    return apiClient.updateQuiz(quizId, quiz)
  }

  /**
   * Delete quiz
   */
  async deleteQuiz(quizId: string): Promise<void> {
    await apiClient.deleteQuiz(quizId)
  }

  /**
   * Export quiz to JSON format
   */
  async exportQuiz(quizId: string): Promise<ExportQuizResponse> {
    return apiClient.exportQuiz(quizId)
  }

  /**
   * Export quiz and download as JSON file
   */
  async exportAndDownloadQuiz(quizId: string, filename?: string): Promise<void> {
    const exportedQuiz = await this.exportQuiz(quizId)
    downloadQuizJson(exportedQuiz, filename)
  }

  /**
   * Import quiz from JSON request
   */
  async importQuiz(request: ImportQuizRequest): Promise<ImportQuizResponse> {
    return apiClient.importQuiz(request)
  }

  /**
   * Import quiz from JSON file
   */
  async importQuizFromFile(file: File): Promise<ImportQuizResponse> {
    const quizData = await readQuizJsonFile(file)
    return this.importQuiz(quizData)
  }

  /**
   * Submit quiz attempt
   */
  async submitAttempt(request: SubmitAttemptRequest): Promise<SubmitAttemptResponse> {
    return apiClient.submitQuizAttempt(request)
  }

  /**
   * Clone quiz (export and import as new)
   */
  async cloneQuiz(quizId: string, newTitle: string): Promise<ImportQuizResponse> {
    const exportedQuiz = await this.exportQuiz(quizId)
    
    const importRequest: ImportQuizRequest = {
      title: newTitle,
      description: exportedQuiz.description,
      isPublished: false, // Clone as unpublished
      questions: exportedQuiz.questions.map(q => ({
        text: q.text,
        imageUrl: q.imageUrl,
        points: q.points,
        options: q.options.map(o => ({
          text: o.text,
          imageUrl: o.imageUrl,
          isCorrect: o.isCorrect
        }))
      })),
      createNew: true,
      overwriteQuizId: null
    }

    return this.importQuiz(importRequest)
  }

  /**
   * Get quiz statistics
   */
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
    const questionsWithImages = exportedQuiz.questions.filter(q => q.imageUrl).length
    const totalOptions = exportedQuiz.questions.reduce((sum, q) => sum + q.options.length, 0)

    return {
      totalQuestions,
      totalPoints,
      averagePointsPerQuestion: totalPoints / totalQuestions || 0,
      questionsWithImages,
      totalOptions
    }
  }

  /**
   * Validate quiz before submission
   */
  validateQuizData(quiz: CreateQuizRequest): { valid: boolean; errors: string[] } {
    const errors: string[] = []

    // Title validation
    if (!quiz.title || quiz.title.trim().length === 0) {
      errors.push('Quiz title is required')
    }
    if (quiz.title && quiz.title.length > 200) {
      errors.push('Title cannot exceed 200 characters')
    }

    // Description validation
    if (quiz.description && quiz.description.length > 1000) {
      errors.push('Description cannot exceed 1000 characters')
    }

    // Questions validation
    if (!quiz.questions || quiz.questions.length === 0) {
      errors.push('Quiz must have at least one question')
    }
    if (quiz.questions && quiz.questions.length > 100) {
      errors.push('Quiz cannot have more than 100 questions')
    }

    // Validate each question
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

      // Check for at least one correct answer
      const hasCorrectAnswer = question.options.some(o => o.isCorrect)
      if (!hasCorrectAnswer) {
        errors.push(`Question ${qIndex + 1}: Must have at least one correct answer`)
      }

      // Validate each option
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
      errors
    }
  }
}

export const cyberPanelService = new CyberPanelService()
export default cyberPanelService
