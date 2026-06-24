import type { ComponentType, ReactNode } from 'react'

interface SectionCardProps {
  icon: ComponentType<{ className?: string }>
  title: string
  children: ReactNode
  iconBg?: string
  iconColor?: string
}

export function SectionCard({
  icon: Icon,
  title,
  children,
  iconBg = 'bg-primary-50 dark:bg-primary-900/20',
  iconColor = 'text-primary-600 dark:text-primary-400',
}: SectionCardProps) {
  return (
    <div className="surface p-5 flex flex-col gap-1">
      <div className="flex items-center gap-2 mb-3">
        <div className={`p-2 rounded-lg ${iconBg}`}>
          <Icon className={`h-5 w-5 ${iconColor}`} />
        </div>
        <h2 className="font-semibold text-gray-900 dark:text-gray-100">{title}</h2>
      </div>
      {children}
    </div>
  )
}
