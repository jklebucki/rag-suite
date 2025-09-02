import React, { useState } from 'react'
import { X, User, Mail, Calendar, Shield, Edit, Trash2 } from 'lucide-react'
import { useI18n } from '@/contexts/I18nContext'
import { useAuth } from '@/contexts/AuthContext'
import type { User as UserType } from '@/types/auth'

interface UserAccountModalProps {
  isOpen: boolean
  onClose: () => void
}

export function UserAccountModal({ isOpen, onClose }: UserAccountModalProps) {
  const { t } = useI18n()
  const { user } = useAuth()
  const [activeTab, setActiveTab] = useState<'profile' | 'security'>('profile')

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
    console.log('Update profile clicked')
  }

  const handleDeleteAccount = () => {
    // TODO: Implement account deletion functionality
    console.log('Delete account clicked')
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-2xl max-h-[90vh] overflow-hidden">
        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b border-gray-200">
          <h2 className="text-xl font-semibold text-gray-900">
            {t('account.title')}
          </h2>
          <button
            onClick={onClose}
            className="p-2 rounded-lg hover:bg-gray-100"
            aria-label={t('common.close')}
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        {/* Tabs */}
        <div className="flex border-b border-gray-200">
          <button
            onClick={() => setActiveTab('profile')}
            className={`px-6 py-3 text-sm font-medium border-b-2 transition-colors ${
              activeTab === 'profile'
                ? 'border-blue-500 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700'
            }`}
          >
            {t('account.profile_tab')}
          </button>
          <button
            onClick={() => setActiveTab('security')}
            className={`px-6 py-3 text-sm font-medium border-b-2 transition-colors ${
              activeTab === 'security'
                ? 'border-blue-500 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700'
            }`}
          >
            {t('account.security_tab')}
          </button>
        </div>

        {/* Content */}
        <div className="p-6 overflow-y-auto max-h-[calc(90vh-200px)]">
          {activeTab === 'profile' && (
            <div className="space-y-6">
              {/* User Info */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      {t('account.firstName')}
                    </label>
                    <div className="flex items-center gap-2 p-3 bg-gray-50 rounded-lg">
                      <User className="h-4 w-4 text-gray-400" />
                      <span className="text-gray-900">{user.firstName}</span>
                    </div>
                  </div>
                  
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      {t('account.lastName')}
                    </label>
                    <div className="flex items-center gap-2 p-3 bg-gray-50 rounded-lg">
                      <User className="h-4 w-4 text-gray-400" />
                      <span className="text-gray-900">{user.lastName}</span>
                    </div>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      {t('account.username')}
                    </label>
                    <div className="flex items-center gap-2 p-3 bg-gray-50 rounded-lg">
                      <User className="h-4 w-4 text-gray-400" />
                      <span className="text-gray-900">{user.userName}</span>
                    </div>
                  </div>
                </div>

                <div className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      {t('account.email')}
                    </label>
                    <div className="flex items-center gap-2 p-3 bg-gray-50 rounded-lg">
                      <Mail className="h-4 w-4 text-gray-400" />
                      <span className="text-gray-900 break-all">{user.email}</span>
                    </div>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      {t('account.roles')}
                    </label>
                    <div className="flex items-center gap-2 p-3 bg-gray-50 rounded-lg">
                      <Shield className="h-4 w-4 text-gray-400" />
                      <div className="flex flex-wrap gap-1">
                        {user.roles.map((role) => (
                          <span
                            key={role}
                            className="px-2 py-1 text-xs font-medium bg-blue-100 text-blue-800 rounded"
                          >
                            {role}
                          </span>
                        ))}
                      </div>
                    </div>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      {t('account.created_at')}
                    </label>
                    <div className="flex items-center gap-2 p-3 bg-gray-50 rounded-lg">
                      <Calendar className="h-4 w-4 text-gray-400" />
                      <span className="text-gray-900">{formatDate(user.createdAt)}</span>
                    </div>
                  </div>

                  {user.lastLoginAt && (
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        {t('account.last_login')}
                      </label>
                      <div className="flex items-center gap-2 p-3 bg-gray-50 rounded-lg">
                        <Calendar className="h-4 w-4 text-gray-400" />
                        <span className="text-gray-900">{formatDate(user.lastLoginAt)}</span>
                      </div>
                    </div>
                  )}
                </div>
              </div>

              {/* Actions */}
              <div className="pt-6 border-t border-gray-200">
                <div className="flex flex-col sm:flex-row gap-3">
                  <button
                    onClick={handleUpdateProfile}
                    className="flex items-center justify-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
                  >
                    <Edit className="h-4 w-4" />
                    {t('account.update_profile')}
                  </button>
                </div>
              </div>
            </div>
          )}

          {activeTab === 'security' && (
            <div className="space-y-6">
              <div className="p-4 bg-yellow-50 border border-yellow-200 rounded-lg">
                <h3 className="text-sm font-medium text-yellow-800 mb-2">
                  {t('account.danger_zone')}
                </h3>
                <p className="text-sm text-yellow-700 mb-4">
                  {t('account.delete_warning')}
                </p>
                <button
                  onClick={handleDeleteAccount}
                  className="flex items-center gap-2 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors"
                >
                  <Trash2 className="h-4 w-4" />
                  {t('account.delete_account')}
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
