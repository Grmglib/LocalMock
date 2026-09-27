import { fileURLToPath } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  base: './',
  plugins: [vue()],
  build: {
    outDir: '../wwwroot/ui',
    emptyOutDir: true,
  },
  server: {
    proxy: {
      '/mock': 'http://localhost:5183',
      '/api': 'http://localhost:5183',
    },
  },
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
})
