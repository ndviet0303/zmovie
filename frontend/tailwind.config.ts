import type { Config } from 'tailwindcss'

export default {
  content: [
    './app/**/*.{vue,js,ts}',
    './components/**/*.{vue,js,ts}',
    './pages/**/*.vue',
  ],
  theme: { extend: {} },
  plugins: [],
} satisfies Config
