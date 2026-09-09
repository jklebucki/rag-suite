// ContactExport - CSV file download component for exporting contacts
import React, { useState } from 'react'
import type { ContactListItem } from '@/features/address-book/types/addressbook'
import {
  ADDRESS_BOOK_CSV_ENCODINGS,
  downloadAddressBookCsv,
  type AddressBookCsvEncoding,
} from '@/features/address-book/utils/csvExport'
import { useI18n } from '@/shared/contexts/I18nContext'

interface ContactExportProps {
  contacts: ContactListItem[]
}

export const ContactExport: React.FC<ContactExportProps> = ({ contacts }) => {
  const { t } = useI18n()
  const [encoding, setEncoding] = useState<AddressBookCsvEncoding>('UTF-8')

  const handleExport = () => {
    downloadAddressBookCsv(contacts, encoding)
  }

  return (
    <div className="space-y-5">
      <div className="surface-muted border border-blue-200 dark:border-blue-900/40 rounded-xl p-4">
        <h3 className="font-medium text-blue-900 dark:text-blue-200 mb-2">{t('addressBook.export.format')}</h3>
        <p className="text-sm text-blue-800 dark:text-blue-200/80 mb-2">
          {t('addressBook.export.formatDesc')}
        </p>
        <code className="text-xs bg-blue-100 dark:bg-blue-900/40 text-blue-900 dark:text-blue-100 px-3 py-2 rounded-lg block overflow-x-auto">
          {t('addressBook.import.csvExample')}
        </code>
        <p className="text-xs text-blue-700 dark:text-blue-200/70 mt-2">
          {t('addressBook.export.compatibilityNote')}
        </p>
      </div>

      <div className="surface border border-gray-200 dark:border-slate-700 rounded-xl p-4">
        <p className="text-sm text-gray-700 dark:text-gray-200">
          {t('addressBook.export.contactsCount', { count: String(contacts.length) })}
        </p>
        <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
          {t('addressBook.export.scopeNote')}
        </p>
      </div>

      <div className="flex items-center gap-3">
        <label htmlFor="export-encoding" className="text-sm text-gray-700 dark:text-gray-200 font-medium">
          {t('addressBook.import.encodingLabel')}
        </label>
        <select
          id="export-encoding"
          value={encoding}
          onChange={(e) => setEncoding(e.target.value as AddressBookCsvEncoding)}
          className="form-select w-48 py-2 text-sm"
        >
          {ADDRESS_BOOK_CSV_ENCODINGS.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      </div>

      <div className="flex justify-end gap-3 pt-4 border-t border-gray-200 dark:border-slate-700">
        <button
          type="button"
          onClick={handleExport}
          disabled={contacts.length === 0}
          className="btn-primary disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {t('addressBook.export.exportContacts')}
        </button>
      </div>
    </div>
  )
}
