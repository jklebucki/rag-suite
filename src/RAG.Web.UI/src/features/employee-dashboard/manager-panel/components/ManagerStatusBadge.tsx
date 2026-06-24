import type { ComponentType } from 'react'
import { AlertCircle, CheckCircle, Home, Plane, UserCheck, UserX, XCircle } from 'lucide-react'
import type {
  DelegationStatus,
  ManagerApprovalStatus,
  TeamMemberPresenceStatus,
} from '../types/managerTypes'
import {
  approvalStatusLabel,
  delegationStatusLabel,
  presenceStatusLabel,
} from './managerPanelUtils'
import { useManagerT } from './managerTranslations'

type StatusBadgeProps =
  | { type: 'presence'; status: TeamMemberPresenceStatus }
  | { type: 'approval'; status: ManagerApprovalStatus }
  | { type: 'delegation'; status: DelegationStatus }

export function ManagerStatusBadge(props: StatusBadgeProps) {
  const t = useManagerT()
  const config = getConfig(props, t)
  const Icon = config.icon

  return (
    <span
      className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium ${config.className}`}
    >
      <Icon className="h-3 w-3" />
      {config.label}
    </span>
  )
}

function getConfig(props: StatusBadgeProps, t: ReturnType<typeof useManagerT>): {
  icon: ComponentType<{ className?: string }>
  label: string
  className: string
} {
  if (props.type === 'presence') {
    const configs: Record<TeamMemberPresenceStatus, ReturnType<typeof getConfig>> = {
      present: {
        icon: UserCheck,
        label: presenceStatusLabel(props.status, t),
        className: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400',
      },
      vacation: {
        icon: Plane,
        label: presenceStatusLabel(props.status, t),
        className: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
      },
      delegation: {
        icon: Plane,
        label: presenceStatusLabel(props.status, t),
        className: 'bg-violet-100 text-violet-700 dark:bg-violet-900/30 dark:text-violet-400',
      },
      homeOffice: {
        icon: Home,
        label: presenceStatusLabel(props.status, t),
        className: 'bg-cyan-100 text-cyan-700 dark:bg-cyan-900/30 dark:text-cyan-400',
      },
      absence: {
        icon: UserX,
        label: presenceStatusLabel(props.status, t),
        className: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',
      },
    }
    return configs[props.status]
  }

  if (props.type === 'approval') {
    const configs: Record<ManagerApprovalStatus, ReturnType<typeof getConfig>> = {
      pending: {
        icon: AlertCircle,
        label: approvalStatusLabel(props.status, t),
        className: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
      },
      approved: {
        icon: CheckCircle,
        label: approvalStatusLabel(props.status, t),
        className: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400',
      },
      rejected: {
        icon: XCircle,
        label: approvalStatusLabel(props.status, t),
        className: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',
      },
    }
    return configs[props.status]
  }

  const configs: Record<DelegationStatus, ReturnType<typeof getConfig>> = {
    active: {
      icon: CheckCircle,
      label: delegationStatusLabel(props.status, t),
      className: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400',
    },
    scheduled: {
      icon: AlertCircle,
      label: delegationStatusLabel(props.status, t),
      className: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
    },
    expired: {
      icon: XCircle,
      label: delegationStatusLabel(props.status, t),
      className: 'bg-gray-100 text-gray-600 dark:bg-slate-800 dark:text-gray-400',
    },
  }
  return configs[props.status]
}
