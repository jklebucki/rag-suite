import { AppProviders } from './AppProviders'
import { AppRouterProvider } from './AppRoutes'

export function App() {
  return (
    <AppProviders>
      <AppRouterProvider />
    </AppProviders>
  )
}

export default App
