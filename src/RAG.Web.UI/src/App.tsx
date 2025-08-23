import { Routes, Route } from 'react-router-dom'
import { Layout } from '@/components/Layout'
import { Dashboard } from '@/components/dashboard/Dashboard'
import { ChatInterface } from '@/components/chat/ChatInterface'
import { SearchInterface } from '@/components/search/SearchInterface'

function App() {
  return (
    <Layout>
      <Routes>
        <Route path="/" element={<Dashboard />} />
        <Route path="/chat" element={<ChatInterface />} />
        <Route path="/search" element={<SearchInterface />} />
      </Routes>
    </Layout>
  )
}

export default App
