import { useRef } from 'react'
import { CalendarDays } from 'lucide-react'

interface DatePickerInputProps {
  id: string
  value: string
  min?: string
  onChange: (value: string) => void
  error?: boolean
}

export function DatePickerInput({ id, value, min, onChange, error }: DatePickerInputProps) {
  const inputRef = useRef<HTMLInputElement>(null)

  return (
    <div className="relative">
      <input
        ref={inputRef}
        id={id}
        type="date"
        value={value}
        min={min}
        onChange={(e) => onChange(e.target.value)}
        className={`flex h-10 w-full rounded-md border bg-white px-3 py-2 pr-10 text-sm ring-offset-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-gray-800 dark:text-gray-100 dark:ring-offset-gray-900 [color-scheme:light] dark:[color-scheme:dark] [&::-webkit-calendar-picker-indicator]:absolute [&::-webkit-calendar-picker-indicator]:right-0 [&::-webkit-calendar-picker-indicator]:h-full [&::-webkit-calendar-picker-indicator]:w-10 [&::-webkit-calendar-picker-indicator]:cursor-pointer [&::-webkit-calendar-picker-indicator]:opacity-0 ${
          error
            ? 'border-red-500 focus-visible:ring-red-500 dark:border-red-600'
            : 'border-gray-300 focus-visible:ring-blue-600 dark:border-gray-600 dark:focus-visible:ring-blue-500'
        }`}
      />
      <CalendarDays className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-gray-400 dark:text-gray-500" />
    </div>
  )
}
