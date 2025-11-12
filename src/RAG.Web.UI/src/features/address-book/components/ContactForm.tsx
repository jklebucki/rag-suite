// ContactForm - Modal form for creating/editing contacts
import React, { useState, useEffect, useActionState } from 'react'
import type { ContactListItem, ContactData, CreateContactRequest, UpdateContactRequest } from '@/features/address-book/types/addressbook'
import { SubmitButton } from '@/shared/components/ui/SubmitButton'

interface FormState {
  success: boolean
  error: string | null
}

interface ContactFormProps {
  contact?: ContactListItem | null // null/undefined = create mode
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
        company: null, // not in list view
        workPhone: null,
        mobilePhone: contact.mobilePhone,
        email: contact.email,
        notes: null,
        photoUrl: null
      })
      setIsActive(contact.isActive)
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

  if (!isOpen) return null

  return (
    <div className="fixed inset-0 bg-black/60 dark:bg-black/70 flex items-center justify-center z-50 p-4">
      <div className="surface rounded-2xl shadow-xl max-w-2xl w-full max-h-[90vh] overflow-hidden">
        <div className="sticky top-0 surface-muted border-b border-gray-200 dark:border-slate-700 px-6 py-4">
          <div className="flex items-center justify-between">
            <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">
              {title || (isEditMode ? 'Edit Contact' : 'Create Contact')}
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
              You are proposing a change. It will be reviewed by an administrator.
            </p>
          )}
        </div>

        <form
          action={formAction}
          className="px-6 py-4 space-y-5 overflow-y-auto max-h-[calc(90vh-88px)]"
        >
          {/* Hidden input for tags array */}
          <input type="hidden" name="tags" value={JSON.stringify(tags)} />
          
          {state?.error && (
            <div className="rounded-xl border border-red-200 dark:border-red-700 bg-red-50 dark:bg-red-900/20 text-red-700 dark:text-red-300 px-4 py-3">
              {state.error}
            </div>
          )}

          {/* Required Fields */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label htmlFor="firstName" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">
                First Name <span className="text-red-500">*</span>
              </label>
              <input
                id="firstName"
                name="firstName"
                type="text"
                required
                value={formData.firstName}
                onChange={(e) => handleInputChange('firstName', e.target.value)}
                placeholder="Enter first name"
                className="form-input"
              />
            </div>
            <div>
              <label htmlFor="lastName" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">
                Last Name <span className="text-red-500">*</span>
              </label>
              <input
                id="lastName"
                name="lastName"
                type="text"
                required
                value={formData.lastName}
                onChange={(e) => handleInputChange('lastName', e.target.value)}
                placeholder="Enter last name"
                className="form-input"
              />
            </div>
          </div>

          {/* Optional Fields */}
          <div>
            <label htmlFor="displayName" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">Display Name</label>
            <input
              id="displayName"
              name="displayName"
              type="text"
              value={formData.displayName || ''}
              onChange={(e) => handleInputChange('displayName', e.target.value)}
              placeholder="Enter display name"
              className="form-input"
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label htmlFor="email" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">Email</label>
              <input
                id="email"
                name="email"
                type="email"
                value={formData.email || ''}
                onChange={(e) => handleInputChange('email', e.target.value)}
                placeholder="email@example.com"
                className="form-input"
              />
            </div>
            <div>
              <label htmlFor="mobilePhone" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">Mobile Phone</label>
              <input
                id="mobilePhone"
                name="mobilePhone"
                type="tel"
                value={formData.mobilePhone || ''}
                onChange={(e) => handleInputChange('mobilePhone', e.target.value)}
                placeholder="+48 123 456 789"
                className="form-input"
              />
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label htmlFor="workPhone" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">Work Phone</label>
              <input
                id="workPhone"
                name="workPhone"
                type="tel"
                value={formData.workPhone || ''}
                onChange={(e) => handleInputChange('workPhone', e.target.value)}
                placeholder="+48 123 456 789"
                className="form-input"
              />
            </div>
            <div>
              <label htmlFor="company" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">Company</label>
              <input
                id="company"
                name="company"
                type="text"
                value={formData.company || ''}
                onChange={(e) => handleInputChange('company', e.target.value)}
                placeholder="Company name"
                className="form-input"
              />
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label htmlFor="department" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">Department</label>
              <input
                id="department"
                name="department"
                type="text"
                value={formData.department || ''}
                onChange={(e) => handleInputChange('department', e.target.value)}
                placeholder="Department"
                className="form-input"
              />
            </div>
            <div>
              <label htmlFor="position" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">Position</label>
              <input
                id="position"
                name="position"
                type="text"
                value={formData.position || ''}
                onChange={(e) => handleInputChange('position', e.target.value)}
                placeholder="Position"
                className="form-input"
              />
            </div>
            <div>
              <label htmlFor="location" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">Location</label>
              <input
                id="location"
                name="location"
                type="text"
                value={formData.location || ''}
                onChange={(e) => handleInputChange('location', e.target.value)}
                placeholder="Location"
                className="form-input"
              />
            </div>
          </div>

          <div>
            <label htmlFor="notes" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">Notes</label>
            <textarea
              id="notes"
              name="notes"
              value={formData.notes || ''}
              onChange={(e) => handleInputChange('notes', e.target.value)}
              rows={3}
              placeholder="Additional notes"
              className="form-input min-h-[120px]"
            />
          </div>

          <div>
            <label htmlFor="photoUrl" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">Photo URL</label>
            <input
              id="photoUrl"
              name="photoUrl"
              type="url"
              value={formData.photoUrl || ''}
              onChange={(e) => handleInputChange('photoUrl', e.target.value)}
              placeholder="https://example.com/photo.jpg"
              className="form-input"
            />
          </div>

          {/* Tags */}
          <div>
            <label htmlFor="tagInput" className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">Tags</label>
            <div className="flex flex-col sm:flex-row gap-2 mb-2">
              <input
                id="tagInput"
                type="text"
                value={tagInput}
                onChange={(e) => setTagInput(e.target.value)}
                onKeyPress={(e) => e.key === 'Enter' && (e.preventDefault(), handleAddTag())}
                placeholder="Add tag..."
                className="form-input sm:flex-1"
              />
              <button
                type="button"
                onClick={handleAddTag}
                className="btn-secondary whitespace-nowrap"
              >
                Add
              </button>
            </div>
            {tags.length > 0 && (
              <div className="flex flex-wrap gap-2">
                {tags.map((tag) => (
                  <span
                    key={tag}
                    className="inline-flex items-center gap-1 px-3 py-1 rounded-full text-sm bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-200"
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
            <div>
              <label className="flex items-center gap-3">
                <input
                  type="checkbox"
                  name="isActive"
                  checked={isActive}
                  onChange={(e) => setIsActive(e.target.checked)}
                  className="form-checkbox"
                />
                <span className="text-sm font-medium text-gray-700 dark:text-gray-200">Active</span>
              </label>
            </div>
          )}

          {/* Form Actions */}
          <div className="flex justify-end gap-3 pt-4 border-t border-gray-200 dark:border-slate-700">
            <button
              type="button"
              onClick={onClose}
              className="btn-secondary"
            >
              Cancel
            </button>
            <SubmitButton
              className="btn-primary"
              loadingText="Submitting..."
            >
              {canModify ? (isEditMode ? 'Update' : 'Create') : 'Propose Change'}
            </SubmitButton>
          </div>
        </form>
      </div>
    </div>
  )
}
