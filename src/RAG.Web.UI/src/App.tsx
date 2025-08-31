import { Routes, Route } from 'react-router-dom'
import { Layout } from '@/components/Layout'
import { Dashboard } from '@/components/dashboard/Dashboard'
import { ChatInterface } from '@/components/chat/ChatInterface'
import { SearchInterface } from '@/components/search/SearchInterface'
import { ToastProvider } from '@/contexts/ToastContext'
import { I18nProvider } from '@/contexts/I18nContext'

function App() {
  return (
    <I18nProvider>
      <ToastProvider>
        <Layout>
          <Routes>
            <Route path="/" element={<Dashboard />} />
            <Route path="/chat" element={<ChatInterface />} />
            <Route path="/search" element={<SearchInterface />} />
          </Routes>
        </Layout>
      </ToastProvider>
    </I18nProvider>
  )
}

export default App
