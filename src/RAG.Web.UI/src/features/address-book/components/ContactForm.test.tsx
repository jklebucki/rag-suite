/**
 * ContactForm.test.tsx
 * 
 * Tests for ContactForm component using useActionState and SubmitButton
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor, act } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ContactForm } from './ContactForm'
import type { ContactListItem } from '../types/addressbook'

// Mock dependencies
vi.mock('@/shared/contexts/I18nContext', () => ({
  useI18n: () => ({
    t: (key: string) => key,
    language: 'en',
  }),
}))

const mockOnSubmit = vi.fn().mockResolvedValue(undefined)
const mockOnClose = vi.fn()

const defaultProps = {
  isOpen: true,
  onClose: mockOnClose,
  onSubmit: mockOnSubmit,
  canModify: true,
}

describe('ContactForm', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('should render form when isOpen is true', () => {
    render(<ContactForm {...defaultProps} />)
    
    expect(screen.getByLabelText(/addressbook\.form\.firstname/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/addressbook\.form\.lastname/i)).toBeInTheDocument()
  })

  it('should not render form when isOpen is false', () => {
    render(<ContactForm {...defaultProps} isOpen={false} />)
    
    expect(screen.queryByLabelText(/addressbook\.form\.firstname/i)).not.toBeInTheDocument()
  })

  it('should render SubmitButton component', () => {
    render(<ContactForm {...defaultProps} />)
    
    // ContactForm uses "Create" or "Update" text, not "Save" or "Submit"
    const submitButton = screen.getByRole('button', { name: /create|update/i })
    expect(submitButton).toBeInTheDocument()
    expect(submitButton).toHaveAttribute('type', 'submit')
  })

  it('should use useActionState for form submission', async () => {
    const user = userEvent.setup()
    render(<ContactForm {...defaultProps} />)
    
    const firstNameInput = screen.getByLabelText(/addressbook\.form\.firstname/i)
    const lastNameInput = screen.getByLabelText(/addressbook\.form\.lastname/i)
    const submitButton = screen.getByRole('button', { name: /create|update/i })
    
    await act(async () => {
      await user.type(firstNameInput, 'John')
      await user.type(lastNameInput, 'Doe')
      await user.click(submitButton)
    })
    
    // useActionState should call onSubmit through formAction
    await waitFor(() => {
      expect(mockOnSubmit).toHaveBeenCalled()
    })
  })

  it('should disable SubmitButton during form submission', async () => {
    let resolveSubmit: () => void
    const submitPromise = new Promise<void>((resolve) => {
      resolveSubmit = resolve
    })
    mockOnSubmit.mockReturnValue(submitPromise)

    const user = userEvent.setup()
    render(<ContactForm {...defaultProps} />)
    
    const firstNameInput = screen.getByLabelText(/addressbook\.form\.firstname/i)
    const lastNameInput = screen.getByLabelText(/addressbook\.form\.lastname/i)
    const submitButton = screen.getByRole('button', { name: /create|update/i })
    
    await act(async () => {
      await user.type(firstNameInput, 'John')
      await user.type(lastNameInput, 'Doe')
      await user.click(submitButton)
    })
    
    // SubmitButton should be disabled during submission (useFormStatus)
    await waitFor(() => {
      expect(submitButton).toBeDisabled()
    })
    
    resolveSubmit!()
    await waitFor(() => {
      expect(mockOnSubmit).toHaveBeenCalled()
    })
  })

  it('should load contact data in edit mode', () => {
    const contact: ContactListItem = {
      id: '1',
      firstName: 'John',
      lastName: 'Doe',
      email: 'john@example.com',
      isActive: true,
    }
    
    render(<ContactForm {...defaultProps} contact={contact} />)
    
    const firstNameInput = screen.getByLabelText(/addressbook\.form\.firstname/i) as HTMLInputElement
    expect(firstNameInput.value).toBe('John')
  })

  it('should display field errors from useActionState', async () => {
    mockOnSubmit.mockRejectedValue(new Error('Validation failed'))
    
    const user = userEvent.setup()
    render(<ContactForm {...defaultProps} />)
    
    const submitButton = screen.getByRole('button', { name: /create|update/i })
    
    // Fill required fields first
    await act(async () => {
      await user.type(screen.getByLabelText(/addressbook\.form\.firstname/i), 'John')
      await user.type(screen.getByLabelText(/addressbook\.form\.lastname/i), 'Doe')
      await user.click(submitButton)
    })
    
    // useActionState should return error in state
    await waitFor(() => {
      // ContactForm displays state.error if present
      const errorMessage = screen.queryByText(/validation failed|error/i)
      expect(errorMessage).toBeTruthy()
    }, { timeout: 2000 })
  })
})

