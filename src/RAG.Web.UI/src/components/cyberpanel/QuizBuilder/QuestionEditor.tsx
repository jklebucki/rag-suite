import { Trash2, Plus, ImageIcon, X, ChevronUp, ChevronDown } from 'lucide-react'
import { Button, Input, Textarea, Card, CardContent } from '@/components/ui'
import type { CreateQuizQuestionDto, CreateQuizOptionDto } from '@/types'
import { useI18n } from '@/contexts/I18nContext'
import { AnswerEditor } from './AnswerEditor'

interface QuestionEditorProps {
  question: CreateQuizQuestionDto
  questionIndex: number
  totalQuestions: number
  onUpdate: (field: keyof CreateQuizQuestionDto, value: any) => void
  onRemove: () => void
  onMove: (direction: 'up' | 'down') => void
  onImageUpload: (file: File) => void
  onAddOption: () => void
  onRemoveOption: (optionIndex: number) => void
  onUpdateOption: (optionIndex: number, field: keyof CreateQuizOptionDto, value: any) => void
  onOptionImageUpload: (optionIndex: number, file: File) => void
}

export function QuestionEditor({
  question,
  questionIndex,
  totalQuestions,
  onUpdate,
  onRemove,
  onMove,
  onImageUpload,
  onAddOption,
  onRemoveOption,
  onUpdateOption,
  onOptionImageUpload,
}: QuestionEditorProps) {
  const { t } = useI18n()

  const handleImageChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (file) {
      onImageUpload(file)
    }
  }

  return (
    <Card>
      <CardContent className="pt-6">
        <div className="flex justify-between items-center mb-4">
          <h3 className="text-lg font-semibold">
            {t('cyberpanel.question')} {questionIndex + 1}
          </h3>
          <div className="flex gap-2">
            <Button
              type="button"
              onClick={() => onMove('up')}
              disabled={questionIndex === 0}
              variant="outline"
              size="sm"
              title="Move up"
            >
              <ChevronUp className="w-4 h-4" />
            </Button>
            <Button
              type="button"
              onClick={() => onMove('down')}
              disabled={questionIndex === totalQuestions - 1}
              variant="outline"
              size="sm"
              title="Move down"
            >
              <ChevronDown className="w-4 h-4" />
            </Button>
            <Button
              type="button"
              onClick={onRemove}
              variant="destructive"
              size="sm"
            >
              <Trash2 className="w-4 h-4" />
            </Button>
          </div>
        </div>

        <div className="space-y-4">
          <Textarea
            placeholder={t('cyberpanel.questionText')}
            value={question.text}
            onChange={(e) => onUpdate('text', e.target.value)}
            rows={3}
          />

          {question.imageUrl && (
            <div className="relative inline-block">
              <img
                src={question.imageUrl}
                alt="Question"
                className="max-w-full h-auto rounded border"
              />
              <button
                type="button"
                onClick={() => onUpdate('imageUrl', null)}
                className="absolute top-2 right-2 bg-red-500 text-white rounded-full p-1 hover:bg-red-600"
                title="Remove image"
                aria-label="Remove image"
              >
                <X className="w-4 h-4" />
              </button>
            </div>
          )}

          <div className="flex gap-4 items-center">
            <label className="cursor-pointer">
              <input
                type="file"
                accept="image/*"
                onChange={handleImageChange}
                className="hidden"
              />
              <Button type="button" variant="outline" size="sm">
                <ImageIcon className="w-4 h-4 mr-2" />
                Add Image
              </Button>
            </label>

            <div className="flex items-center gap-2">
              <label className="text-sm font-medium">{t('cyberpanel.points')}:</label>
              <Input
                type="number"
                value={question.points}
                onChange={(e) => onUpdate('points', parseInt(e.target.value) || 0)}
                className="w-20"
                min="0"
              />
            </div>
          </div>

          <div className="space-y-2">
            <div className="flex justify-between items-center">
              <label className="text-sm font-medium">{t('cyberpanel.options')}:</label>
              <Button
                type="button"
                onClick={onAddOption}
                variant="outline"
                size="sm"
              >
                <Plus className="w-4 h-4 mr-1" />
                {t('cyberpanel.addOption')}
              </Button>
            </div>

            {question.options.map((option, optionIndex) => (
              <AnswerEditor
                key={optionIndex}
                option={option}
                optionIndex={optionIndex}
                questionIndex={questionIndex}
                onUpdate={(field, value) => onUpdateOption(optionIndex, field, value)}
                onRemove={() => onRemoveOption(optionIndex)}
                onImageUpload={(file) => onOptionImageUpload(optionIndex, file)}
                canRemove={question.options.length > 2}
              />
            ))}
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
