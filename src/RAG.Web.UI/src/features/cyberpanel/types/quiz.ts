export interface QuizOption {
  id?: string
  text: string
  imageUrl?: string | null
  isCorrect?: boolean
}

export interface QuizQuestion {
  id?: string
  text: string
  imageUrl?: string | null
  points: number
  order?: number
  options: QuizOption[]
}

export interface Quiz {
  id: string
  title: string
  description?: string | null
  createdByUserId: string
  createdAt: string | Date
  isPublished: boolean
  questions: QuizQuestion[]
}

export interface QuizListItem {
  id: string
  title: string
  description?: string | null
  isPublished: boolean
  createdAt: string | Date
  questionCount: number
  language?: string | null
}

export interface ListQuizzesResponse {
  quizzes: QuizListItem[]
  total: number
}

export interface QuizOptionDto {
  id: string
  text: string
  imageUrl?: string | null
}

export interface QuizQuestionDto {
  id: string
  text: string
  imageUrl?: string | null
  points: number
  options: QuizOptionDto[]
}

export interface GetQuizResponse {
  id: string
  title: string
  description?: string | null
  isPublished: boolean
  questions: QuizQuestionDto[]
}

export interface CreateQuizOptionDto {
  id?: string | null
  text: string
  imageUrl?: string | null
  isCorrect: boolean
}

export interface CreateQuizQuestionDto {
  id?: string | null
  text: string
  imageUrl?: string | null
  points: number
  options: CreateQuizOptionDto[]
}

export interface CreateQuizRequest {
  title: string
  description?: string | null
  isPublished: boolean
  questions: CreateQuizQuestionDto[]
  language?: string | null
}

export interface CreateQuizResponse {
  id: string
  title: string
}

export interface ExportedOptionDto {
  id: string
  text: string
  imageUrl?: string | null
  isCorrect: boolean
}

export interface ExportedQuestionDto {
  id: string
  text: string
  imageUrl?: string | null
  order: number
  points: number
  options: ExportedOptionDto[]
}

export interface ExportQuizResponse {
  id: string
  title: string
  description?: string | null
  createdByUserId: string
  createdAt: string | Date
  isPublished: boolean
  questions: ExportedQuestionDto[]
  language?: string | null
  exportVersion: string
  exportedAt: string | Date
}

export interface ImportedOptionDto {
  text: string
  imageUrl?: string | null
  isCorrect: boolean
}

export interface ImportedQuestionDto {
  text: string
  imageUrl?: string | null
  points: number
  options: ImportedOptionDto[]
}

export interface ImportQuizRequest {
  title: string
  description?: string | null
  isPublished: boolean
  questions: ImportedQuestionDto[]
  createNew?: boolean
  overwriteQuizId?: string | null
  language?: string | null
}

export interface ImportQuizResponse {
  quizId: string
  title: string
  questionsImported: number
  optionsImported: number
  wasOverwritten: boolean
  importedAt: string | Date
}

export interface SubmitAttemptRequest {
  quizId: string
  answers: {
    questionId: string
    selectedOptionIds: string[]
  }[]
}

export interface SubmitAttemptResponse {
  attemptId: string
  quizId: string
  score: number
  maxScore: number
  percentageScore?: number
  submittedAt?: string | Date
  perQuestionResults: {
    questionId: string
    correct: boolean
    pointsAwarded: number
    maxPoints: number
  }[]
}

export interface QuizAttemptDto {
  id: string
  quizId: string
  quizTitle: string
  userId: string
  userName: string
  userEmail?: string
  score: number
  maxScore: number
  percentageScore: number
  submittedAt: string | Date
  questionCount: number
  correctAnswers: number
}

export interface ListAttemptsResponse {
  attempts: QuizAttemptDto[]
}

export interface QuestionResultDto {
  questionId: string
  questionText: string
  questionImageUrl?: string
  points: number
  isCorrect: boolean
  pointsAwarded: number
  maxPoints: number
  options: OptionResultDto[]
  selectedOptionIds: string[]
  correctOptionIds: string[]
}

export interface OptionResultDto {
  id: string
  text: string
  imageUrl?: string
  isCorrect: boolean
}

export interface AttemptDetailDto {
  id: string
  quizId: string
  quizTitle: string
  userId: string
  userName: string
  userEmail?: string
  score: number
  maxScore: number
  percentageScore: number
  submittedAt: string | Date
  questionCount: number
  correctAnswers: number
  questions: QuestionResultDto[]
}

export interface GetAttemptByIdResponse {
  attempt: AttemptDetailDto
}

export interface DeleteQuizResponse {
  quizId: string
  quizTitle: string
  questionCount: number
  attemptCount: number
  ownerUserName: string
  deletedByUserName: string
  deletedAt: string
}

