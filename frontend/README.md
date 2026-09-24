# Frontend (Nuxt 4)

SPA em Nuxt 4 + Nuxt UI + Pinia que conversa **somente** com o API Gateway (`NUXT_PUBLIC_API_BASE`).

```bash
npm install
npm run dev      # http://localhost:3100 (gateway em http://localhost:5100)
npm test         # vitest
```

| Pasta | Conteúdo |
|-------|----------|
| `app/pages` | Telas: produtos, pedidos, arquitetura |
| `app/stores` | Estado (Pinia) — compõe Catalog + Inventory na tela de produtos |
| `app/composables/useApi.ts` | Cliente HTTP tipado do gateway |
| `app/composables/useOrderHub.ts` | Conexão SignalR para status de pedidos em tempo real |
