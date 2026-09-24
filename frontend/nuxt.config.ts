// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2026-09-01',
  devtools: { enabled: true },

  // SPA mode: the browser talks directly to the API Gateway (and its SignalR hub),
  // so there is no server-side data fetching from inside the container.
  ssr: false,

  modules: ['@nuxt/ui', '@pinia/nuxt'],

  css: ['~/assets/css/main.css'],

  runtimeConfig: {
    public: {
      // Overridden at runtime by NUXT_PUBLIC_API_BASE (see docker-compose.yml).
      apiBase: 'http://localhost:5100',
      rabbitMqUrl: 'http://localhost:15672',
      dashboardUrl: 'http://localhost:18888',
    },
  },

  app: {
    head: {
      htmlAttrs: { lang: 'pt-BR' },
      title: 'Microservices Store',
    },
  },

  typescript: {
    strict: true,
  },
})
