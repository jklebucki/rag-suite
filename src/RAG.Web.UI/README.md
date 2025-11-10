# RAG Web UI

Modern React TypeScript frontend for RAG Suite.

## Features

* 🚀 **Modern Stack**: React 19 + TypeScript + Vite 7
* 🎨 **Tailwind CSS**: Utility-first CSS framework
* 🔍 **Advanced Search**: Full-text search with filters and semantic search
* 💬 **Chat Interface**: Interactive chat with RAG-powered responses
* 📊 **Dashboard**: System metrics and usage analytics
* 🔌 **Plugin Management**: Monitor and manage RAG plugins
* 🧠 **Knowledge Exchange Forum**: Share knowledge, follow discussions, and receive updates on new replies
* 📱 **Responsive Design**: Works on desktop and mobile devices

## Getting Started

### Prerequisites

* Node.js 20.10+
* npm 10+

### Installation


1. Navigate to the UI project directory:

```bash
cd src/RAG.Web.UI
```


2. Install dependencies:

```bash
npm install
```


3. Start the development server:

```bash
npm run dev
```


4. Open your browser and navigate to `http://localhost:3000`

### Building for Production

```bash
npm run build
```

The built files will be in the `dist` directory.

## Project Structure

```
src/
├── app/                  # Router setup and application providers
├── features/             # Feature-oriented modules (forum, chat, search, dashboard, settings, ...)
│   └── forum/            # Knowledge exchange forum views, hooks, and services
├── shared/               # Reusable UI components, contexts, hooks, services
├── locales/              # i18n translation dictionaries
├── utils/                # Utility functions
└── main.tsx              # Application entry point
```

## Knowledge Exchange Forum

The forum adds a structured discussion area tightly integrated with the suite:

* Accessible from the main sidebar as **“Wymiana wiedzy / Knowledge Exchange”**
* Requires authentication; unauthenticated users are redirected to login
* Filter threads by category, search titles, and paginate results
* Create threads with rich text and file attachments (max 5 files, 5 MB each)
* Reply to threads with attachments and optionally subscribe for email notifications
* View unread badges and acknowledge them once a thread is read
* Download attachments securely via the orchestrator API

## Configuration

### API Base URL

The frontend is configured to proxy API requests to the backend. Update the proxy configuration in `vite.config.ts` if needed:

```typescript
server: {
  proxy: {
    '/api': {
      target: 'https://localhost:7000', // Your API URL
      changeOrigin: true,
      secure: false,
    },
  },
}
```

### Environment Variables

Create a `.env` file for environment-specific configuration:

```
VITE_API_BASE_URL=https://localhost:7000
VITE_APP_NAME=RAG Suite
```

## Available Scripts

* `npm run dev` - Start development server
* `npm run build` - Build for production
* `npm run preview` - Preview production build
* `npm run lint` - Run ESLint
* `npm run type-check` - Run TypeScript type checking
* `npm run test:run` - Run Vitest unit tests

## Architecture

The frontend follows a modern React architecture with:

* **Component-based structure**: Reusable UI components
* **Type safety**: Full TypeScript coverage
* **State management**: React Query for server state
* **Routing**: React Router for navigation
* **Styling**: Tailwind CSS for consistent design
* **API integration**: Axios with interceptors for error handling

## Integration with Backend

The UI integrates with the RAG.Orchestrator.Api backend through:

* REST API calls for search, chat, and management operations
* WebSocket connections for real-time chat (planned)
* JWT authentication for secure access
* Error handling and user feedback

## Contributing


1. Follow the existing code style and conventions
2. Add TypeScript types for new features
3. Write unit tests for components
4. Update documentation as needed


