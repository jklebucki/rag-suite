// ---------------------------------------------------------------------------
// Employee Dashboard – mock service
//
// Replace the body of `getEmployeeDashboardData` with a real API call when
// the backend is ready. The function signature and return type must stay the
// same so the hook and UI layer require zero changes.
//
// Example real implementation:
//   const response = await apiClient.get<EmployeeDashboardData>(
//     `/api/v1/employee-dashboard/${userId}`
//   )
//   return response.data
// ---------------------------------------------------------------------------

import type { EmployeeDashboardData } from '../types/employeeDashboard'

const MOCK_DATA: EmployeeDashboardData = {
  profile: {
    id: 'emp-001',
    firstName: 'Anna',
    lastName: 'Kowalska',
    fullName: 'Anna Kowalska',
    position: 'Starszy Inżynier Oprogramowania',
    department: 'IT / Dział IT',
    supervisor: 'Jarosław Kłębucki',
    hireDate: '2019-03-01',
    phone: '+48 600 123 456',
    email: 'anna.kowalska@citronex.pl',
    avatarUrl: undefined,
    lastLoginAt: new Date(Date.now() - 1000 * 60 * 30).toISOString(), // 30 min ago
  },

  leaveBalance: {
    annual: 14,
    carryover: 2,
    onDemand: 4,
    total: 20,
  },

  lastPayslip: {
    grossAmount: 12500,
    netAmount: 8934.5,
    currency: 'PLN',
    paymentDate: '2025-05-09',
    periodLabel: 'Maj 2025',
    downloadUrl: undefined, // set to a real URL when backend is connected
  },

  hrRequestsSummary: {
    pending: 1,
    approved: 8,
    rejected: 2,
    total: 11,
  },

  notifications: [
    {
      id: 'notif-1',
      date: new Date(Date.now() - 1000 * 60 * 60 * 2).toISOString(),
      category: 'leave',
      title: 'Wniosek urlopowy zaakceptowany',
      description: 'Twój wniosek o urlop wypoczynkowy na 15–22 czerwca 2025 r. został zaakceptowany.',
      severity: 'success',
      isRead: false,
    },
    {
      id: 'notif-2',
      date: new Date(Date.now() - 1000 * 60 * 60 * 24).toISOString(),
      category: 'payslip',
      title: 'Nowy pasek płacowy dostępny',
      description: 'Twój pasek płacowy za maj 2025 r. jest już dostępny do pobrania.',
      severity: 'info',
      isRead: false,
    },
    {
      id: 'notif-3',
      date: new Date(Date.now() - 1000 * 60 * 60 * 48).toISOString(),
      category: 'medical',
      title: 'Przypomnienie o badaniach lekarskich',
      description: 'Termin Twoich okresowych badań lekarskich upływa za 30 dni (18 lipca 2025 r.).',
      severity: 'warning',
      isRead: true,
    },
    {
      id: 'notif-4',
      date: new Date(Date.now() - 1000 * 60 * 60 * 72).toISOString(),
      category: 'training',
      title: 'Wygasające szkolenie BHP',
      description: 'Twoje zaświadczenie o szkoleniu BHP traci ważność 10 sierpnia 2025 r.',
      severity: 'warning',
      isRead: true,
    },
    {
      id: 'notif-5',
      date: new Date(Date.now() - 1000 * 60 * 60 * 96).toISOString(),
      category: 'leave',
      title: 'Wniosek urlopowy odrzucony',
      description: 'Twój wniosek urlopowy na 1–3 maja 2025 r. został odrzucony. Skontaktuj się z przełożonym.',
      severity: 'error',
      isRead: true,
    },
  ],

  upcomingEvents: [
    {
      id: 'event-1',
      type: 'leave',
      title: 'Urlop wypoczynkowy',
      startDate: '2025-06-15',
      endDate: '2025-06-22',
      description: '8 dni urlopu wypoczynkowego',
    },
    {
      id: 'event-2',
      type: 'medical',
      title: 'Badania okresowe',
      startDate: '2025-07-18',
      description: 'Wizyta w MedCenter, ul. Zdrowa 5, Warszawa',
    },
    {
      id: 'event-3',
      type: 'bhp_training',
      title: 'Szkolenie BHP',
      startDate: '2025-08-05',
      endDate: '2025-08-05',
      description: 'Szkolenie odświeżające BHP – sesja online o godz. 10:00',
    },
    {
      id: 'event-4',
      type: 'delegation',
      title: 'Delegacja – Kraków',
      startDate: '2025-06-25',
      endDate: '2025-06-26',
      description: 'Wizyta u klienta w siedzibie TechCorp',
    },
    {
      id: 'event-5',
      type: 'organization',
      title: 'Spotkanie ogólnofirmowe',
      startDate: '2025-07-01',
      description: 'Kwartalne spotkanie całej firmy – siedziba Warszawa, 2. piętro',
    },
  ],
}

// ---------------------------------------------------------------------------
// Service function — replace with real API call when backend is ready
// ---------------------------------------------------------------------------
export async function getEmployeeDashboardData(
  _userId: string
): Promise<EmployeeDashboardData> {
  // Simulates network latency; remove when connecting to a real API
  await new Promise((resolve) => setTimeout(resolve, 600))
  return structuredClone(MOCK_DATA)
}
