import { StrictMode, startTransition } from 'react'
import ReactDOM from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { App } from '@/app'
import './index.css'
import './utils/debug' // Import debug utilities
import { CACHE_TIMES, QUERY_RETRY } from '@/app/config/appConfig'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: CACHE_TIMES.STALE_TIME,
      gcTime: CACHE_TIMES.GC_TIME,
      retry: QUERY_RETRY.QUERIES,
    },
    mutations: {
      retry: QUERY_RETRY.MUTATIONS, // No retry for mutations to prevent double sending
    },
  },
})

const rootElement = document.getElementById('root')

if (!rootElement) {
  throw new Error('Root element with id "root" was not found in the document.')
}

const root = ReactDOM.createRoot(rootElement)

const app = (
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <App />
    </QueryClientProvider>
  </StrictMode>
)

startTransition(() => {
  root.render(app)
})

