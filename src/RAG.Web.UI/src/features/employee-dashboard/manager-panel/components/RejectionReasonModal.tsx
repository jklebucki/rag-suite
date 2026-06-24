import { useEffect, useState } from 'react'
import { Button } from '@/shared/components/ui/Button'
import { Modal } from '@/shared/components/ui/Modal'
import { Textarea } from '@/shared/components/ui/Textarea'
import type { ApprovalRequest } from '../types/managerTypes'
import { useManagerT } from './managerTranslations'

interface RejectionReasonModalProps {
  request: ApprovalRequest | null
  isMutating: boolean
  onClose: () => void
  onConfirm: (requestId: string, reason: string) => Promise<void>
}

export function RejectionReasonModal({
  request,
  isMutating,
  onClose,
  onConfirm,
}: RejectionReasonModalProps) {
  const t = useManagerT()
  const [reason, setReason] = useState('')
  const [error, setError] = useState('')

  useEffect(() => {
    if (request) {
      setReason('')
      setError('')
    }
  }, [request])

  if (!request) return null

  async function handleConfirm() {
    if (!request) return
    const trimmedReason = reason.trim()
    if (!trimmedReason) {
      setError(t('rejection.required'))
      return
    }
    await onConfirm(request.id, trimmedReason)
  }

  return (
    <Modal
      isOpen={!!request}
      onClose={onClose}
      title={t('rejection.title')}
      size="sm"
    >
      <div className="space-y-4 p-6">
        <p className="text-sm text-gray-600 dark:text-gray-300">
          {t('rejection.message', { employee: request.employeeName })}
        </p>

        <div>
          <label
            htmlFor="rejectionReason"
            className="mb-1.5 block text-sm font-medium text-gray-700 dark:text-gray-300"
          >
            {t('rejection.reason')}
          </label>
          <Textarea
            id="rejectionReason"
            rows={4}
            value={reason}
            error={!!error}
            onChange={(event) => {
              setReason(event.target.value)
              if (error) setError('')
            }}
            placeholder={t('rejection.placeholder')}
          />
          {error && <p className="mt-1 text-xs text-red-600 dark:text-red-400">{error}</p>}
        </div>

        <div className="flex flex-col-reverse gap-3 pt-2 sm:flex-row sm:justify-end">
          <Button type="button" variant="outline" disabled={isMutating} onClick={onClose}>
            {t('common.cancel')}
          </Button>
          <Button type="button" variant="destructive" disabled={isMutating} onClick={handleConfirm}>
            {t('requests.reject')}
          </Button>
        </div>
      </div>
    </Modal>
  )
}
