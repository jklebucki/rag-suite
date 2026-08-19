import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { ApprovalRequest, TeamMember } from '../types/managerTypes'
import { PendingRequestsTable } from './PendingRequestsTable'
import { TeamMembersTable } from './TeamMembersTable'

vi.mock('@/shared/contexts/I18nContext', () => ({
  useI18n: () => ({ language: 'en' }),
}))

const members: TeamMember[] = [
  {
    id: 'employee-1',
    fullName: 'Anna Nowak',
    position: 'Specialist',
    department: 'HR',
    seniority: '2 years',
    presenceStatus: 'present',
    remainingLeaveDays: 10,
    absenceDaysThisYear: 2,
    email: 'anna@example.com',
    phone: '123',
    currentProject: 'Project A',
  },
  {
    id: 'employee-2',
    fullName: 'Marek Kowalczyk',
    position: 'Coordinator',
    department: 'Administration',
    seniority: '3 years',
    presenceStatus: 'vacation',
    remainingLeaveDays: 7,
    absenceDaysThisYear: 4,
    email: 'marek@example.com',
    phone: '456',
    currentProject: 'Project B',
  },
]

const requests: ApprovalRequest[] = [
  {
    id: 'request-1',
    employeeId: 'employee-1',
    employeeName: 'Anna Nowak',
    leaveType: 'annual',
    dateFrom: '2026-07-06',
    dateTo: '2026-07-10',
    daysCount: 5,
    submittedAt: '2026-06-18T09:24:00.000Z',
    status: 'pending',
    employeeComment: '',
    hasConflict: true,
  },
  {
    id: 'request-2',
    employeeId: 'employee-2',
    employeeName: 'Marek Kowalczyk',
    leaveType: 'onDemand',
    dateFrom: '2026-07-01',
    dateTo: '2026-07-01',
    daysCount: 1,
    submittedAt: '2026-06-19T09:24:00.000Z',
    status: 'pending',
    employeeComment: '',
    hasConflict: false,
  },
]

describe('manager table filters', () => {
  it('opens the team view with the status filter provided by the dashboard tile', () => {
    const { container } = render(
      <TeamMembersTable members={members} initialStatusFilter="vacation" />
    )
    const desktopTable = container.querySelector('table')

    expect(desktopTable).not.toBeNull()
    expect(within(desktopTable!).getByText('Marek Kowalczyk')).toBeInTheDocument()
    expect(within(desktopTable!).queryByText('Anna Nowak')).not.toBeInTheDocument()
  })

  it('combines column filtering with sorting', async () => {
    const user = userEvent.setup()
    const { container } = render(<TeamMembersTable members={members} />)
    const desktopTable = container.querySelector('table')!
    const employeeFilter = within(desktopTable).getByPlaceholderText('Search in employee...')

    await user.type(employeeFilter, 'Marek')
    await user.click(within(desktopTable).getByText('Employee'))

    expect(within(desktopTable).getByText('Marek Kowalczyk')).toBeInTheDocument()
    expect(within(desktopTable).queryByText('Anna Nowak')).not.toBeInTheDocument()
  })

  it('opens leave conflicts with the conflicts-only filter enabled', () => {
    const { container } = render(
      <PendingRequestsTable
        requests={requests}
        isMutating={false}
        onViewDetails={vi.fn()}
        onApprove={vi.fn()}
        onReject={vi.fn()}
        leaveOnly
        initialConflictsOnly
      />
    )
    const desktopTable = container.querySelector('table')

    expect(screen.getByRole('checkbox', { name: 'Conflicts only' })).toBeChecked()
    expect(within(desktopTable!).getByText('Anna Nowak')).toBeInTheDocument()
    expect(within(desktopTable!).queryByText('Marek Kowalczyk')).not.toBeInTheDocument()
  })
})
