import React, { useEffect, useMemo, useState } from 'react'
import { Plus, Edit2, Trash2 } from 'lucide-react'
import { Card } from '@/shared/components/ui/Card'
import { Input } from '@/shared/components/ui/Input'
import { Button } from '@/shared/components/ui/Button'
import { Modal } from '@/shared/components/ui/Modal'
import { useToast } from '@/shared/contexts'
import { useI18n } from '@/shared/contexts/I18nContext'
import type { ForumCategory, ForumSettings } from '@/features/forum/types/forum'
import {
  useCreateForumCategoryMutation,
  useDeleteForumCategoryMutation,
  useForumCategories,
  useForumSettingsQuery,
  useUpdateForumCategoryMutation,
  useUpdateForumSettings,
} from '@/features/forum/hooks/useForumQueries'
import type { CreateForumCategoryPayload, UpdateForumCategoryPayload } from '@/features/forum/services/forum.service'

interface ForumSettingsFormState extends ForumSettings {}

export function ForumSettingsPanel() {
  const { t } = useI18n()
  const { addToast } = useToast()

  const settingsQuery = useForumSettingsQuery()
  const updateSettings = useUpdateForumSettings()

  const categoriesQuery = useForumCategories()
  const createCategoryMutation = useCreateForumCategoryMutation()
  const updateCategoryMutation = useUpdateForumCategoryMutation()
  const deleteCategoryMutation = useDeleteForumCategoryMutation()

  const [formState, setFormState] = useState<ForumSettingsFormState>({
    enableAttachments: true,
    maxAttachmentCount: 5,
    maxAttachmentSizeMb: 5,
    enableEmailNotifications: true,
    badgeRefreshSeconds: 60,
  })
  const [isSubmitting, setIsSubmitting] = useState(false)

  const [categoryModal, setCategoryModal] = useState<{ mode: 'create' | 'edit'; category?: ForumCategory } | null>(null)
  const [categoryForm, setCategoryForm] = useState<CreateForumCategoryPayload>({
    name: '',
    slug: '',
    description: '',
    order: undefined,
    isArchived: false,
  })
  const [categorySubmitting, setCategorySubmitting] = useState(false)

  useEffect(() => {
    if (settingsQuery.data) {
      setFormState(settingsQuery.data)
    }
  }, [settingsQuery.data])

  const categories = useMemo(() => categoriesQuery.data ?? [], [categoriesQuery.data])

  const handleSettingsChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const { name, type, checked, value } = event.target
    const newValue = type === 'checkbox' ? checked : parseInt(value, 10) || 0
    setFormState(prev => ({ ...prev, [name]: newValue }))
  }

  const handleSettingsSubmit = async (event: React.FormEvent) => {
    event.preventDefault()
    try {
      setIsSubmitting(true)
      await updateSettings.mutateAsync(formState)
      addToast({
        type: 'success',
        title: t('common.success'),
        message: t('settings.forum.toast.update_success'),
      })
    } catch {
      addToast({
        type: 'error',
        title: t('common.error'),
        message: t('settings.forum.toast.update_error'),
      })
    } finally {
      setIsSubmitting(false)
    }
  }

  const openCreateCategoryModal = () => {
    setCategoryForm({
      name: '',
      slug: '',
      description: '',
      order: (categories?.length ?? 0) + 1,
      isArchived: false,
    })
    setCategoryModal({ mode: 'create' })
  }

  const openEditCategoryModal = (category: ForumCategory) => {
    setCategoryForm({
      name: category.name,
      slug: category.slug,
      description: category.description ?? '',
      order: category.order,
      isArchived: category.isArchived,
    })
    setCategoryModal({ mode: 'edit', category })
  }

  const closeCategoryModal = () => {
    setCategoryModal(null)
  }

  const handleCategoryFieldChange = (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value, type } = event.target
    if (type === 'checkbox') {
      setCategoryForm(prev => ({ ...prev, [name]: (event.target as HTMLInputElement).checked }))
    } else if (name === 'order') {
      setCategoryForm(prev => ({ ...prev, order: value ? parseInt(value, 10) : undefined }))
    } else {
      setCategoryForm(prev => ({ ...prev, [name]: value }))
    }
  }

  const handleCategorySubmit = async (event: React.FormEvent) => {
    event.preventDefault()
    setCategorySubmitting(true)
    try {
      if (categoryModal?.mode === 'edit' && categoryModal.category) {
        const payload: UpdateForumCategoryPayload = {
          id: categoryModal.category.id,
          ...categoryForm,
        }
        await updateCategoryMutation.mutateAsync(payload)
        addToast({
          type: 'success',
          title: t('common.success'),
          message: t('settings.forum.toast.category_updated'),
        })
      } else {
        await createCategoryMutation.mutateAsync(categoryForm)
        addToast({
          type: 'success',
          title: t('common.success'),
          message: t('settings.forum.toast.category_created'),
        })
      }
      closeCategoryModal()
    } catch {
      addToast({
        type: 'error',
        title: t('common.error'),
        message: t('settings.forum.toast.category_error'),
      })
    } finally {
      setCategorySubmitting(false)
    }
  }

  const handleDeleteCategory = async (category: ForumCategory) => {
    if (!confirm(t('settings.forum.confirm.delete_category', { name: category.name }))) return
    try {
      await deleteCategoryMutation.mutateAsync(category.id)
      addToast({
        type: 'success',
        title: t('common.success'),
        message: t('settings.forum.toast.category_deleted'),
      })
    } catch {
      addToast({
        type: 'error',
        title: t('common.error'),
        message: t('settings.forum.toast.category_delete_error'),
      })
    }
  }

  return (
    <div className="space-y-6">
      <Card className="p-6">
        <form onSubmit={handleSettingsSubmit} className="space-y-6">
          <div>
            <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">{t('settings.forum.section.general')}</h2>
            <p className="text-sm text-gray-600 dark:text-gray-300">{t('settings.forum.section.general_hint')}</p>
          </div>

          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <label className="flex items-center text-sm font-medium text-gray-700 dark:text-gray-200">
              <input
                type="checkbox"
                name="enableAttachments"
                checked={formState.enableAttachments}
                onChange={handleSettingsChange}
                className="form-checkbox"
              />
              <span className="ml-2">{t('settings.forum.fields.enableAttachments')}</span>
            </label>
            <label className="flex items-center text-sm font-medium text-gray-700 dark:text-gray-200">
              <input
                type="checkbox"
                name="enableEmailNotifications"
                checked={formState.enableEmailNotifications}
                onChange={handleSettingsChange}
                className="form-checkbox"
              />
              <span className="ml-2">{t('settings.forum.fields.enableEmailNotifications')}</span>
            </label>
          </div>

          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">
                {t('settings.forum.fields.maxAttachmentCount')}
              </label>
              <Input
                type="number"
                name="maxAttachmentCount"
                value={formState.maxAttachmentCount}
                onChange={handleSettingsChange}
                min={1}
                max={50}
              />
              <p className="mt-1 text-xs text-gray-500 dark:text-gray-400">{t('settings.forum.hints.maxAttachmentCount')}</p>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">
                {t('settings.forum.fields.maxAttachmentSizeMb')}
              </label>
              <Input
                type="number"
                name="maxAttachmentSizeMb"
                value={formState.maxAttachmentSizeMb}
                onChange={handleSettingsChange}
                min={1}
                max={100}
              />
              <p className="mt-1 text-xs text-gray-500 dark:text-gray-400">{t('settings.forum.hints.maxAttachmentSizeMb')}</p>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-200">
                {t('settings.forum.fields.badgeRefreshSeconds')}
              </label>
              <Input
                type="number"
                name="badgeRefreshSeconds"
                value={formState.badgeRefreshSeconds}
                onChange={handleSettingsChange}
                min={15}
                max={600}
              />
              <p className="mt-1 text-xs text-gray-500 dark:text-gray-400">{t('settings.forum.hints.badgeRefreshSeconds')}</p>
            </div>
          </div>

          <div className="flex justify-end">
            <Button type="submit" variant="primary" disabled={isSubmitting || settingsQuery.isLoading}>
              {isSubmitting ? t('common.processing') : t('settings.forum.actions.save')}
            </Button>
          </div>
        </form>
      </Card>

      <Card className="p-6">
        <div className="mb-4 flex items-center justify-between gap-3">
          <div>
            <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">{t('settings.forum.section.categories')}</h2>
            <p className="text-sm text-gray-600 dark:text-gray-300">{t('settings.forum.section.categories_hint')}</p>
          </div>
          <Button onClick={openCreateCategoryModal} variant="primary">
            <Plus className="mr-2 h-4 w-4" />
            {t('settings.forum.actions.addCategory')}
          </Button>
        </div>

        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead>
              <tr>
                <th className="px-3 py-2 text-left text-sm font-semibold text-gray-700 dark:text-gray-200">{t('settings.forum.table.name')}</th>
                <th className="px-3 py-2 text-left text-sm font-semibold text-gray-700 dark:text-gray-200">{t('settings.forum.table.slug')}</th>
                <th className="px-3 py-2 text-left text-sm font-semibold text-gray-700 dark:text-gray-200">{t('settings.forum.table.order')}</th>
                <th className="px-3 py-2 text-left text-sm font-semibold text-gray-700 dark:text-gray-200">{t('settings.forum.table.status')}</th>
                <th className="px-3 py-2 text-right text-sm font-semibold text-gray-700 dark:text-gray-200">{t('settings.forum.table.actions')}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
              {categories.map(category => (
                <tr key={category.id}>
                  <td className="px-3 py-2 text-sm text-gray-900 dark:text-gray-100">{category.name}</td>
                  <td className="px-3 py-2 text-sm text-gray-600 dark:text-gray-300">{category.slug}</td>
                  <td className="px-3 py-2 text-sm text-gray-600 dark:text-gray-300">{category.order}</td>
                  <td className="px-3 py-2 text-sm">
                    {category.isArchived ? (
                      <span className="rounded-full bg-gray-200 px-2 py-0.5 text-xs font-medium text-gray-700 dark:bg-gray-700 dark:text-gray-200">
                        {t('settings.forum.status.archived')}
                      </span>
                    ) : (
                      <span className="rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-700 dark:bg-green-900/30 dark:text-green-300">
                        {t('settings.forum.status.active')}
                      </span>
                    )}
                  </td>
                  <td className="px-3 py-2 text-right text-sm">
                    <div className="flex justify-end gap-2">
                      <Button variant="secondary" size="sm" onClick={() => openEditCategoryModal(category)}>
                        <Edit2 className="mr-1 h-4 w-4" />
                        {t('common.edit')}
                      </Button>
                      <Button
                        variant="destructive"
                        size="sm"
                        onClick={() => handleDeleteCategory(category)}
                        disabled={deleteCategoryMutation.isPending}
                      >
                        <Trash2 className="mr-1 h-4 w-4" />
                        {t('common.delete')}
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {categories.length === 0 && (
            <p className="mt-4 text-sm text-gray-600 dark:text-gray-300">{t('settings.forum.empty.categories')}</p>
          )}
        </div>
      </Card>

      <CategoryModal
        open={categoryModal !== null}
        mode={categoryModal?.mode ?? 'create'}
        formState={categoryForm}
        onClose={closeCategoryModal}
        onChange={handleCategoryFieldChange}
        onSubmit={handleCategorySubmit}
        submitting={categorySubmitting}
        title={
          categoryModal?.mode === 'edit'
            ? t('settings.forum.modal.editCategoryTitle')
            : t('settings.forum.modal.createCategoryTitle')
        }
      />
    </div>
  )
}

interface CategoryModalProps {
  open: boolean
  mode: 'create' | 'edit'
  formState: CreateForumCategoryPayload
  onClose: () => void
  onChange: (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => void
  onSubmit: (event: React.FormEvent) => Promise<void>
  submitting: boolean
  title: string
}

function CategoryModal({ open, mode, formState, onClose, onChange, onSubmit, submitting, title }: CategoryModalProps) {
  const { t } = useI18n()

  return (
    <Modal isOpen={open} onClose={onClose} title={title} size="md">
      <form onSubmit={onSubmit} className="space-y-4 p-6">
        <div>
          <label htmlFor="forum-category-name" className="block text-sm font-medium text-gray-700 dark:text-gray-200">
            {t('settings.forum.fields.name')}
          </label>
          <Input id="forum-category-name" name="name" value={formState.name} onChange={onChange} required maxLength={100} />
        </div>
        <div>
          <label htmlFor="forum-category-slug" className="block text-sm font-medium text-gray-700 dark:text-gray-200">
            {t('settings.forum.fields.slug')}
          </label>
          <Input id="forum-category-slug" name="slug" value={formState.slug} onChange={onChange} required maxLength={100} />
          <p className="mt-1 text-xs text-gray-500 dark:text-gray-400">{t('settings.forum.hints.slug')}</p>
        </div>
        <div>
          <label htmlFor="forum-category-description" className="block text-sm font-medium text-gray-700 dark:text-gray-200">
            {t('settings.forum.fields.description')}
          </label>
          <textarea
            id="forum-category-description"
            name="description"
            value={formState.description ?? ''}
            onChange={onChange}
            className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100"
            maxLength={500}
            rows={3}
          />
        </div>
        <div>
          <label htmlFor="forum-category-order" className="block text-sm font-medium text-gray-700 dark:text-gray-200">
            {t('settings.forum.fields.order')}
          </label>
          <Input
            id="forum-category-order"
            name="order"
            type="number"
            value={formState.order ?? ''}
            onChange={onChange}
            min={1}
            max={200}
          />
        </div>
        <label className="flex items-center text-sm font-medium text-gray-700 dark:text-gray-200">
          <input
            type="checkbox"
            name="isArchived"
            checked={formState.isArchived ?? false}
            onChange={onChange}
            className="form-checkbox"
          />
          <span className="ml-2">{t('settings.forum.fields.isArchived')}</span>
        </label>

        <div className="flex justify-end gap-3">
          <Button type="button" variant="secondary" onClick={onClose}>
            {t('common.cancel')}
          </Button>
          <Button type="submit" variant="primary" disabled={submitting}>
            {submitting ? t('common.processing') : mode === 'edit' ? t('common.save') : t('settings.forum.actions.create')}
          </Button>
        </div>
      </form>
    </Modal>
  )
}

