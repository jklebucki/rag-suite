import { Suspense } from 'react'
import { AppProviders } from './AppProviders'
import { AppRoutes } from './AppRoutes'

export function App() {
  return (
    <AppProviders>
      <Suspense fallback={<div>Loading...</div>}>
        <AppRoutes />
      </Suspense>
    </AppProviders>
  )
}

export default App
