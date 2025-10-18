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
        manualChunks: {
          // Separate vendor chunks
          'vendor-react': ['react', 'react-dom'],
          'vendor-router': ['react-router-dom'],
          'vendor-query': ['@tanstack/react-query'],
          'vendor-icons': ['lucide-react'],
          'vendor-utils': ['clsx', 'tailwind-merge', 'axios'],
          // PDF viewer in separate chunk
          'pdf-viewer': ['react-pdf', 'pdfjs-dist'],
        },
      },
    },
    chunkSizeWarningLimit: 1000, // Increase warning limit to 1000kB
  },
})
