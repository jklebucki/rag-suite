import React from 'react'
import ReactDOM from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import { App } from '@/app'
import './index.css'
import './utils/debug' // Import debug utilities
import { CACHE_TIMES, QUERY_RETRY } from '@/app/config/appConfig'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: CACHE_TIMES.STALE_TIME,
      cacheTime: CACHE_TIMES.CACHE_TIME,
      retry: QUERY_RETRY.QUERIES,
    },
    mutations: {
      retry: QUERY_RETRY.MUTATIONS, // No retry for mutations to prevent double sending
    },
  },
})

ReactDOM.createRoot(document.getElementById('root')!).render(
  <QueryClientProvider client={queryClient}>
    <BrowserRouter>
      <App />
    </BrowserRouter>
  </QueryClientProvider>,
)

