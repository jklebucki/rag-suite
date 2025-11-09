import React, { useState } from 'react'
import { X, User, Mail, Calendar, Shield, Edit, Trash2, LogOut } from 'lucide-react'
import { useI18n } from '@/shared/contexts/I18nContext'
import { useAuth } from '@/shared/contexts/AuthContext'
import { ConfirmModal } from '@/shared/components/ui/ConfirmModal'
import { logger } from '@/utils/logger'

interface UserAccountModalProps {
  isOpen: boolean
  onClose: () => void
}

export function UserAccountModal({ isOpen, onClose }: UserAccountModalProps) {
  const { t } = useI18n()
  const { user, logoutAllDevices } = useAuth()
  const [activeTab, setActiveTab] = useState<'profile' | 'security'>('profile')
  const [showLogoutConfirm, setShowLogoutConfirm] = useState(false)
  const [isLoggingOut, setIsLoggingOut] = useState(false)

  if (!isOpen || !user) return null

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('pl-PL', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    })
  }

  const handleUpdateProfile = () => {
    // TODO: Implement profile update functionality
    logger.debug('Update profile clicked')
  }

  const handleDeleteAccount = () => {
    // TODO: Implement account deletion functionality
    logger.debug('Delete account clicked')
  }

  const handleLogoutAllDevices = () => {
    setShowLogoutConfirm(true)
  }

  const handleConfirmLogout = async () => {
    setIsLoggingOut(true)
    try {
      await logoutAllDevices()
      setShowLogoutConfirm(false)
      onClose()
    } catch (error) {
      logger.error('Failed to logout from all devices:', error)
    } finally {
      setIsLoggingOut(false)
    }
  }

  const handleCancelLogout = () => {
    setShowLogoutConfirm(false)
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 dark:bg-black/80 p-4">
      <div className="surface w-full max-w-2xl max-h-[90vh] overflow-hidden rounded-2xl shadow-xl">
        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b border-gray-200 dark:border-slate-700">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">
            {t('account.title')}
          </h2>
          <button
            onClick={onClose}
            className="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-slate-800 transition-colors"
            aria-label={t('common.close')}
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        {/* Tabs */}
        <div className="flex border-b border-gray-200 dark:border-slate-700">
          <button
            onClick={() => setActiveTab('profile')}
            className={`px-6 py-3 text-sm font-medium border-b-2 transition-colors ${
              activeTab === 'profile'
                ? 'border-primary-500 text-primary-600 dark:text-primary-300'
                : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200'
            }`}
          >
            {t('account.profile_tab')}
          </button>
          <button
            onClick={() => setActiveTab('security')}
            className={`px-6 py-3 text-sm font-medium border-b-2 transition-colors ${
              activeTab === 'security'
                ? 'border-primary-500 text-primary-600 dark:text-primary-300'
                : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200'
            }`}
          >
            {t('account.security_tab')}
          </button>
        </div>

        {/* Content */}
        <div className="p-6 overflow-y-auto max-h-[calc(90vh-200px)] space-y-6">
          {activeTab === 'profile' && (
            <div className="space-y-6">
              {/* User Info */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">
                      {t('account.firstName')}
                    </label>
                    <div className="flex items-center gap-2 p-3 surface-muted rounded-xl border border-gray-200 dark:border-slate-700">
                      <User className="h-4 w-4 text-gray-400 dark:text-gray-500" />
                      <span className="text-gray-900 dark:text-gray-100">{user.firstName}</span>
                    </div>
                  </div>
                  
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">
                      {t('account.lastName')}
                    </label>
                    <div className="flex items-center gap-2 p-3 surface-muted rounded-xl border border-gray-200 dark:border-slate-700">
                      <User className="h-4 w-4 text-gray-400 dark:text-gray-500" />
                      <span className="text-gray-900 dark:text-gray-100">{user.lastName}</span>
                    </div>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">
                      {t('account.username')}
                    </label>
                    <div className="flex items-center gap-2 p-3 surface-muted rounded-xl border border-gray-200 dark:border-slate-700">
                      <User className="h-4 w-4 text-gray-400 dark:text-gray-500" />
                      <span className="text-gray-900 dark:text-gray-100">{user.userName}</span>
                    </div>
                  </div>
                </div>

                <div className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">
                      {t('account.email')}
                    </label>
                    <div className="flex items-center gap-2 p-3 surface-muted rounded-xl border border-gray-200 dark:border-slate-700">
                      <Mail className="h-4 w-4 text-gray-400 dark:text-gray-500" />
                      <span className="text-gray-900 dark:text-gray-100 break-all">{user.email}</span>
                    </div>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">
                      {t('account.roles')}
                    </label>
                    <div className="flex items-center gap-2 p-3 surface-muted rounded-xl border border-gray-200 dark:border-slate-700">
                      <Shield className="h-4 w-4 text-gray-400 dark:text-gray-500" />
                      <div className="flex flex-wrap gap-1 text-xs">
                        {user.roles.map((role) => (
                          <span
                            key={role}
                            className="px-2 py-1 font-medium bg-primary-50 text-primary-700 rounded dark:bg-primary-900/30 dark:text-primary-300"
                          >
                            {role}
                          </span>
                        ))}
                      </div>
                    </div>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">
                      {t('account.created_at')}
                    </label>
                    <div className="flex items-center gap-2 p-3 surface-muted rounded-xl border border-gray-200 dark:border-slate-700">
                      <Calendar className="h-4 w-4 text-gray-400 dark:text-gray-500" />
                      <span className="text-gray-900 dark:text-gray-100">{formatDate(user.createdAt)}</span>
                    </div>
                  </div>

                  {user.lastLoginAt && (
                    <div>
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">
                        {t('account.last_login')}
                      </label>
                      <div className="flex items-center gap-2 p-3 surface-muted rounded-xl border border-gray-200 dark:border-slate-700">
                        <Calendar className="h-4 w-4 text-gray-400 dark:text-gray-500" />
                        <span className="text-gray-900 dark:text-gray-100">{formatDate(user.lastLoginAt)}</span>
                      </div>
                    </div>
                  )}
                </div>
              </div>

              {/* Actions */}
              <div className="pt-6 border-t border-gray-200 dark:border-slate-700">
                <button
                  onClick={handleUpdateProfile}
                  className="btn-primary inline-flex items-center gap-2"
                >
                  <Edit className="h-4 w-4" />
                  {t('account.update_profile')}
                </button>
              </div>
            </div>
          )}

          {activeTab === 'security' && (
            <div className="space-y-6">
              {/* Logout from all devices */}
              <div className="p-4 surface-muted border border-blue-200 dark:border-blue-900/40 rounded-xl">
                <h3 className="text-sm font-medium text-blue-800 dark:text-blue-300 mb-2">
                  {t('account.logout_all_devices')}
                </h3>
                <p className="text-sm text-blue-700 dark:text-blue-200 mb-4">
                  {t('account.logout_all_devices_description')}
                </p>
                <button
                  onClick={handleLogoutAllDevices}
                  className="btn-primary inline-flex items-center gap-2"
                >
                  <LogOut className="h-4 w-4" />
                  {t('account.logout_all_devices')}
                </button>
              </div>

              <div className="p-4 surface-muted border border-yellow-200 dark:border-yellow-900/40 rounded-xl">
                <h3 className="text-sm font-medium text-yellow-800 dark:text-yellow-300 mb-2">
                  {t('account.danger_zone')}
                </h3>
                <p className="text-sm text-yellow-700 dark:text-yellow-200 mb-4">
                  {t('account.delete_warning')}
                </p>
                <button
                  onClick={handleDeleteAccount}
                  className="btn-primary inline-flex items-center gap-2 bg-red-600 hover:bg-red-700 focus-visible:ring-red-500 dark:bg-red-500 dark:hover:bg-red-600"
                >
                  <Trash2 className="h-4 w-4" />
                  {t('account.delete_account')}
                </button>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Logout Confirmation Modal */}
      <ConfirmModal
        isOpen={showLogoutConfirm}
        onClose={handleCancelLogout}
        onConfirm={handleConfirmLogout}
        title={t('account.logout_all_devices')}
        message={t('account.logout_all_devices_confirm')}
        confirmText={t('account.logout_all_devices')}
        cancelText={t('common.cancel')}
        variant="warning"
        isLoading={isLoggingOut}
      />
    </div>
  )
}
