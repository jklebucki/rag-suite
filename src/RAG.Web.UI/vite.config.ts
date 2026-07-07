import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

function isPackage(id: string, packageName: string): boolean {
  const normalizedId = id.replace(/\\/g, '/')
  return normalizedId.includes(`/node_modules/${packageName}/`)
}

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
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
          // Core React libraries - MUST be bundled together first
          // This ensures React is available before use-sync-external-store-shim tries to use it
          if (isPackage(id, 'react') ||
              isPackage(id, 'react-dom') ||
              isPackage(id, 'scheduler') ||
              isPackage(id, 'use-sync-external-store')) {
            return 'vendor-react'
          }
          
          // React Router
          if (isPackage(id, 'react-router-dom') || isPackage(id, 'react-router')) {
            return 'vendor-router'
          }
          
          // React Query - depends on React, so it should load after vendor-react
          if (isPackage(id, '@tanstack/react-query')) {
            return 'vendor-query'
          }
          
          // React Table (large dependency)
          if (isPackage(id, '@tanstack/react-table')) {
            return 'vendor-table'
          }
          
          // Icons
          if (isPackage(id, 'lucide-react') || isPackage(id, '@heroicons/react')) {
            return 'vendor-icons'
          }
          
          // Markdown rendering (heavy)
          if (isPackage(id, 'react-markdown') ||
              isPackage(id, 'remark-gfm') ||
              isPackage(id, 'remark-parse') ||
              isPackage(id, 'remark-rehype') ||
              isPackage(id, 'remark-stringify') ||
              isPackage(id, 'react-syntax-highlighter') ||
              isPackage(id, 'prismjs')) {
            return 'vendor-markdown'
          }
          
          // PDF viewer (very large)
          if (isPackage(id, 'react-pdf') || isPackage(id, 'pdfjs-dist')) {
            return 'vendor-pdf'
          }
          
          // Utility libraries
          if (isPackage(id, 'clsx') ||
              isPackage(id, 'tailwind-merge') ||
              isPackage(id, 'remove-accents')) {
            return 'vendor-utils'
          }
          
          // Axios (HTTP client)
          if (isPackage(id, 'axios')) {
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
    // Ensure proper module resolution and deduplication
    commonjsOptions: {
      include: [/node_modules/],
      transformMixedEsModules: true,
      // Handle Prism.js CommonJS exports properly
      strictRequires: false,
      // Ensure Prism.js is properly transformed
      requireReturnsDefault: 'auto',
    },
  },
  optimizeDeps: {
    include: [
      'react', 
      'react-dom', 
      'react/jsx-runtime', 
      'react/jsx-dev-runtime',
      'prismjs',
      'react-syntax-highlighter',
      'react-syntax-highlighter/dist/esm/styles/prism'
    ],
    exclude: [],
    esbuildOptions: {
      // Handle CommonJS modules properly
      target: 'es2020',
    },
  },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
    // Ensure proper resolution of CommonJS modules
    dedupe: ['react', 'react-dom'],
    // Ensure proper conditions for module resolution
    conditions: ['import', 'module', 'browser', 'default'],
  },
  // Explicitly handle Prism.js CommonJS issues
  define: {
    // Ensure Prism.js works correctly in production
    'process.env.NODE_ENV': JSON.stringify(process.env.NODE_ENV || 'production'),
  },
})
