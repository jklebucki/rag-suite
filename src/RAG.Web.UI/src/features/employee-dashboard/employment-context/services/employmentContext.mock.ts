import type { EmploymentContextOption } from '../types/employmentContextTypes'

const employmentContexts: EmploymentContextOption[] = [
  {
    id: 'employment-citronex-it',
    companyName: 'Citronex Sp. z o.o.',
    position: 'Specjalista IT',
    status: 'active',
    startDate: '2024-09-01',
    companyCode: 'CTX',
    employmentType: 'Umowa o prace',
  },
  {
    id: 'employment-polskie-pomidory-consultant',
    companyName: 'Polskie Pomidory Sp. z o.o.',
    position: 'Konsultant IT',
    status: 'active',
    startDate: '2025-01-01',
    companyCode: 'PPO',
    employmentType: 'Kontrakt B2B',
  },
]

export async function getEmploymentContexts(_userId: string): Promise<EmploymentContextOption[]> {
  await new Promise((resolve) => setTimeout(resolve, 150))
  return structuredClone(employmentContexts)
}
