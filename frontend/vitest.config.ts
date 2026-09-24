import { fileURLToPath } from 'node:url'
import { defineConfig } from 'vitest/config'

// Pure unit tests (no Nuxt runtime needed) for utils and domain helpers.
export default defineConfig({
  resolve: {
    alias: { '~': fileURLToPath(new URL('./app', import.meta.url)) },
  },
  test: {
    include: ['tests/**/*.test.ts'],
    environment: 'node',
  },
})
