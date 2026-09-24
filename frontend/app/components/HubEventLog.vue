<script setup lang="ts">
const orders = useOrdersStore()
</script>

<template>
  <UCard>
    <template #header>
      <div class="flex items-center gap-2">
        <UIcon name="i-lucide-radio" class="text-(--ui-primary)" />
        <h2 class="font-semibold">
          Eventos em tempo real (SignalR)
        </h2>
      </div>
    </template>
    <p v-if="orders.events.length === 0" class="text-sm text-(--ui-text-muted)">
      Faça um pedido e acompanhe aqui cada transição publicada pela saga.
    </p>
    <ul v-else class="max-h-72 space-y-2 overflow-y-auto text-sm">
      <li v-for="event in orders.events" :key="`${event.orderId}-${event.status}-${event.receivedAt}`" class="flex items-center gap-2">
        <code class="text-xs text-(--ui-text-muted)">{{ event.orderId.slice(0, 8) }}</code>
        <OrderStatusBadge :status="event.status" />
        <span class="ms-auto text-xs text-(--ui-text-muted)">{{ formatDateTime(event.receivedAt) }}</span>
      </li>
    </ul>
  </UCard>
</template>
