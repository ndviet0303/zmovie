import tailwindcss from '@tailwindcss/vite'

// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: true },
  ssr: true,
  modules: ['@nuxt/eslint'],
  css: ['../assets/css/main.css'],
  app: {
    head: {
      title: 'ZMovie — Xem phim hay',
      link: [
        { rel: 'icon', type: 'image/svg+xml', href: '/favicon.svg' },
        { rel: 'apple-touch-icon', href: '/favicon.svg' },
      ],
      meta: [
        { name: 'referrer', content: 'no-referrer-when-downgrade' },
        { name: 'theme-color', content: '#181A20' },
        { property: 'og:site_name', content: 'ZMovie' },
        { property: 'og:type', content: 'website' },
        { property: 'og:locale', content: 'vi_VN' },
        { property: 'og:image', content: 'https://movie.ziet.dev/og-image.svg' },
        { property: 'og:image:width', content: '1200' },
        { property: 'og:image:height', content: '630' },
        { name: 'twitter:card', content: 'summary_large_image' },
        { name: 'twitter:site', content: '@zmovie' },
      ],
    },
  },
  vite: { plugins: [tailwindcss()] },
  runtimeConfig: {
    public: {
      apiBaseUrl: '/',
      localAiUrl: process.env.NUXT_PUBLIC_LOCAL_AI_URL ?? 'http://127.0.0.1:8788',
      siteUrl: process.env.NUXT_PUBLIC_SITE_URL ?? 'https://movie.ziet.dev',
      googleClientId: process.env.NUXT_PUBLIC_GOOGLE_CLIENT_ID ?? '39010162417-34sjb806htrhds433p75b6s4k1l928nk.apps.googleusercontent.com',
    },
  },
  routeRules: {
    '/v1/**': { proxy: 'http://localhost:5275/v1/**' },
  },
  components: [{ path: '~/components', pathPrefix: false, ignore: ['**/index.ts'] }],
})
