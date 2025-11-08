// ContactImport - CSV file upload component for importing contacts
import React, { useState, useRef } from 'react'
import type { ImportContactsResponse } from '@/features/address-book/types/addressbook'

interface ContactImportProps {
  onImport: (file: File, skipDuplicates: boolean, encoding: string) => Promise<ImportContactsResponse>
  onClose: () => void
}

export const ContactImport: React.FC<ContactImportProps> = ({ onImport, onClose }) => {
  const [file, setFile] = useState<File | null>(null)
  const [skipDuplicates, setSkipDuplicates] = useState(true)
  const [encoding, setEncoding] = useState<string>('UTF-8')
  const [isUploading, setIsUploading] = useState(false)
  const [result, setResult] = useState<ImportContactsResponse | null>(null)
  const [error, setError] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0]
    if (selectedFile) {
      if (!selectedFile.name.endsWith('.csv')) {
        setError('Please select a CSV file')
        setFile(null)
        return
      }
      setFile(selectedFile)
      setError(null)
      setResult(null)
    }
  }

  const handleUpload = async () => {
    if (!file) {
      setError('Please select a file first')
      return
    }

    setIsUploading(true)
    setError(null)

    try {
      const importResult = await onImport(file, skipDuplicates, encoding)
      setResult(importResult)
      setFile(null)
      if (fileInputRef.current) {
        fileInputRef.current.value = ''
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Import failed')
    } finally {
      setIsUploading(false)
    }
  }

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()

    const droppedFile = e.dataTransfer.files?.[0]
    if (droppedFile) {
      if (!droppedFile.name.endsWith('.csv')) {
        setError('Please drop a CSV file')
        return
      }
      setFile(droppedFile)
      setError(null)
      setResult(null)
    }
  }

  return (
    <div className="space-y-4">
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <h3 className="font-medium text-blue-900 mb-2">CSV Import Format</h3>
        <p className="text-sm text-blue-800 mb-2">
          Expected columns (semicolon-separated):
        </p>
        <code className="text-xs bg-blue-100 px-2 py-1 rounded block overflow-x-auto">
          Imię;Nazwisko;Stanowisko;Telefon służbowy;Telefon komórkowy;Email;Lokalizacja;Wyświetlana nazwa;Notatki
        </code>
        <p className="text-xs text-blue-700 mt-2">
          Polish column names are automatically mapped to English. Encoding: UTF-8.
        </p>
      </div>

      {/* File Upload Area */}
      <div
        onDragOver={handleDragOver}
        onDrop={handleDrop}
        className={`border-2 border-dashed rounded-lg p-8 text-center transition-colors ${
          file
            ? 'border-green-400 bg-green-50'
            : 'border-gray-300 bg-gray-50 hover:border-blue-400 hover:bg-blue-50'
        }`}
      >
        <input
          ref={fileInputRef}
          type="file"
          accept=".csv"
          onChange={handleFileChange}
          className="hidden"
          id="csv-file-input"
        />
        <label
          htmlFor="csv-file-input"
          className="cursor-pointer flex flex-col items-center"
        >
          <svg
            className="w-12 h-12 text-gray-400 mb-3"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"
            />
          </svg>
          <span className="text-sm text-gray-600">
            {file ? (
              <span className="text-green-600 font-medium">{file.name}</span>
            ) : (
              <>
                <span className="text-blue-600 font-medium">Click to upload</span> or drag and drop
              </>
            )}
          </span>
          <span className="text-xs text-gray-500 mt-1">CSV files only</span>
        </label>
      </div>

      {/* Options */}
      <div className="space-y-3">
        {/* Skip duplicates checkbox */}
        <div className="flex items-center gap-2">
          <input
            type="checkbox"
            id="skip-duplicates"
            checked={skipDuplicates}
            onChange={(e) => setSkipDuplicates(e.target.checked)}
            className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
          />
          <label htmlFor="skip-duplicates" className="text-sm text-gray-700">
            Skip contacts with duplicate emails
          </label>
        </div>

        {/* Encoding selector */}
        <div className="flex items-center gap-3">
          <label htmlFor="encoding" className="text-sm text-gray-700 font-medium">
            File encoding:
          </label>
          <select
            id="encoding"
            value={encoding}
            onChange={(e) => setEncoding(e.target.value)}
            className="px-3 py-1.5 text-sm border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          >
            <option value="UTF-8">UTF-8</option>
            <option value="windows-1250">Windows-1250 (CP1250)</option>
            <option value="ISO-8859-2">ISO-8859-2 (Latin-2)</option>
            <option value="windows-1252">Windows-1252 (CP1252)</option>
          </select>
        </div>
      </div>

      {/* Error Message */}
      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
          {error}
        </div>
      )}

      {/* Import Result */}
      {result && (
        <div className="bg-green-50 border border-green-200 rounded-lg p-4">
          <h3 className="font-medium text-green-900 mb-2">Import Complete</h3>
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span className="text-gray-600">Total rows:</span>{' '}
              <span className="font-medium">{result.totalRows}</span>
            </div>
            <div>
              <span className="text-gray-600">Imported:</span>{' '}
              <span className="font-medium text-green-700">{result.successCount}</span>
            </div>
            <div>
              <span className="text-gray-600">Skipped:</span>{' '}
              <span className="font-medium text-yellow-700">{result.skippedCount}</span>
            </div>
            <div>
              <span className="text-gray-600">Errors:</span>{' '}
              <span className="font-medium text-red-700">{result.errorCount}</span>
            </div>
          </div>

          {result.errors.length > 0 && (
            <div className="mt-3">
              <p className="text-sm font-medium text-red-800 mb-1">Errors:</p>
              <ul className="text-xs text-red-700 space-y-1 max-h-32 overflow-y-auto">
                {result.errors.map((err, idx) => (
                  <li key={idx}>• {err}</li>
                ))}
              </ul>
            </div>
          )}

          {result.importedContacts.length > 0 && (
            <div className="mt-3">
              <p className="text-sm font-medium text-green-800 mb-1">
                Sample imported contacts (first 5):
              </p>
              <ul className="text-xs text-green-700 space-y-1">
                {result.importedContacts.slice(0, 5).map((contact) => (
                  <li key={contact.id}>
                    • {contact.firstName} {contact.lastName}
                    {contact.email && ` (${contact.email})`}
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>
      )}

      {/* Actions */}
      <div className="flex justify-end gap-3 pt-4 border-t border-gray-200">
        <button
          type="button"
          onClick={onClose}
          disabled={isUploading}
          className="px-4 py-2 text-gray-700 bg-gray-100 rounded-lg hover:bg-gray-200 disabled:opacity-50"
        >
          {result ? 'Close' : 'Cancel'}
        </button>
        {!result && (
          <button
            type="button"
            onClick={handleUpload}
            disabled={!file || isUploading}
            className="px-4 py-2 text-white bg-blue-600 rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isUploading ? (
              <span className="flex items-center gap-2">
                <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24">
                  <circle
                    className="opacity-25"
                    cx="12"
                    cy="12"
                    r="10"
                    stroke="currentColor"
                    strokeWidth="4"
                    fill="none"
                  />
                  <path
                    className="opacity-75"
                    fill="currentColor"
                    d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                  />
                </svg>
                Importing...
              </span>
            ) : (
              'Import Contacts'
            )}
          </button>
        )}
      </div>
    </div>
  )
}
