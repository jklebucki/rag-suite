import { useCallback, useEffect, useState } from 'react'
import { useAuth } from '@/shared/contexts/AuthContext'
import { downloadPayslipPdf, getSalaryPageData } from './salaryMockData'
import type { SalaryPageData } from './salaryTypes'

interface UseSalaryDataResult {
  data: SalaryPageData | null
  isLoading: boolean
  isDownloading: boolean
  error: string | null
  downloadMessage: string | null
  downloadPayslip: (paymentId: string) => Promise<void>
  clearDownloadMessage: () => void
  refetch: () => void
}

export function useSalaryData(): UseSalaryDataResult {
  const { user } = useAuth()
  const [data, setData] = useState<SalaryPageData | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isDownloading, setIsDownloading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [downloadMessage, setDownloadMessage] = useState<string | null>(null)
  const [revision, setRevision] = useState(0)

  useEffect(() => {
    if (!user) return

    let cancelled = false
    setIsLoading(true)
    setError(null)

    getSalaryPageData(user.id)
      .then((result) => {
        if (!cancelled) {
          setData(result)
          setIsLoading(false)
        }
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError('employeeDashboard.salary.error.loadFailed')
          setIsLoading(false)
        }
      })

    return () => {
      cancelled = true
    }
  }, [user, revision])

  const downloadPayslip = useCallback(
    async (paymentId: string) => {
      if (!user) return

      setIsDownloading(true)
      setDownloadMessage(null)

      try {
        await downloadPayslipPdf(user.id, paymentId)
        setDownloadMessage(
          'employeeDashboard.salary.pdf.backendPlaceholder'
        )
      } finally {
        setIsDownloading(false)
      }
    },
    [user]
  )

  const refetch = () => setRevision((value) => value + 1)

  return {
    data,
    isLoading,
    isDownloading,
    error,
    downloadMessage,
    downloadPayslip,
    clearDownloadMessage: () => setDownloadMessage(null),
    refetch,
  }
}
