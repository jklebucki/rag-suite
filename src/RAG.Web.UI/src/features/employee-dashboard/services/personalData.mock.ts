// ---------------------------------------------------------------------------
// Employee Personal Data – mock service
//
// Replace the body of `getPersonalDataPageData` with a real API call when
// the backend is ready. The function signature and return type must stay the
// same so the hook and UI layer require zero changes.
//
// Example real implementation:
//   const response = await apiClient.get<PersonalDataPageData>(
//     `/api/v1/employees/${userId}/personal-data`
//   )
//   return response.data
// ---------------------------------------------------------------------------

import type { PersonalDataPageData } from '../types/personalData'

const MOCK_DATA: PersonalDataPageData = {
  personalData: {
    basicInfo: {
      firstName: 'Anna',
      lastName: 'Kowalska',
      employeeCode: 'EMP-2019-0042',
      birthDate: '1990-04-15',
      position: 'Starszy Inżynier Oprogramowania',
      department: 'IT / Dział IT',
      supervisor: 'Jarosław Kłębucki',
      hireDate: '2019-03-01',
    },
    contactInfo: {
      workEmail: 'anna.kowalska@citronex.pl',
      privateEmail: 'anna.k.private@gmail.com',
      workPhone: '+48 600 123 456',
      privatePhone: '+48 510 987 654',
      residenceAddress: {
        street: 'ul. Różana',
        buildingNumber: '12',
        apartmentNumber: '5',
        postalCode: '00-001',
        city: 'Warszawa',
      },
      correspondenceAddress: {
        street: 'ul. Różana',
        buildingNumber: '12',
        apartmentNumber: '5',
        postalCode: '00-001',
        city: 'Warszawa',
      },
    },
    employmentInfo: {
      company: 'Citronex Sp. z o.o.',
      organizationalUnit: 'Dział IT – Backend',
      costCenter: 'CC-IT-001',
      contractType: 'Umowa o pracę na czas nieokreślony',
      workTimeFraction: 'Pełny etat (1/1)',
      employmentStatus: 'Aktywny',
      hireDate: '2019-03-01',
    },
    emergencyContact: {
      fullName: 'Piotr Kowalski',
      relationship: 'Mąż',
      phone: '+48 500 000 001',
    },
  },

  changeRequests: [
    {
      id: 'cr-001',
      date: new Date(Date.now() - 1000 * 60 * 60 * 24 * 30).toISOString(),
      changeType: 'privatePhone',
      status: 'approved',
      comment: 'Zmiana zaakceptowana przez dział HR.',
    },
    {
      id: 'cr-002',
      date: new Date(Date.now() - 1000 * 60 * 60 * 24 * 10).toISOString(),
      changeType: 'residenceAddress',
      status: 'pending',
      comment: undefined,
    },
    {
      id: 'cr-003',
      date: new Date(Date.now() - 1000 * 60 * 60 * 24 * 60).toISOString(),
      changeType: 'emergencyContact',
      status: 'rejected',
      comment: 'Brak wymaganych dokumentów potwierdzających zmianę.',
    },
  ],
}

// ---------------------------------------------------------------------------
// Service function — replace with real API call when backend is ready
// ---------------------------------------------------------------------------
export async function getPersonalDataPageData(
  _userId: string
): Promise<PersonalDataPageData> {
  // Simulates network latency; remove when connecting to a real API
  await new Promise((resolve) => setTimeout(resolve, 500))
  return structuredClone(MOCK_DATA)
}
