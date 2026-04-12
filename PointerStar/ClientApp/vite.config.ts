/// <reference types="vitest/config" />

import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: process.env.VITE_BACKEND_URL
    ? {
        proxy: {
          '/api': {
            changeOrigin: true,
            secure: false,
            target: process.env.VITE_BACKEND_URL,
          },
          '/RoomHub': {
            changeOrigin: true,
            secure: false,
            target: process.env.VITE_BACKEND_URL,
            ws: true,
          },
        },
      }
    : undefined,
  test: {
    css: true,
    environment: 'jsdom',
    globals: true,
    setupFiles: './src/test/setup.ts',
  },
})
