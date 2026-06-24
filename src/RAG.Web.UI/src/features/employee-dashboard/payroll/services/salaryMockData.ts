import type { SalaryPageData, SalaryPayment } from '../types/salaryTypes'

const salaryHistory: SalaryPayment[] = [
  {
    id: 'pay-2026-05',
    grossAmount: 14200,
    netAmount: 10084.62,
    currency: 'PLN',
    paymentDate: '2026-06-10',
    periodLabel: '05/2026',
    paymentType: 'base_salary',
    status: 'paid',
    payslipDocumentId: 'payslip-2026-05',
    details: {
      baseSalary: 13200,
      bonus: 600,
      allowances: 250,
      overtime: 150,
      deductions: 120,
      socialContributions: 1946.82,
      healthContribution: 906.42,
      tax: 1142.14,
    },
  },
  {
    id: 'pay-2026-04',
    grossAmount: 13950,
    netAmount: 9921.28,
    currency: 'PLN',
    paymentDate: '2026-05-10',
    periodLabel: '04/2026',
    paymentType: 'base_salary',
    status: 'paid',
    payslipDocumentId: 'payslip-2026-04',
    details: {
      baseSalary: 13200,
      bonus: 450,
      allowances: 300,
      overtime: 0,
      deductions: 80,
      socialContributions: 1912.55,
      healthContribution: 889.21,
      tax: 1146.96,
    },
  },
  {
    id: 'pay-2026-03-correction',
    grossAmount: 480,
    netAmount: 341.2,
    currency: 'PLN',
    paymentDate: '2026-04-18',
    periodLabel: '03/2026',
    paymentType: 'correction',
    status: 'correction',
    payslipDocumentId: 'payslip-2026-03-correction',
    details: {
      baseSalary: 0,
      bonus: 0,
      allowances: 480,
      overtime: 0,
      deductions: 0,
      socialContributions: 65.81,
      healthContribution: 30.71,
      tax: 42.28,
    },
  },
  {
    id: 'pay-2026-03',
    grossAmount: 13600,
    netAmount: 9672.45,
    currency: 'PLN',
    paymentDate: '2026-04-10',
    periodLabel: '03/2026',
    paymentType: 'base_salary',
    status: 'paid',
    payslipDocumentId: 'payslip-2026-03',
    details: {
      baseSalary: 13200,
      bonus: 0,
      allowances: 250,
      overtime: 150,
      deductions: 100,
      socialContributions: 1864.56,
      healthContribution: 870.19,
      tax: 1092.8,
    },
  },
  {
    id: 'pay-2026-06',
    grossAmount: 14200,
    netAmount: 10084.62,
    currency: 'PLN',
    paymentDate: '2026-07-10',
    periodLabel: '06/2026',
    paymentType: 'base_salary',
    status: 'pending',
    details: {
      baseSalary: 13200,
      bonus: 600,
      allowances: 250,
      overtime: 150,
      deductions: 120,
      socialContributions: 1946.82,
      healthContribution: 906.42,
      tax: 1142.14,
    },
  },
  {
    id: 'pay-2025-12-bonus',
    grossAmount: 2500,
    netAmount: 1776.45,
    currency: 'PLN',
    paymentDate: '2026-01-15',
    periodLabel: '12/2025',
    paymentType: 'annual_bonus',
    status: 'cancelled',
    details: {
      baseSalary: 0,
      bonus: 2500,
      allowances: 0,
      overtime: 0,
      deductions: 0,
      socialContributions: 342.75,
      healthContribution: 159.52,
      tax: 221.28,
    },
  },
]

const salaryPageData: SalaryPageData = {
  latestPayment: salaryHistory[0],
  bankAccount: {
    bankName: 'PKO Bank Polski',
    maskedAccountNumber: 'PL12 **** **** **** **** **** 1234',
    payoutMethod: 'Przelew bankowy',
    lastUpdatedAt: '2026-02-14',
  },
  paymentHistory: salaryHistory,
  canViewSalaryData: true,
  canDownloadPayslip: true,
}

export async function getSalaryPageData(_userId: string): Promise<SalaryPageData> {
  await new Promise((resolve) => setTimeout(resolve, 450))
  return structuredClone(salaryPageData)
}

export async function downloadPayslipPdf(
  _userId: string,
  _paymentId: string
): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve, 250))
}
