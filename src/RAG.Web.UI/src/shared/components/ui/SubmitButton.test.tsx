/**
 * SubmitButton.test.tsx
 * 
 * Tests for SubmitButton component using React 19's useFormStatus hook
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useActionState } from 'react'
import { SubmitButton } from './SubmitButton'

// Mock useFormStatus - it will be tested through actual form submission
vi.mock('react-dom', async () => {
  const actual = await vi.importActual('react-dom')
  return {
    ...actual,
    // useFormStatus will work naturally with form submission
  }
})

describe('SubmitButton', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('should render button with children', () => {
    render(
      <form>
        <SubmitButton>Submit</SubmitButton>
      </form>
    )
    
    expect(screen.getByRole('button', { name: 'Submit' })).toBeInTheDocument()
  })

  it('should have type="submit"', () => {
    render(
      <form>
        <SubmitButton>Submit</SubmitButton>
      </form>
    )
    
    const button = screen.getByRole('button')
    expect(button).toHaveAttribute('type', 'submit')
  })

  it('should display loadingText when pending', async () => {
    const TestForm = () => {
      const [state, formAction] = useActionState(
        async () => {
          // Simulate async operation
          await new Promise(resolve => setTimeout(resolve, 100))
          return { success: true }
        },
        null
      )

      return (
        <form action={formAction}>
          <SubmitButton loadingText="Submitting...">Submit</SubmitButton>
        </form>
      )
    }

    const user = userEvent.setup()
    render(<TestForm />)
    
    const button = screen.getByRole('button', { name: 'Submit' })
    
    await user.click(button)
    
    // During submission, loadingText should be displayed
    await waitFor(() => {
      const loadingButton = screen.queryByRole('button', { name: /submitting/i })
      expect(loadingButton).toBeTruthy()
    }, { timeout: 2000 })
  })

  it('should be disabled when disabled prop is true', () => {
    render(
      <form>
        <SubmitButton disabled>Submit</SubmitButton>
      </form>
    )
    
    const button = screen.getByRole('button')
    expect(button).toBeDisabled()
  })

  it('should be disabled during form submission (useFormStatus)', async () => {
    const TestForm = () => {
      const [state, formAction] = useActionState(
        async () => {
          await new Promise(resolve => setTimeout(resolve, 100))
          return { success: true }
        },
        null
      )

      return (
        <form action={formAction}>
          <SubmitButton>Submit</SubmitButton>
        </form>
      )
    }

    const user = userEvent.setup()
    render(<TestForm />)
    
    const button = screen.getByRole('button', { name: 'Submit' })
    
    await user.click(button)
    
    // Button should be disabled during submission
    await waitFor(() => {
      expect(button).toBeDisabled()
    })
  })

  it('should show spinner when showSpinner is true and pending', async () => {
    const TestForm = () => {
      const [state, formAction] = useActionState(
        async () => {
          await new Promise(resolve => setTimeout(resolve, 100))
          return { success: true }
        },
        null
      )

      return (
        <form action={formAction}>
          <SubmitButton showSpinner={true}>Submit</SubmitButton>
        </form>
      )
    }

    const user = userEvent.setup()
    render(<TestForm />)
    
    const button = screen.getByRole('button', { name: 'Submit' })
    
    await user.click(button)
    
    // Spinner should appear during submission
    await waitFor(() => {
      const spinner = button.querySelector('.animate-spin')
      expect(spinner).toBeTruthy()
    })
  })

  it('should not show spinner when showSpinner is false', async () => {
    const TestForm = () => {
      const [state, formAction] = useActionState(
        async () => {
          await new Promise(resolve => setTimeout(resolve, 100))
          return { success: true }
        },
        null
      )

      return (
        <form action={formAction}>
          <SubmitButton showSpinner={false}>Submit</SubmitButton>
        </form>
      )
    }

    const user = userEvent.setup()
    render(<TestForm />)
    
    const button = screen.getByRole('button', { name: 'Submit' })
    
    await user.click(button)
    
    // Spinner should not appear
    await waitFor(() => {
      const spinner = button.querySelector('.animate-spin')
      expect(spinner).toBeFalsy()
    })
  })

  it('should apply custom className', () => {
    render(
      <form>
        <SubmitButton className="custom-class">Submit</SubmitButton>
      </form>
    )
    
    const button = screen.getByRole('button')
    expect(button).toHaveClass('custom-class')
  })

  it('should apply disabled styles when disabled', () => {
    render(
      <form>
        <SubmitButton disabled>Submit</SubmitButton>
      </form>
    )
    
    const button = screen.getByRole('button')
    expect(button).toHaveClass('opacity-50', 'cursor-not-allowed')
  })

  it('should pass through other button props', () => {
    render(
      <form>
        <SubmitButton aria-label="Submit form" data-testid="submit-btn">
          Submit
        </SubmitButton>
      </form>
    )
    
    const button = screen.getByTestId('submit-btn')
    expect(button).toHaveAttribute('aria-label', 'Submit form')
  })
})

