import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { I18nProvider } from '@/shared/contexts/I18nContext'
import type { HrAbsenceRequest } from '../types/hrAbsenceTypes'
import { HrAbsenceDashboard } from './HrAbsenceDashboard'

const request: HrAbsenceRequest = {
  id: 'request-1',
  companyId: 'company-a',
  employeeId: 'employee-a',
  employeeName: 'Anna Nowak',
  leaveType: 'annual',
  dateFrom: '2020-01-10',
  dateTo: '2020-01-12',
  days: 3,
  submittedAt: '2020-01-01T08:00:00Z',
  managerName: 'Jan Kowalski',
  status: 'pending',
  requiresPayrollClosure: true,
}

describe('HrAbsenceDashboard', () => {
  it.each([
    ['Wszystkie wnioski', {}],
    ['Oczekujące', { status: 'pending' }],
    ['Do zamknięcia przed listą płac', { category: 'payrollClosure' }],
    ['Po terminie', { category: 'overdue' }],
  ])('passes the expected filter from the %s KPI tile', async (tile, expectedFilter) => {
    localStorage.setItem('rag-suite-language', 'pl')
    const onNavigate = vi.fn()
    const user = userEvent.setup()

    render(
      <I18nProvider>
        <HrAbsenceDashboard requests={[request]} onNavigate={onNavigate} />
      </I18nProvider>
    )

    await user.click(screen.getByRole('button', { name: new RegExp(tile) }))

    expect(onNavigate).toHaveBeenCalledWith(expectedFilter)
  })
})
