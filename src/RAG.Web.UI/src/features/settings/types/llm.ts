export interface LlmSettings {
  url: string
  maxTokens: number
  temperature: number
  model: string
  isOllama: boolean
  timeoutMinutes: number
  chatEndpoint: string
  generateEndpoint: string
}

export interface LlmSettingsRequest {
  url: string
  maxTokens: number
  temperature: number
  model: string
  isOllama: boolean
  timeoutMinutes: number
  chatEndpoint: string
  generateEndpoint: string
}

export interface LlmSettingsResponse {
  url: string
  maxTokens: number
  temperature: number
  model: string
  isOllama: boolean
  timeoutMinutes: number
  chatEndpoint: string
  generateEndpoint: string
}

export interface AvailableModelsResponse {
  models: string[]
}

