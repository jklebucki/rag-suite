// ContactForm - Modal form for creating/editing contacts
import React, { useState, useEffect, useActionState, useRef } from 'react'
import type { ContactListItem, Contact, ContactData, CreateContactRequest, UpdateContactRequest } from '@/features/address-book/types/addressbook'
import { SubmitButton } from '@/shared/components/ui/SubmitButton'
import { useI18n } from '@/shared/contexts/I18nContext'
import { PhotoIcon, XMarkIcon, PlusIcon } from '@heroicons/react/24/outline'

interface FormState {
  success: boolean
  error: string | null
}

interface ContactFormProps {
  contact?: ContactListItem | Contact | null // null/undefined = create mode
  isOpen: boolean
  onClose: () => void
  onSubmit: (data: CreateContactRequest | UpdateContactRequest) => Promise<void>
  canModify: boolean // Admin/PowerUser can modify directly
  title?: string
}

export const ContactForm: React.FC<ContactFormProps> = ({
  contact,
  isOpen,
  onClose,
  onSubmit,
  canModify,
  title
}) => {
  const { t } = useI18n()
  const isEditMode = !!contact
  const [formData, setFormData] = useState<ContactData>({
    firstName: '',
    lastName: '',
    displayName: null,
    department: null,
    position: null,
    location: null,
    company: null,
    workPhone: null,
    mobilePhone: null,
    email: null,
    notes: null,
    photoUrl: null
  })
  const [isActive, setIsActive] = useState(true)
  const [tags, setTags] = useState<string[]>([])
  const [tagInput, setTagInput] = useState('')
  const [photoPreview, setPhotoPreview] = useState<string | null>(null)
  const [photoError, setPhotoError] = useState<string | null>(null)
  const [isDragging, setIsDragging] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const MAX_PHOTO_SIZE = 1024 * 1024 // 1 MB

  // Load contact data when editing
  useEffect(() => {
    if (contact) {
      setFormData({
        firstName: contact.firstName,
        lastName: contact.lastName,
        displayName: contact.displayName,
        department: contact.department,
        position: contact.position,
        location: contact.location,
        company: 'company' in contact ? contact.company : null, // Contact has company, ContactListItem doesn't
        workPhone: 'workPhone' in contact ? contact.workPhone : null, // Contact has workPhone, ContactListItem doesn't
        mobilePhone: contact.mobilePhone,
        email: contact.email,
        notes: 'notes' in contact ? contact.notes : null, // Contact has notes, ContactListItem doesn't
        photoUrl: 'photoUrl' in contact ? contact.photoUrl : null // Contact has photoUrl, ContactListItem doesn't
      })
      setIsActive(contact.isActive)
      // Set photo preview if photoUrl exists and is base64 data URI
      const photoUrl = 'photoUrl' in contact ? contact.photoUrl : null
      if (photoUrl && (photoUrl.startsWith('data:') || photoUrl.length > 100)) {
        setPhotoPreview(photoUrl.startsWith('data:') ? photoUrl : `data:image/png;base64,${photoUrl}`)
      } else {
        setPhotoPreview(null)
      }
      
      // Map ContactTagDto[] → string[] if contact has tags (Contact type)
      // ContactListItem doesn't have tags, so this is for future compatibility
      if ('tags' in contact && contact.tags) {
        const tagNames = contact.tags.map(tag => typeof tag === 'string' ? tag : tag.tagName)
        setTags(tagNames)
      } else {
        setTags([])
      }
    } else {
      // Reset form for create mode
      setFormData({
        firstName: '',
        lastName: '',
        displayName: null,
        department: null,
        position: null,
        location: null,
        company: null,
        workPhone: null,
        mobilePhone: null,
        email: null,
      notes: null,
      photoUrl: null
    })
    setIsActive(true)
    setTags([])
    setPhotoPreview(null)
    setPhotoError(null)
    }
  }, [contact])

  const [state, formAction] = useActionState(
    async (prevState: FormState | null, formData: FormData): Promise<FormState> => {
      try {
        // Extract form data
        const firstName = formData.get('firstName') as string
        const lastName = formData.get('lastName') as string
        const displayName = formData.get('displayName') as string || null
        const department = formData.get('department') as string || null
        const position = formData.get('position') as string || null
        const location = formData.get('location') as string || null
        const company = formData.get('company') as string || null
        const workPhone = formData.get('workPhone') as string || null
        const mobilePhone = formData.get('mobilePhone') as string || null
        const email = formData.get('email') as string || null
        const notes = formData.get('notes') as string || null
        const photoUrl = formData.get('photoUrl') as string || null
        const isActiveValue = formData.get('isActive') === 'on'
        const tagsValue = formData.get('tags') as string
        const parsedTags = tagsValue ? JSON.parse(tagsValue) : []

        if (isEditMode) {
          const updateData: UpdateContactRequest = {
            firstName,
            lastName,
            displayName,
            department,
            position,
            location,
            company,
            workPhone,
            mobilePhone,
            email,
            notes,
            photoUrl,
            isActive: isActiveValue,
            tags: parsedTags.length > 0 ? parsedTags : undefined
          }
          await onSubmit(updateData)
        } else {
          const createData: CreateContactRequest = {
            firstName,
            lastName,
            displayName,
            department,
            position,
            location,
            company,
            workPhone,
            mobilePhone,
            email,
            notes,
            photoUrl,
            tags: parsedTags.length > 0 ? parsedTags : undefined
          }
          await onSubmit(createData)
        }
        onClose()
        return { success: true, error: null }
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'An error occurred'
        return { success: false, error: errorMessage }
      }
    },
    null
  )

  const handleInputChange = (field: keyof ContactData, value: string) => {
    setFormData((prev) => ({
      ...prev,
      [field]: value || null
    }))
  }

  const handleAddTag = () => {
    if (tagInput.trim() && !tags.includes(tagInput.trim())) {
      setTags([...tags, tagInput.trim()])
      setTagInput('')
    }
  }

  const handleRemoveTag = (tag: string) => {
    setTags(tags.filter((t) => t !== tag))
  }

  const readFileAsBase64 = (file: File): Promise<string> => {
    return new Promise((resolve, reject) => {
      const reader = new FileReader()
      reader.onload = () => {
        const result = reader.result as string
        // Extract base64 part (remove data:image/...;base64, prefix)
        const base64Index = result.indexOf(',')
        const base64 = base64Index >= 0 ? result.slice(base64Index + 1) : result
        resolve(base64)
      }
      reader.onerror = () => reject(reader.error)
      reader.readAsDataURL(file)
    })
  }

  const handleFileSelect = async (file: File) => {
    setPhotoError(null)

    // Validate file size (1 MB max)
    if (file.size > MAX_PHOTO_SIZE) {
      setPhotoError(t('addressBook.form.photoTooLarge', { size: '1 MB' }))
      return
    }

    // Validate file type (images only)
    if (!file.type.startsWith('image/')) {
      setPhotoError(t('addressBook.form.photoInvalidFormat'))
      return
    }

    try {
      const base64 = await readFileAsBase64(file)
      const dataUrl = `data:${file.type};base64,${base64}`
      
      setPhotoPreview(dataUrl)
      setFormData((prev) => ({
        ...prev,
        photoUrl: base64 // Store base64 without data URI prefix
      }))
    } catch (err) {
      setPhotoError(t('addressBook.form.photoError'))
      console.error('Error reading file:', err)
    }
  }

  const handleFileInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (file) {
      handleFileSelect(file)
    }
    // Reset input so same file can be selected again
    if (fileInputRef.current) {
      fileInputRef.current.value = ''
    }
  }

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    setIsDragging(true)
  }

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    setIsDragging(false)
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    setIsDragging(false)

    const file = e.dataTransfer.files?.[0]
    if (file) {
      handleFileSelect(file)
    }
  }

  const handleRemovePhoto = () => {
    setPhotoPreview(null)
    setPhotoError(null)
    setFormData((prev) => ({
      ...prev,
      photoUrl: null
    }))
    if (fileInputRef.current) {
      fileInputRef.current.value = ''
    }
  }

  if (!isOpen) return null

  return (
    <div className="fixed inset-0 bg-black/60 dark:bg-black/70 flex items-center justify-center z-50 p-4">
      <div className="surface rounded-2xl shadow-xl max-w-5xl w-full max-h-[90vh] overflow-hidden flex flex-col">
        <div className="sticky top-0 surface-muted border-b border-gray-200 dark:border-slate-700 px-6 py-4 flex-shrink-0">
          <div className="flex items-center justify-between">
            <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">
              {title || (isEditMode ? t('addressBook.form.title.edit') : t('addressBook.form.title.create'))}
            </h2>
            <button
              onClick={onClose}
              className="text-gray-400 hover:text-gray-700 dark:text-gray-500 dark:hover:text-gray-300"
              type="button"
            >
              <span className="text-2xl">&times;</span>
            </button>
          </div>
          {!canModify && (
            <p className="text-sm text-purple-600 dark:text-purple-300 mt-2">
              {t('addressBook.form.proposalNotice')}
            </p>
          )}
        </div>

        <form
          action={formAction}
          className="flex-1 overflow-y-auto"
        >
          {/* Hidden input for tags array */}
          <input type="hidden" name="tags" value={JSON.stringify(tags)} />
          
          {state?.error && (
            <div className="mx-6 mt-4 rounded-xl border border-red-200 dark:border-red-700 bg-red-50 dark:bg-red-900/20 text-red-700 dark:text-red-300 px-4 py-3">
              {state.error}
            </div>
          )}

          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 p-6">
            {/* Left column - Form fields */}
            <div className="lg:col-span-2 space-y-6">
              {/* Required Fields */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label htmlFor="firstName" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1.5">
                    {t('addressBook.form.firstName')} <span className="text-red-500">*</span>
                  </label>
                  <input
                    id="firstName"
                    name="firstName"
                    type="text"
                    required
                    value={formData.firstName}
                    onChange={(e) => handleInputChange('firstName', e.target.value)}
                    placeholder={t('addressBook.form.firstNamePlaceholder')}
                    className="form-input"
                  />
                </div>
                <div>
                  <label htmlFor="lastName" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1.5">
                    {t('addressBook.form.lastName')} <span className="text-red-500">*</span>
                  </label>
                  <input
                    id="lastName"
                    name="lastName"
                    type="text"
                    required
                    value={formData.lastName}
                    onChange={(e) => handleInputChange('lastName', e.target.value)}
                    placeholder={t('addressBook.form.lastNamePlaceholder')}
                    className="form-input"
                  />
                </div>
              </div>

              {/* Display Name - Full width */}
              <div>
                <label htmlFor="displayName" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1.5">{t('addressBook.form.displayName')}</label>
                <input
                  id="displayName"
                  name="displayName"
                  type="text"
                  value={formData.displayName || ''}
                  onChange={(e) => handleInputChange('displayName', e.target.value)}
                  placeholder={t('addressBook.form.displayNamePlaceholder')}
                  className="form-input"
                />
              </div>

              {/* Contact Information - 2 columns */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label htmlFor="email" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1.5">{t('addressBook.form.email')}</label>
                  <input
                    id="email"
                    name="email"
                    type="email"
                    value={formData.email || ''}
                    onChange={(e) => handleInputChange('email', e.target.value)}
                    placeholder={t('addressBook.form.emailPlaceholder')}
                    className="form-input"
                  />
                </div>
                <div>
                  <label htmlFor="mobilePhone" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1.5">{t('addressBook.form.mobilePhone')}</label>
                  <input
                    id="mobilePhone"
                    name="mobilePhone"
                    type="tel"
                    value={formData.mobilePhone || ''}
                    onChange={(e) => handleInputChange('mobilePhone', e.target.value)}
                    placeholder={t('addressBook.form.mobilePhonePlaceholder')}
                    className="form-input"
                  />
                </div>
              </div>

              {/* Work Phone & Company - 2 columns */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label htmlFor="workPhone" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1.5">{t('addressBook.form.workPhone')}</label>
                  <input
                    id="workPhone"
                    name="workPhone"
                    type="tel"
                    value={formData.workPhone || ''}
                    onChange={(e) => handleInputChange('workPhone', e.target.value)}
                    placeholder={t('addressBook.form.workPhonePlaceholder')}
                    className="form-input"
                  />
                </div>
                <div>
                  <label htmlFor="company" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1.5">{t('addressBook.form.company')}</label>
                  <input
                    id="company"
                    name="company"
                    type="text"
                    value={formData.company || ''}
                    onChange={(e) => handleInputChange('company', e.target.value)}
                    placeholder={t('addressBook.form.companyPlaceholder')}
                    className="form-input"
                  />
                </div>
              </div>

              {/* Department, Position, Location - 3 columns */}
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div>
                  <label htmlFor="department" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1.5">{t('addressBook.form.department')}</label>
                  <input
                    id="department"
                    name="department"
                    type="text"
                    value={formData.department || ''}
                    onChange={(e) => handleInputChange('department', e.target.value)}
                    placeholder={t('addressBook.form.departmentPlaceholder')}
                    className="form-input"
                  />
                </div>
                <div>
                  <label htmlFor="position" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1.5">{t('addressBook.form.position')}</label>
                  <input
                    id="position"
                    name="position"
                    type="text"
                    value={formData.position || ''}
                    onChange={(e) => handleInputChange('position', e.target.value)}
                    placeholder={t('addressBook.form.positionPlaceholder')}
                    className="form-input"
                  />
                </div>
                <div>
                  <label htmlFor="location" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1.5">{t('addressBook.form.location')}</label>
                  <input
                    id="location"
                    name="location"
                    type="text"
                    value={formData.location || ''}
                    onChange={(e) => handleInputChange('location', e.target.value)}
                    placeholder={t('addressBook.form.locationPlaceholder')}
                    className="form-input"
                  />
                </div>
              </div>

              {/* Notes - Full width, aligned with right column bottom */}
              <div>
                <label htmlFor="notes" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1.5">{t('addressBook.form.notes')}</label>
                <textarea
                  id="notes"
                  name="notes"
                  value={formData.notes || ''}
                  onChange={(e) => handleInputChange('notes', e.target.value)}
                  rows={4}
                  placeholder={t('addressBook.form.notesPlaceholder')}
                  className="form-input min-h-[140px]"
                />
              </div>
            </div>

            {/* Right column - Photo Upload, Tags, Status */}
            <div className="lg:col-span-1 flex flex-col">
              <div className="flex-shrink-0">
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1.5">
                  {t('addressBook.form.photo')}
                </label>
                <input
                  ref={fileInputRef}
                  type="file"
                  accept="image/*"
                  onChange={handleFileInputChange}
                  className="hidden"
                  id="photo-upload"
                />
                <input type="hidden" name="photoUrl" value={formData.photoUrl || ''} />
                
                {photoPreview ? (
                  <div className="space-y-2">
                    <div 
                      className="relative w-full cursor-pointer group" 
                      style={{ aspectRatio: '3/4' }}
                      onClick={() => fileInputRef.current?.click()}
                    >
                      <img
                        src={photoPreview}
                        alt={t('addressBook.form.photoPreview')}
                        className="w-full h-full object-contain rounded-lg border-2 border-gray-300 dark:border-slate-600 bg-gray-50 dark:bg-slate-800 group-hover:opacity-90 transition-opacity"
                      />
                      <button
                        type="button"
                        onClick={(e) => {
                          e.stopPropagation()
                          handleRemovePhoto()
                        }}
                        className="absolute top-2 right-2 p-1.5 bg-red-500 text-white rounded-full hover:bg-red-600 transition-colors shadow-lg"
                        aria-label={t('addressBook.form.removePhoto')}
                      >
                        <XMarkIcon className="w-4 h-4" />
                      </button>
                    </div>
                    <p className="text-xs text-gray-500 dark:text-gray-400 text-center">
                      {t('addressBook.form.photoMaxSize', { size: '1 MB' })}
                    </p>
                  </div>
                ) : (
                  <div
                    onDragOver={handleDragOver}
                    onDragLeave={handleDragLeave}
                    onDrop={handleDrop}
                    onClick={() => fileInputRef.current?.click()}
                    className={`border-2 border-dashed rounded-lg p-6 text-center cursor-pointer transition-colors ${
                      isDragging
                        ? 'border-primary-500 bg-primary-50 dark:border-primary-400 dark:bg-primary-900/20'
                        : 'border-gray-300 bg-gray-50 hover:border-primary-400 hover:bg-primary-50 dark:border-slate-700 dark:bg-slate-800 dark:hover:border-primary-400 dark:hover:bg-slate-800/70'
                    }`}
                    style={{ aspectRatio: '3/4' }}
                  >
                    <div className="flex flex-col items-center justify-center h-full">
                      <PhotoIcon className="h-8 w-8 text-gray-400 dark:text-gray-500 mb-2" />
                      <p className="text-xs text-gray-600 dark:text-gray-300 mb-1">
                        {t('addressBook.form.photoDragDrop')}
                      </p>
                      <p className="text-xs text-gray-500 dark:text-gray-400">
                        {t('addressBook.form.photoMaxSize', { size: '1 MB' })}
                      </p>
                    </div>
                  </div>
                )}
                
                {photoError && (
                  <p className="mt-2 text-sm text-red-600 dark:text-red-400 text-center">{photoError}</p>
                )}
              </div>

              {/* Tags - positioned to align with notes bottom */}
              <div className="mt-auto pt-5">
                <div>
                  <label htmlFor="tagInput" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1.5">{t('addressBook.form.tags')}</label>
                  <div className="flex gap-0 mb-2">
                    <input
                      id="tagInput"
                      type="text"
                      value={tagInput}
                      onChange={(e) => setTagInput(e.target.value)}
                      onKeyPress={(e) => e.key === 'Enter' && (e.preventDefault(), handleAddTag())}
                      placeholder={t('addressBook.form.tagInputPlaceholder')}
                      className="form-input rounded-r-none flex-1"
                    />
                    <button
                      type="button"
                      onClick={handleAddTag}
                      className="px-3 py-2 bg-gray-200 dark:bg-slate-700 text-gray-700 dark:text-gray-200 rounded-r-lg border border-l-0 border-gray-300 dark:border-slate-600 hover:bg-gray-300 dark:hover:bg-slate-600 transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-1"
                      aria-label={t('addressBook.form.addTag')}
                    >
                      <PlusIcon className="w-5 h-5" />
                    </button>
                  </div>
                  {tags.length > 0 && (
                    <div className="flex flex-wrap gap-2">
                      {tags.map((tag) => (
                        <span
                          key={tag}
                          className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-200"
                        >
                          {tag}
                          <button
                            type="button"
                            onClick={() => handleRemoveTag(tag)}
                            className="text-primary-600 dark:text-primary-300 hover:text-primary-700 dark:hover:text-primary-200"
                          >
                            &times;
                          </button>
                        </span>
                      ))}
                    </div>
                  )}
                </div>

                {/* Status (Edit mode only) */}
                {isEditMode && (
                  <div className="mt-4">
                    <label className="flex items-center gap-3 cursor-pointer">
                      <input
                        type="checkbox"
                        name="isActive"
                        checked={isActive}
                        onChange={(e) => setIsActive(e.target.checked)}
                        className="form-checkbox"
                      />
                      <span className="text-sm font-medium text-gray-700 dark:text-gray-200">{t('addressBook.form.isActive')}</span>
                    </label>
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* Form Actions */}
          <div className="sticky bottom-0 surface-muted border-t border-gray-200 dark:border-slate-700 px-6 py-4 flex justify-end gap-3">
            <button
              type="button"
              onClick={onClose}
              className="btn-secondary"
            >
              {t('common.cancel')}
            </button>
            <SubmitButton
              className="btn-primary"
              loadingText={t('addressBook.messages.submitting')}
            >
              {canModify ? (isEditMode ? t('addressBook.messages.update') : t('addressBook.messages.create')) : t('addressBook.messages.proposeChange')}
            </SubmitButton>
          </div>
        </form>
      </div>
    </div>
  )
}
