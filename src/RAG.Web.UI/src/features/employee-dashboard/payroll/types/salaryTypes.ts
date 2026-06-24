export type SalaryStatus = 'paid' | 'pending' | 'cancelled' | 'correction'

export type SalaryPaymentType =
  | 'base_salary'
  | 'bonus'
  | 'correction'
  | 'annual_bonus'

export interface SalaryPaymentDetails {
  baseSalary: number
  bonus: number
  allowances: number
  overtime: number
  deductions: number
  socialContributions: number
  healthContribution: number
  tax: number
}

export interface SalaryPayment {
  id: string
  grossAmount: number
  netAmount: number
  currency: string
  paymentDate: string
  periodLabel: string
  paymentType: SalaryPaymentType
  status: SalaryStatus
  details: SalaryPaymentDetails
  payslipDocumentId?: string
}

export interface SalaryBankAccount {
  bankName: string
  maskedAccountNumber: string
  payoutMethod: string
  lastUpdatedAt: string
}

export interface SalaryPageData {
  latestPayment: SalaryPayment
  bankAccount: SalaryBankAccount
  paymentHistory: SalaryPayment[]
  canViewSalaryData: boolean
  canDownloadPayslip: boolean
}
