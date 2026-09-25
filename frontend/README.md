# Frontend (Nuxt 4)

Nuxt 4 + Nuxt UI + Pinia SPA that talks **only** to the API gateway (`NUXT_PUBLIC_API_BASE`).

```bash
npm install
npm run dev      # http://localhost:3100 (gateway at http://localhost:5100)
npm test         # vitest
```

| Folder | Contents |
|--------|----------|
| `app/pages` | Screens: products, orders, architecture |
| `app/stores` | State (Pinia); composes Catalog + Inventory data on the products screen |
| `app/composables/useApi.ts` | Typed HTTP client for the gateway |
| `app/composables/useOrderHub.ts` | SignalR connection for real-time order status |
