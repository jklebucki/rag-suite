import { Trash2, ImageIcon, X } from 'lucide-react'
import { Button, Input } from '@/shared/components/ui'
import type { CreateQuizOptionDto } from '@/features/cyberpanel/types/quiz'
import { useI18n } from '@/shared/contexts/I18nContext'

interface AnswerEditorProps {
  option: CreateQuizOptionDto
  optionIndex: number
  questionIndex: number
  onUpdate: (field: keyof CreateQuizOptionDto, value: string | boolean | null) => void
  onRemove: () => void
  onImageUpload: (file: File) => void
  canRemove: boolean
}

export function AnswerEditor({
  option,
  optionIndex,
  onUpdate,
  onRemove,
  onImageUpload,
  canRemove,
}: AnswerEditorProps) {
  const { t } = useI18n()

  const handleImageChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (file) {
      onImageUpload(file)
    }
  }

  return (
    <div className="flex gap-2 items-start p-3 bg-gray-50 dark:bg-slate-900 rounded border border-gray-200 dark:border-slate-800">
      <input
        type="checkbox"
        checked={option.isCorrect}
        onChange={(e) => onUpdate('isCorrect', e.target.checked)}
        className="form-checkbox mt-2"
        title={t('cyberpanel.correctAnswer')}
        aria-label={t('cyberpanel.correctAnswer')}
      />
      <div className="flex-1 space-y-2">
        <Input
          placeholder={`${t('cyberpanel.option')} ${optionIndex + 1}`}
          value={option.text}
          onChange={(e) => onUpdate('text', e.target.value)}
        />
        {option.imageUrl && (
          <div className="relative inline-block">
            <img
              src={option.imageUrl}
              alt="Option"
              className="max-w-xs h-auto rounded border border-gray-200 dark:border-slate-700"
            />
            <button
              type="button"
              onClick={() => onUpdate('imageUrl', null)}
              className="absolute top-1 right-1 bg-red-500 text-white rounded-full p-1 hover:bg-red-600 dark:bg-red-600 dark:hover:bg-red-500"
              title="Remove image"
              aria-label="Remove image"
            >
              <X className="w-3 h-3" />
            </button>
          </div>
        )}
        <div className="flex gap-2">
          <label className="cursor-pointer">
            <input
              type="file"
              accept="image/*"
              onChange={handleImageChange}
              className="hidden"
            />
            <Button type="button" variant="outline" size="sm">
              <ImageIcon className="w-4 h-4 mr-1" />
              Add Image
            </Button>
          </label>
        </div>
      </div>
      {canRemove && (
        <Button
          type="button"
          onClick={onRemove}
          variant="destructive"
          size="sm"
        >
          <Trash2 className="w-4 h-4" />
        </Button>
      )}
    </div>
  )
}
