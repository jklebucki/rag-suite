import { describe, expect, it } from 'vitest'
import type { HrAbsenceRequest } from '../types/hrAbsenceTypes'
import {
  getHrAbsenceStatistics,
  selectHrAbsenceRequestsByCompany,
} from './hrAbsenceSelectors'

const requests: HrAbsenceRequest[] = [
  {
    id: 'a-pending-payroll',
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
    id: 'a-overdue',
    companyId: 'company-a',
    employeeId: 'employee-b',
    employeeName: 'Marek Wiśniewski',
    leaveType: 'onDemand',
    dateFrom: '2020-01-10',
    dateTo: '2020-01-10',
    days: 1,
    submittedAt: '2020-01-09T08:00:00Z',
    managerName: 'Jan Kowalski',
    status: 'pending',
    requiresPayrollClosure: false,
  },
  {
    id: 'b-approved',
    companyId: 'company-b',
    employeeId: 'employee-c',
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

describe('HR absence company scope and KPI selectors', () => {
  it('never mixes requests from different companies', () => {
    const selected = selectHrAbsenceRequestsByCompany(requests, 'company-a')

    expect(selected).toHaveLength(2)
    expect(selected.every((request) => request.companyId === 'company-a')).toBe(true)
  })

  it('calculates all four KPI values only from the supplied company data', () => {
    const selected = selectHrAbsenceRequestsByCompany(requests, 'company-a')

    expect(getHrAbsenceStatistics(selected)).toEqual({
      all: 2,
      pending: 2,
      payrollClosure: 1,
      overdue: 1,
    })
  })
})
