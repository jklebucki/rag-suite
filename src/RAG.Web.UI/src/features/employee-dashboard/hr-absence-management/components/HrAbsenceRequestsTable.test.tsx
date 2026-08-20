import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { I18nProvider } from '@/shared/contexts/I18nContext'
import type { HrAbsenceRequest } from '../types/hrAbsenceTypes'
import { HrAbsenceRequestsTable } from './HrAbsenceRequestsTable'

const requests: HrAbsenceRequest[] = [
  {
    id: 'pending-payroll',
    companyId: 'company-a',
    employeeId: 'employee-a',
    employeeName: 'Anna Nowak',
    leaveType: 'annual',
    dateFrom: '2099-01-10',
    dateTo: '2099-01-12',
    days: 3,
    submittedAt: '2098-12-01T08:00:00Z',
    managerName: 'Jan Kowalski',
    status: 'pending',
    requiresPayrollClosure: true,
  },
  {
    id: 'approved',
    companyId: 'company-a',
    employeeId: 'employee-b',
    employeeName: 'Joanna Wójcik',
    leaveType: 'childCare',
    dateFrom: '2020-01-10',
    dateTo: '2020-01-11',
    days: 2,
    submittedAt: '2020-01-01T08:00:00Z',
    managerName: 'Ewa Zielińska',
    status: 'approved',
    requiresPayrollClosure: false,
  },
]

function renderTable(initialFilters: Parameters<typeof HrAbsenceRequestsTable>[0]['initialFilters']) {
  localStorage.setItem('rag-suite-language', 'pl')
  return render(
    <I18nProvider>
      <HrAbsenceRequestsTable requests={requests} initialFilters={initialFilters} />
    </I18nProvider>
  )
}

describe('HrAbsenceRequestsTable KPI filters', () => {
  it('applies the pending status passed from a KPI tile', () => {
    renderTable({ status: 'pending' })

    expect(screen.getAllByText('Anna Nowak')).not.toHaveLength(0)
    expect(screen.queryByText('Joanna Wójcik')).not.toBeInTheDocument()
  })

  it('applies the payroll-closure category passed from a KPI tile', () => {
    renderTable({ category: 'payrollClosure' })

    expect(screen.getAllByText('Anna Nowak')).not.toHaveLength(0)
    expect(screen.queryByText('Joanna Wójcik')).not.toBeInTheDocument()
    expect(screen.getByRole('checkbox', { name: /przed listą płac/i })).toBeChecked()
  })
})
