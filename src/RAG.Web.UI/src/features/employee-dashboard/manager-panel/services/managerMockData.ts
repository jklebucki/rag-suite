import type {
  ApprovalDelegation,
  ApprovalRequest,
  DelegationPayload,
  ManagerPanelData,
  OperationLogEntry,
  TeamMember,
} from '../types/managerTypes'

const teamMembers: TeamMember[] = [
  {
    id: 'emp-001',
    fullName: 'Anna Nowak',
    position: 'Starsza Specjalistka ds. HR',
    department: 'HR Operations',
    seniority: '6 lat 4 mies.',
    presenceStatus: 'present',
    remainingLeaveDays: 12,
    absenceDaysThisYear: 8,
    email: 'anna.nowak@citronex.pl',
    phone: '+48 601 214 510',
    currentProject: 'Digitalizacja akt pracowniczych',
  },
  {
    id: 'emp-002',
    fullName: 'Marek Kowalczyk',
    position: 'Koordynator ds. Administracji',
    department: 'Administracja',
    seniority: '3 lata 9 mies.',
    presenceStatus: 'vacation',
    remainingLeaveDays: 7,
    absenceDaysThisYear: 14,
    email: 'marek.kowalczyk@citronex.pl',
    phone: '+48 601 214 511',
    currentProject: 'Umowy serwisowe Q3',
  },
  {
    id: 'emp-003',
    fullName: 'Karolina Zielinska',
    position: 'Analityczka Procesow',
    department: 'Centrum Uslug Wspolnych',
    seniority: '2 lata 2 mies.',
    presenceStatus: 'homeOffice',
    remainingLeaveDays: 18,
    absenceDaysThisYear: 3,
    email: 'karolina.zielinska@citronex.pl',
    phone: '+48 601 214 512',
    currentProject: 'Mapowanie procesow ERP',
  },
  {
    id: 'emp-004',
    fullName: 'Piotr Lewandowski',
    position: 'Specjalista ds. Rozliczen',
    department: 'Finanse',
    seniority: '4 lata 1 mies.',
    presenceStatus: 'delegation',
    remainingLeaveDays: 10,
    absenceDaysThisYear: 6,
    email: 'piotr.lewandowski@citronex.pl',
    phone: '+48 601 214 513',
    currentProject: 'Audyt kosztow delegacji',
  },
  {
    id: 'emp-005',
    fullName: 'Ewa Malinowska',
    position: 'Mlodsza Specjalistka ds. Kadr',
    department: 'HR Operations',
    seniority: '1 rok 8 mies.',
    presenceStatus: 'absence',
    remainingLeaveDays: 21,
    absenceDaysThisYear: 5,
    email: 'ewa.malinowska@citronex.pl',
    phone: '+48 601 214 514',
    currentProject: 'Onboarding sezonowy',
  },
  {
    id: 'emp-006',
    fullName: 'Tomasz Wrobel',
    position: 'Lider Zmiany',
    department: 'Logistyka',
    seniority: '8 lat 7 mies.',
    presenceStatus: 'present',
    remainingLeaveDays: 9,
    absenceDaysThisYear: 11,
    email: 'tomasz.wrobel@citronex.pl',
    phone: '+48 601 214 515',
    currentProject: 'Grafiki magazynowe Q3',
  },
]

let approvalRequests: ApprovalRequest[] = [
  {
    id: 'ar-001',
    employeeId: 'emp-001',
    employeeName: 'Anna Nowak',
    leaveType: 'annual',
    dateFrom: '2026-07-06',
    dateTo: '2026-07-10',
    daysCount: 5,
    submittedAt: '2026-06-18T09:24:00.000Z',
    status: 'pending',
    employeeComment: 'Planowany urlop rodzinny. Dokumenty do projektu zostana przekazane przed wyjazdem.',
    hasConflict: true,
    conflictNote: 'W tym samym terminie urlop planuje Marek Kowalczyk z tego samego obszaru zadan.',
  },
  {
    id: 'ar-002',
    employeeId: 'emp-003',
    employeeName: 'Karolina Zielinska',
    leaveType: 'homeOffice',
    dateFrom: '2026-06-29',
    dateTo: '2026-06-30',
    daysCount: 2,
    submittedAt: '2026-06-20T12:10:00.000Z',
    status: 'pending',
    employeeComment: 'Praca koncepcyjna nad analiza procesow, bez spotkan stacjonarnych.',
    hasConflict: false,
  },
  {
    id: 'ar-003',
    employeeId: 'emp-004',
    employeeName: 'Piotr Lewandowski',
    leaveType: 'delegation',
    dateFrom: '2026-07-02',
    dateTo: '2026-07-03',
    daysCount: 2,
    submittedAt: '2026-06-21T08:15:00.000Z',
    status: 'pending',
    employeeComment: 'Spotkanie uzgodnieniowe z dostawca systemu rozliczen.',
    hasConflict: false,
  },
  {
    id: 'ar-004',
    employeeId: 'emp-006',
    employeeName: 'Tomasz Wrobel',
    leaveType: 'onDemand',
    dateFrom: '2026-06-26',
    dateTo: '2026-06-26',
    daysCount: 1,
    submittedAt: '2026-06-24T06:45:00.000Z',
    status: 'pending',
    employeeComment: 'Pilna sprawa rodzinna.',
    hasConflict: false,
  },
]

