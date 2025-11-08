import type { LlmSettingsRequest, LlmSettingsResponse, AvailableModelsResponse } from '@/types'
import { apiHttpClient } from '@/shared/services/api/httpClients'

export async function getLlmSettings(): Promise<LlmSettingsResponse> {
  const response = await apiHttpClient.get<LlmSettingsResponse>('/settings/llm')
  return response.data
}

export async function updateLlmSettings(settings: LlmSettingsRequest): Promise<{ message: string }> {
  const response = await apiHttpClient.put<{ message: string }>('/settings/llm', settings)
  return response.data
}

export async function getAvailableLlmModels(): Promise<AvailableModelsResponse> {
  const response = await apiHttpClient.get<AvailableModelsResponse>('/settings/llm/models')
  return response.data
}

export async function getAvailableLlmModelsFromUrl(url: string, isOllama: boolean): Promise<AvailableModelsResponse> {
  const params = new URLSearchParams({
    url,
    isOllama: isOllama.toString(),
  })
  const response = await apiHttpClient.get<AvailableModelsResponse>(`/settings/llm/models?${params}`)
  return response.data
}

const llmService = {
  getLlmSettings,
  updateLlmSettings,
  getAvailableLlmModels,
  getAvailableLlmModelsFromUrl,
}

export default llmService
