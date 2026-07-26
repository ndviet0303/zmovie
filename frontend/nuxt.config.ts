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
      siteUrl: process.env.NUXT_PUBLIC_SITE_URL ?? 'https://movie.ziet.dev',
      googleClientId: process.env.NUXT_PUBLIC_GOOGLE_CLIENT_ID ?? '39010162417-34sjb806htrhds433p75b6s4k1l928nk.apps.googleusercontent.com',
    },
  },
  routeRules: {
    '/v1/**': { proxy: 'http://localhost:5275/v1/**' },
    // The admin area is session-gated and must never be baked into the public
    // static output. It renders client-side and is reached through the SPA
    // fallback in public/_redirects.
    '/admin/**': { ssr: false, prerender: false },
    '/admin': { ssr: false, prerender: false },
  },
  nitro: {
    prerender: {
      ignore: ['/admin'],
    },
  },
  components: [{ path: '~/components', pathPrefix: false, ignore: ['**/index.ts'] }],
})
