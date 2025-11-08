// CyberPanel Quiz Helper Functions
// Utility functions for working with quiz images and validation

import type { CreateQuizRequest, GetQuizResponse } from '@/shared/types/api'

/**
 * Helper type for image handling (URL or base64)
 */
export type ImageData = string | null

/**
 * Helper to convert base64 to data URI
 */
export function toDataUri(base64: string, mimeType: string = 'image/png'): string {
  if (base64.startsWith('data:')) {
    return base64
  }
  return `data:${mimeType};base64,${base64}`
}

/**
 * Helper to extract base64 from data URI
 */
export function fromDataUri(dataUri: string): string {
  if (!dataUri.startsWith('data:')) {
    return dataUri
  }
  const parts = dataUri.split(',')
  return parts.length > 1 ? parts[1] : dataUri
}

/**
 * Helper to check if string is data URI
 */
export function isDataUri(str: string): boolean {
  return str.startsWith('data:')
}

/**
 * Helper to validate image size (for base64)
 */
export function validateImageSize(imageData: string, maxSizeKB: number = 100): boolean {
  if (!imageData) return true
  const base64 = isDataUri(imageData) ? fromDataUri(imageData) : imageData
  const sizeBytes = (base64.length * 3) / 4
  const sizeKB = sizeBytes / 1024
  return sizeKB <= maxSizeKB
}

/**
 * Convert File to base64 data URI
 */
export function fileToDataUri(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = () => resolve(reader.result as string)
    reader.onerror = reject
    reader.readAsDataURL(file)
  })
}

/**
 * Download JSON file with quiz data
 */
export function downloadQuizJson(quiz: CreateQuizRequest | GetQuizResponse, filename?: string): void {
  const json = JSON.stringify(quiz, null, 2)
  const blob = new Blob([json], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  const quizId = 'id' in quiz ? quiz.id : 'export'
  link.download = filename || `quiz-${quizId}-${Date.now()}.json`
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(url)
}

/**
 * Read JSON file and parse quiz data
 */
export function readQuizJsonFile(file: File): Promise<CreateQuizRequest> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = () => {
      try {
        const quiz = JSON.parse(reader.result as string) as CreateQuizRequest
        resolve(quiz)
      } catch {
        reject(new Error('Invalid JSON file'))
      }
    }
    reader.onerror = () => reject(new Error('Failed to read file'))
    reader.readAsText(file)
  })
}

