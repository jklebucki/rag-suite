/**
 * SettingsForm.test.tsx
 * 
 * Tests for SettingsForm component using useActionState and SubmitButton
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor, act } from '@testing-library/react'
import { fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { SettingsForm } from './SettingsForm'

// Mock dependencies
vi.mock('@/shared/contexts/I18nContext', () => ({
  useI18n: () => ({
    t: (key: string) => key,
    language: 'en',
  }),
}))

vi.mock('@/shared/contexts', () => ({
  useToast: () => ({
    addToast: vi.fn(),
  }),
}))

// Use vi.hoisted to ensure mocks are available in vi.mock
const { mockGetLlmSettings, mockUpdateLlmSettings, mockGetAvailableLlmModelsFromUrl, mockValidateLlmSettings } = vi.hoisted(() => {
  const mockGetLlmSettings = vi.fn()
  const mockUpdateLlmSettings = vi.fn()
  const mockGetAvailableLlmModelsFromUrl = vi.fn()
  const mockValidateLlmSettings = vi.fn().mockReturnValue({ isValid: true, errors: {} })
  
  return {
    mockGetLlmSettings,
    mockUpdateLlmSettings,
    mockGetAvailableLlmModelsFromUrl,
    mockValidateLlmSettings,
  }
})

vi.mock('@/features/settings/services/llm.service', () => ({
  default: {
    getLlmSettings: mockGetLlmSettings,
    updateLlmSettings: mockUpdateLlmSettings,
    getAvailableLlmModelsFromUrl: mockGetAvailableLlmModelsFromUrl,
  },
}))

vi.mock('@/utils/llmValidation', () => ({
  validateLlmSettings: (settings: any) => mockValidateLlmSettings(settings),
}))

vi.mock('@/utils/logger', () => ({
  logger: {
    error: vi.fn(),
  },
}))

describe('SettingsForm', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    // Reset mocks to default values
    mockGetLlmSettings.mockResolvedValue({
      url: 'http://localhost:11434',
      maxTokens: 3000,
      temperature: 0.7,
      model: 'llama2',
      isOllama: true,
      timeoutMinutes: 15,
      chatEndpoint: '/api/chat',
      generateEndpoint: '/api/generate',
    })
    mockUpdateLlmSettings.mockResolvedValue({})
    mockGetAvailableLlmModelsFromUrl.mockResolvedValue({ models: ['llama2', 'mistral'] })
    mockValidateLlmSettings.mockReturnValue({ isValid: true, errors: {} })
  })

  it('should render settings form', async () => {
    render(<SettingsForm />)
    
    // SettingsForm loads data asynchronously in useEffect
    // Labels use translation keys, so we check for inputs by name or placeholder
    await waitFor(() => {
      const urlInput = screen.queryByLabelText(/settings\.llm\.fields\.url\.label|url/i) || 
                       screen.queryByPlaceholderText(/settings\.llm\.fields\.url\.placeholder/i) ||
                       document.querySelector('input[name="url"]')
      expect(urlInput).toBeTruthy()
    }, { timeout: 5000 })
  })

  it('should render SubmitButton component', async () => {
    render(<SettingsForm />)
    
    await waitFor(() => {
      const submitButton = screen.getByRole('button', { name: /save/i })
      expect(submitButton).toBeInTheDocument()
      expect(submitButton).toHaveAttribute('type', 'submit')
    })
  })

  it('should use useActionState for form submission', async () => {
    mockUpdateLlmSettings.mockResolvedValue({})
    mockValidateLlmSettings.mockReturnValue({ isValid: true, errors: {} })

    render(<SettingsForm />)
    
    // Wait for form to load and data to be populated
    // SettingsForm loads data in useEffect via loadSettings
    await waitFor(() => {
      expect(mockGetLlmSettings).toHaveBeenCalled()
    }, { timeout: 5000 })
    
    // Wait for form inputs to be populated with data
    // Also wait for loadAvailableModels to complete (it's called when url changes)
    await waitFor(() => {
      const urlInput = document.querySelector('input[name="url"]') as HTMLInputElement
      expect(urlInput?.value).toBe('http://localhost:11434')
      // Ensure form is fully rendered - check that all required inputs exist
      const form = document.querySelector('form')
      expect(form).toBeTruthy()
    }, { timeout: 5000 })
    
    // Wait a bit more to ensure all effects have completed
    await act(async () => {
      await new Promise(resolve => setTimeout(resolve, 200))
    })
    
    const form = document.querySelector('form') as HTMLFormElement
    
    // Verify form has all required values before submitting
    const urlInput = document.querySelector('input[name="url"]') as HTMLInputElement
    const isOllamaCheckbox = document.querySelector('input[name="isOllama"]') as HTMLInputElement
    
    expect(urlInput?.value).toBe('http://localhost:11434')
    expect(isOllamaCheckbox?.checked).toBe(true) // Default value from mock
    
    // Submit form using fireEvent.submit to trigger formAction
    // This ensures FormData is properly constructed and formAction is called
    fireEvent.submit(form)
    
    // useActionState should call updateLlmSettings through formAction
    // formAction processes FormData, validates, and then calls updateLlmSettings
    // Note: formAction is async, so we need to wait for it to complete
    await waitFor(() => {
      // Check that validateLlmSettings was called (formAction validates first)
      expect(mockValidateLlmSettings).toHaveBeenCalled()
    }, { timeout: 3000 })
    
    await waitFor(() => {
      // Then updateLlmSettings should be called if validation passes
      expect(mockUpdateLlmSettings).toHaveBeenCalled()
    }, { timeout: 3000 })
  }, 15000)

  it('should disable SubmitButton during form submission', async () => {
    let resolveUpdate: () => void
    const updatePromise = new Promise((resolve) => {
      resolveUpdate = resolve
    })
    mockUpdateLlmSettings.mockReturnValue(updatePromise as any)

    const user = userEvent.setup()
    render(<SettingsForm />)
    
    await waitFor(() => {
      expect(screen.getByLabelText(/url/i)).toBeInTheDocument()
    })
    
    const submitButton = screen.getByRole('button', { name: /save/i })
    
    await act(async () => {
      await user.click(submitButton)
    })
    
    // SubmitButton should be disabled during submission (useFormStatus)
    await waitFor(() => {
      expect(submitButton).toBeDisabled()
    })
    
    resolveUpdate!()
    await waitFor(() => {
      expect(mockUpdateLlmSettings).toHaveBeenCalled()
    })
  })

  // Note: Error handling verification has been moved to SettingsForm.integration.test.tsx
  // Unit tests focus on basic functionality, integration tests verify full error display flow
})

