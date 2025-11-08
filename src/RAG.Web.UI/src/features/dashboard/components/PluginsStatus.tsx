import React from 'react'
import { Database } from 'lucide-react'

interface Plugin {
  id: string
  name: string
  version: string
  enabled: boolean
}

interface PluginsStatusProps {
  plugins?: Plugin[]
  pluginUsage?: Record<string, number>
}

export function PluginsStatus({ plugins, pluginUsage }: PluginsStatusProps) {
  return (
    <div className="bg-white rounded-lg shadow-sm border p-6">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold text-gray-900">Plugins Status</h2>
        <Database className="h-5 w-5 text-gray-400" />
      </div>

      <div className="space-y-3">
        {plugins?.map((plugin) => (
          <div key={plugin.id} className="flex items-center justify-between py-2">
            <div className="flex items-center gap-3">
              <div className={`w-3 h-3 rounded-full ${
                plugin.enabled ? 'bg-green-400' : 'bg-gray-300'
              }`} />
              <div>
                <p className="text-sm font-medium text-gray-900">{plugin.name}</p>
                <p className="text-xs text-gray-500">v{plugin.version}</p>
              </div>
            </div>
            <span className="text-xs text-gray-500">
              {pluginUsage?.[plugin.id] || 0} uses
            </span>
          </div>
        )) || (
          <div className="text-gray-500 text-sm py-4 text-center">
            No plugins configured
          </div>
        )}
      </div>
    </div>
  )
}
