<script setup lang="ts">
import type { NavigationMenuItem } from '@nuxt/ui'

const links: NavigationMenuItem[] = [
  { label: 'Produtos', icon: 'i-lucide-package', to: '/products' },
  { label: 'Pedidos', icon: 'i-lucide-shopping-cart', to: '/orders' },
  { label: 'Arquitetura', icon: 'i-lucide-network', to: '/architecture' },
]

const hub = useOrderHub()
onMounted(() => hub.connect())

const hubBadge = computed(() => ({
  connected: { color: 'success' as const, label: 'Tempo real conectado' },
  connecting: { color: 'warning' as const, label: 'Conectando...' },
  disconnected: { color: 'error' as const, label: 'Tempo real offline' },
}[hub.status.value]))
</script>

<template>
  <div class="min-h-screen bg-(--ui-bg-muted)">
    <header class="border-b border-(--ui-border) bg-(--ui-bg)">
      <div class="mx-auto flex max-w-7xl items-center gap-6 px-4 py-3">
        <NuxtLink to="/" class="flex items-center gap-2 font-semibold">
          <UIcon name="i-lucide-boxes" class="size-6 text-(--ui-primary)" />
          Microservices Store
        </NuxtLink>
        <UNavigationMenu :items="links" class="flex-1" />
        <UBadge :color="hubBadge.color" variant="subtle" icon="i-lucide-radio">
          {{ hubBadge.label }}
        </UBadge>
      </div>
    </header>
    <main class="mx-auto max-w-7xl px-4 py-6">
      <slot />
    </main>
  </div>
</template>
