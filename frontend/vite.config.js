import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  css: {
    preprocessorOptions: {
      scss: {
        additionalData: `@import "@/daping/styles/common/style.scss";`
      }
    }
  },
  optimizeDeps: {
    include: [
      'naive-ui',
      'echarts',
      'vue-echarts',
      '@vueuse/core'
    ]
  },
  server: {
    port: 9999,
    host: '127.0.0.1',
    strictPort: false,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        secure: false,
        rewrite: (path) => path
      }
    }
  }
})
