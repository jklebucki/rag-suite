import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { ManagerPanelData } from '../types/managerTypes'
import { ManagerDashboard } from './ManagerDashboard'

vi.mock('@/shared/contexts/I18nContext', () => ({
  useI18n: () => ({ language: 'en' }),
}))

const data: ManagerPanelData = {
  managerName: 'Jan Kowalski',
  teamName: 'Team',
  statistics: {
    directReports: 2,
    pendingRequests: 1,
    absentToday: 1,
    absentNextSevenDays: 0,
    vacationConflicts: 1,
  },
  teamMembers: [
    {
      id: 'employee-1',
      fullName: 'Anna Nowak',
      position: 'Specialist',
      department: 'HR',
      seniority: '2 years',
      presenceStatus: 'vacation',
      remainingLeaveDays: 10,
      absenceDaysThisYear: 2,
      email: 'anna@example.com',
      phone: '123',
      currentProject: 'Project',
    },
    {
      id: 'employee-2',
      fullName: 'Ewa Malinowska',
      position: 'Specialist',
      department: 'HR',
      seniority: '1 year',
      presenceStatus: 'absence',
      remainingLeaveDays: 12,
      absenceDaysThisYear: 5,
      email: 'ewa@example.com',
      phone: '456',
      currentProject: 'Project',
    },
  ],
  approvalRequests: [],
  operationLogs: [],
  delegations: [],
  activeDelegation: null,
}

describe('ManagerDashboard', () => {
  it('navigates from statistic tiles to the expected filtered views', async () => {
    const user = userEvent.setup()
    const onNavigate = vi.fn()
    render(<ManagerDashboard data={data} onNavigate={onNavigate} />)

    await user.click(screen.getByRole('button', { name: /my team/i }))
    await user.click(screen.getByRole('button', { name: /pending requests/i }))
    await user.click(screen.getByRole('button', { name: /on leave today/i }))
    await user.click(screen.getByRole('button', { name: /sick leave/i }))
    await user.click(screen.getByRole('button', { name: /leave conflicts/i }))

    expect(onNavigate.mock.calls.map(([target]) => target)).toEqual([
      { tab: 'team' },
      { tab: 'requests', status: 'pending' },
      { tab: 'team', presenceStatus: 'vacation' },
      { tab: 'team', presenceStatus: 'absence' },
      { tab: 'leaveRequests', conflictsOnly: true },
    ])
  })
})