let operationLogs: OperationLogEntry[] = [
  {
    id: 'log-001',
    date: '2026-06-10T10:22:00.000Z',
    user: 'Jan Kowalski',
    operation: 'Akceptacja urlopu Anny Nowak',
  },
  {
    id: 'log-002',
    date: '2026-06-11T14:40:00.000Z',
    user: 'Anna Nowak',
    operation: 'Odrzucenie wniosku Marka Kowalczyka',
  },
  {
    id: 'log-003',
    date: '2026-06-17T09:05:00.000Z',
    user: 'Jan Kowalski',
    operation: 'Aktualizacja zastępstwa akceptacyjnego',
  },
]

let delegations: ApprovalDelegation[] = [
  {
    id: 'del-001',
    substituteId: 'emp-001',
    substituteName: 'Anna Nowak',
    dateFrom: '2026-07-01',
    dateTo: '2026-07-15',
    status: 'active',
    createdAt: '2026-06-17T09:05:00.000Z',
  },
  {
    id: 'del-002',
    substituteId: 'emp-006',
    substituteName: 'Tomasz Wrobel',
    dateFrom: '2026-08-05',
    dateTo: '2026-08-12',
    status: 'scheduled',
    createdAt: '2026-06-19T11:30:00.000Z',
  },
  {
    id: 'del-003',
    substituteId: 'emp-003',
    substituteName: 'Karolina Zielinska',
    dateFrom: '2026-05-06',
    dateTo: '2026-05-09',
    status: 'expired',
    createdAt: '2026-04-28T13:12:00.000Z',
  },
]

function buildStatistics() {
  return {
    directReports: 18,
    pendingRequests: approvalRequests.filter((request) => request.status === 'pending').length,
    absentToday: 2,
    absentNextSevenDays: 5,
    vacationConflicts: approvalRequests.filter((request) => request.hasConflict && request.status === 'pending').length,
  }
}

function buildData(): ManagerPanelData {
  return {
    managerName: 'Jan Kowalski',
    teamName: 'Centrum Uslug Wspolnych',
    statistics: buildStatistics(),
    teamMembers,
    approvalRequests,
    operationLogs,
    delegations,
    activeDelegation: delegations.find((delegation) => delegation.status === 'active') ?? null,
  }
}

export async function getManagerPanelData(_managerId: string): Promise<ManagerPanelData> {
  await new Promise((resolve) => setTimeout(resolve, 500))
  return buildData()
}

export async function approveApprovalRequest(
  _managerId: string,
  requestId: string
): Promise<ApprovalRequest> {
  await new Promise((resolve) => setTimeout(resolve, 350))
  approvalRequests = approvalRequests.map((request) =>
    request.id === requestId ? { ...request, status: 'approved' } : request
  )
  const updated = approvalRequests.find((request) => request.id === requestId)
  if (!updated) throw new Error('Nie znaleziono wniosku')

  operationLogs = [
    {
      id: `log-${Date.now()}`,
      date: new Date().toISOString(),
      user: 'Jan Kowalski',
      operation: `Akceptacja wniosku: ${updated.employeeName}`,
    },
    ...operationLogs,
  ]

  return updated
}

export async function rejectApprovalRequest(
  _managerId: string,
  requestId: string,
  reason: string
): Promise<ApprovalRequest> {
  await new Promise((resolve) => setTimeout(resolve, 350))
  approvalRequests = approvalRequests.map((request) =>
    request.id === requestId ? { ...request, status: 'rejected' } : request
  )
  const updated = approvalRequests.find((request) => request.id === requestId)
  if (!updated) throw new Error('Nie znaleziono wniosku')

  operationLogs = [
    {
      id: `log-${Date.now()}`,
      date: new Date().toISOString(),
      user: 'Jan Kowalski',
      operation: `Odrzucenie wniosku: ${updated.employeeName}. Powod: ${reason}`,
    },
    ...operationLogs,
  ]

  return updated
}

export async function saveApprovalDelegation(
  _managerId: string,
  payload: DelegationPayload
): Promise<ApprovalDelegation> {
  await new Promise((resolve) => setTimeout(resolve, 400))
  const substitute = teamMembers.find((member) => member.id === payload.substituteId)
  if (!substitute) throw new Error('Nie znaleziono zastępcy')

  delegations = delegations.map((delegation) =>
    delegation.status === 'active' ? { ...delegation, status: 'expired' } : delegation
  )

  const delegation: ApprovalDelegation = {
    id: `del-${Date.now()}`,
    substituteId: substitute.id,
    substituteName: substitute.fullName,
    dateFrom: payload.dateFrom,
    dateTo: payload.dateTo,
    status: 'active',
    createdAt: new Date().toISOString(),
  }

  delegations = [delegation, ...delegations]
  operationLogs = [
    {
      id: `log-${Date.now()}`,
      date: new Date().toISOString(),
      user: 'Jan Kowalski',
      operation: `Ustawienie zastępstwa: ${delegation.substituteName}`,
    },
    ...operationLogs,
  ]

  return delegation
}
