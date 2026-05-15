import { defineConfig, loadEnv } from 'vite'
import tailwindcss from '@tailwindcss/vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig(({ mode }) => ({
  plugins: [vue(), tailwindcss()],
  server: {
    proxy: createApiProxy(mode),
  },
}))

function createApiProxy(mode) {
  const env = loadEnv(mode, process.cwd(), '')
  const apiBaseUrl = normalizeApiBaseUrl(env.API_ORIGIN || env.VITE_API_PROXY_TARGET)

  if (!apiBaseUrl) {
    return {}
  }

  const targetUrl = new URL(apiBaseUrl)
  const targetPath = targetUrl.pathname.replace(/\/+$/, '')

  return {
    '/api': {
      target: targetUrl.origin,
      changeOrigin: true,
      secure: true,
      rewrite: (path) => {
        const apiPath = path.replace(/^\/api\/v\d+/i, '')
        return `${targetPath}${apiPath}`.replace(/\/{2,}/g, '/')
      },
    },
  }
}

function normalizeApiBaseUrl(value) {
  const trimmedUrl = String(value || '').replace(/\/+$/, '')

  if (!trimmedUrl) {
    return ''
  }

  if (/\/api\/v\d+$/i.test(trimmedUrl)) {
    return trimmedUrl
  }

  return `${trimmedUrl}/api/v1`
}
