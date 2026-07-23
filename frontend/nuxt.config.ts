import tailwindcss from '@tailwindcss/vite'

// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: true },
  ssr: false,
  modules: ['@nuxt/eslint'],
  css: ['../assets/css/main.css'],
  app: {
    head: {
      title: 'ZMovie — Xem phim hay',
      link: [{ rel: 'icon', type: 'image/svg+xml', href: '/favicon.svg' }],
      meta: [{ name: 'referrer', content: 'no-referrer-when-downgrade' }],
    },
  },
  vite: { plugins: [tailwindcss()] },
  runtimeConfig: {
    public: {
      apiBaseUrl: '/',
      googleClientId: process.env.NUXT_PUBLIC_GOOGLE_CLIENT_ID ?? '39010162417-34sjb806htrhds433p75b6s4k1l928nk.apps.googleusercontent.com',
    },
  },
  routeRules: {
    '/v1/**': { proxy: 'http://localhost:5275/v1/**' },
  },
  components: [{ path: '~/components', pathPrefix: false, ignore: ['**/index.ts'] }],
})
