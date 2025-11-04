import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'http://localhost:7107',
        changeOrigin: true,
        secure: false,
      },
      '/healthz': {
        target: 'http://localhost:7107',
        changeOrigin: true,
        secure: false,
      },
      '/health': {
        target: 'http://localhost:7107',
        changeOrigin: true,
        secure: false,
      },
    },
  },
  build: {
    outDir: 'dist',
    sourcemap: true,
    rollupOptions: {
      output: {
        manualChunks: (id) => {
          // Core React libraries
          if (id.includes('node_modules/react') || id.includes('node_modules/react-dom')) {
            return 'vendor-react'
          }
          
          // React Router
          if (id.includes('node_modules/react-router-dom')) {
            return 'vendor-router'
          }
          
          // React Query
          if (id.includes('node_modules/@tanstack/react-query')) {
            return 'vendor-query'
          }
          
          // React Table (large dependency)
          if (id.includes('node_modules/@tanstack/react-table')) {
            return 'vendor-table'
          }
          
          // Icons
          if (id.includes('node_modules/lucide-react') || id.includes('node_modules/@heroicons')) {
            return 'vendor-icons'
          }
          
          // Markdown rendering (heavy)
          if (id.includes('node_modules/react-markdown') || 
              id.includes('node_modules/remark-') || 
              id.includes('node_modules/react-syntax-highlighter') ||
              id.includes('node_modules/prismjs')) {
            return 'vendor-markdown'
          }
          
          // PDF viewer (very large)
          if (id.includes('node_modules/react-pdf') || id.includes('node_modules/pdfjs-dist')) {
            return 'vendor-pdf'
          }
          
          // Utility libraries
          if (id.includes('node_modules/clsx') || 
              id.includes('node_modules/tailwind-merge') ||
              id.includes('node_modules/remove-accents')) {
            return 'vendor-utils'
          }
          
          // Axios (HTTP client)
          if (id.includes('node_modules/axios')) {
            return 'vendor-http'
          }
          
          // Other node_modules
          if (id.includes('node_modules')) {
            return 'vendor-misc'
          }
        },
        // Better cache busting with content hashes
        chunkFileNames: 'assets/[name]-[hash].js',
        entryFileNames: 'assets/[name]-[hash].js',
        assetFileNames: 'assets/[name]-[hash].[ext]',
      },
    },
    chunkSizeWarningLimit: 1000, // Increase warning limit to 1000kB
    // Minification
    minify: 'terser',
  },
})
