import React from 'react'
import { AlertTriangle, RefreshCw, LogOut } from 'lucide-react'
import { Modal } from './Modal'
import { useI18n } from '@/shared/contexts/I18nContext'

interface SessionExpiredModalProps {
  isOpen: boolean
  onClose: () => void
  onTryAgain: () => void
  onLogout: () => void
}

export function SessionExpiredModal({
  isOpen,
  onClose,
  onTryAgain,
  onLogout
}: SessionExpiredModalProps) {
  const { t } = useI18n()

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={t('session.expired.title')}
      size="md"
    >
      <div className="p-6">
        {/* Warning Icon */}
        <div className="flex items-center justify-center mb-6">
          <div className="flex items-center justify-center w-16 h-16 bg-orange-100 rounded-full">
            <AlertTriangle className="w-8 h-8 text-orange-600" />
          </div>
        </div>

        {/* Message */}
        <div className="text-center mb-8">
          <p className="text-gray-600 text-lg leading-relaxed">
            {t('session.expired.message')}
          </p>
        </div>

        {/* Action Buttons */}
        <div className="flex flex-col sm:flex-row gap-3">
          <button
            onClick={onTryAgain}
            className="flex-1 flex items-center justify-center gap-2 px-4 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors font-medium"
          >
            <RefreshCw className="w-5 h-5" />
            {t('session.expired.try_again')}
          </button>

          <button
            onClick={onLogout}
            className="flex-1 flex items-center justify-center gap-2 px-4 py-3 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors font-medium"
          >
            <LogOut className="w-5 h-5" />
            {t('session.expired.logout')}
          </button>
        </div>

        {/* Footer Note */}
        <div className="mt-6 text-center">
          <p className="text-sm text-gray-500">
            Możesz również zamknąć to okno i spróbować ponownie później.
          </p>
        </div>
      </div>
    </Modal>
  )
}