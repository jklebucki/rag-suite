// ---------------------------------------------------------------------------
// Leave Request – mock service
//
// Replace the body of `getLeaveRequestPageData` and `createLeaveRequest`
// with real API calls when the backend is ready. Function signatures and
// return types must remain unchanged so the hook and UI require zero edits.
//
// Example real implementations:
//
//   export async function getLeaveRequestPageData(
//     userId: string
//   ): Promise<LeaveRequestPageData> {
//     const response = await apiClient.get<LeaveRequestPageData>(
//       `/api/v1/employees/${userId}/leave-requests`
//     )
//     return response.data
//   }
//
//   export async function createLeaveRequest(
//     userId: string,
//     payload: CreateLeaveRequestPayload
//   ): Promise<LeaveRequestRecord> {
//     const response = await apiClient.post<LeaveRequestRecord>(
//       `/api/v1/employees/${userId}/leave-requests`,
//       payload
//     )
//     return response.data
//   }
//
//   export async function cancelLeaveRequest(
//     userId: string,
//     requestId: string
//   ): Promise<void> {
//     await apiClient.patch(
//       `/api/v1/employees/${userId}/leave-requests/${requestId}/cancel`
//     )
//   }
// ---------------------------------------------------------------------------

import type {
  LeaveRequestPageData,
  LeaveRequestRecord,
  CreateLeaveRequestPayload,
} from '../types/leaveRequest'

const MOCK_DATA: LeaveRequestPageData = {
  leaveBalance: {
    annual: 14,
    carryover: 2,
    onDemand: 4,
    total: 20,
  },

  companies: [
    { id: 'company-citronex', name: 'Citronex Sp. z o.o.' },
    { id: 'company-polskie-pomidory', name: 'Polskie Pomidory Sp. z o.o.' },
  ],

  substitutes: [
    { id: 'sub-001', fullName: 'Marek Nowak', position: 'Inżynier Oprogramowania' },
    { id: 'sub-002', fullName: 'Katarzyna Wiśniewska', position: 'Starszy Programista' },
    { id: 'sub-003', fullName: 'Tomasz Zając', position: 'Analityk Biznesowy' },
    { id: 'sub-004', fullName: 'Agnieszka Dąbrowska', position: 'Project Manager' },
    { id: 'sub-005', fullName: 'Piotr Lewandowski', position: 'DevOps Engineer' },
  ],

  requests: [
    {
      id: 'lr-001',
      companyId: 'company-citronex',
      companyName: 'Citronex Sp. z o.o.',
      leaveType: 'annual',
      dateFrom: '2025-06-16',
      dateTo: '2025-06-20',
      daysCount: 5,
      status: 'approved',
      substituteId: 'sub-001',
      substituteName: 'Marek Nowak',
      comment: 'Urlop letni – wakacje rodzinne.',
      createdAt: '2025-05-20T10:15:00.000Z',
      reviewedAt: '2025-05-22T09:00:00.000Z',
      reviewedBy: 'Jarosław Kłębucki',
      managerComment: 'Zatwierdzone. Miłego wypoczynku!',
    },
    {
      id: 'lr-002',
      companyId: 'company-citronex',
      companyName: 'Citronex Sp. z o.o.',
      leaveType: 'onDemand',
      dateFrom: '2025-04-07',
      dateTo: '2025-04-07',
      daysCount: 1,
      status: 'approved',
      createdAt: '2025-04-07T06:45:00.000Z',
      reviewedAt: '2025-04-07T08:00:00.000Z',
      reviewedBy: 'Jarosław Kłębucki',
    },
    {
      id: 'lr-003',
      companyId: 'company-polskie-pomidory',
      companyName: 'Polskie Pomidory Sp. z o.o.',
      leaveType: 'occasional',
      dateFrom: '2025-03-14',
      dateTo: '2025-03-15',
      daysCount: 2,
      status: 'rejected',
      comment: 'Ślub brata.',
      createdAt: '2025-03-01T11:30:00.000Z',
      reviewedAt: '2025-03-05T14:20:00.000Z',
      reviewedBy: 'Jarosław Kłębucki',
      managerComment: 'Przepraszam, ale w tym terminie nie mogę zatwierdzić ze względu na kluczowy projekt.',
    },
    {
      id: 'lr-004',
      companyId: 'company-citronex',
      companyName: 'Citronex Sp. z o.o.',
      leaveType: 'annual',
      dateFrom: '2025-07-21',
      dateTo: '2025-08-01',
      daysCount: 10,
      status: 'pending',
      substituteId: 'sub-002',
      substituteName: 'Katarzyna Wiśniewska',
      createdAt: '2025-06-10T08:00:00.000Z',
    },
    {
      id: 'lr-005',
      companyId: 'company-polskie-pomidory',
      companyName: 'Polskie Pomidory Sp. z o.o.',
      leaveType: 'homeOffice',
      dateFrom: '2025-06-25',
      dateTo: '2025-06-25',
      daysCount: 1,
      status: 'pending',
      createdAt: '2025-06-18T16:00:00.000Z',
    },
    {
      id: 'lr-006',
      companyId: 'company-citronex',
      companyName: 'Citronex Sp. z o.o.',
      leaveType: 'delegation',
      dateFrom: '2025-02-10',
      dateTo: '2025-02-12',
      daysCount: 3,
      status: 'cancelled',
      comment: 'Delegacja do Gdańska – konferencja IT.',
      createdAt: '2025-01-28T09:00:00.000Z',
    },
  ],
}

// In-memory mutable copy for CRUD simulation
let mockRequests = [...MOCK_DATA.requests]

export async function getLeaveRequestPageData(
  _userId: string
): Promise<LeaveRequestPageData> {
  // Simulate network latency
  await new Promise((resolve) => setTimeout(resolve, 600))
  return { ...MOCK_DATA, requests: [...mockRequests] }
}

export async function createLeaveRequest(
  _userId: string,
  payload: CreateLeaveRequestPayload
): Promise<LeaveRequestRecord> {
  await new Promise((resolve) => setTimeout(resolve, 800))

  const substitute = payload.substituteId
    ? MOCK_DATA.substitutes.find((s) => s.id === payload.substituteId)
    : undefined
  const company = MOCK_DATA.companies.find((c) => c.id === payload.companyId)

  const newRecord: LeaveRequestRecord = {
    id: `lr-${Date.now()}`,
    companyId: payload.companyId,
    companyName: company?.name ?? payload.companyId,
    leaveType: payload.leaveType,
    dateFrom: payload.dateFrom,
    dateTo: payload.dateTo,
    daysCount: payload.daysCount,
    status: 'pending',
    substituteId: payload.substituteId,
    substituteName: substitute?.fullName,
    comment: payload.comment,
    createdAt: new Date().toISOString(),
  }

  mockRequests = [newRecord, ...mockRequests]
  return newRecord
}

export async function cancelLeaveRequest(
  _userId: string,
  requestId: string
): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve, 500))
  mockRequests = mockRequests.map((r) =>
    r.id === requestId ? { ...r, status: 'cancelled' } : r
  )
}
