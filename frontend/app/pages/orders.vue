<script setup lang="ts">
const catalog = useCatalogStore()
const orders = useOrdersStore()
const hub = useOrderHub()
const toast = useToast()
const expanded = ref<string | null>(null)

// Every saga transition is pushed by the Ordering service; stock changes with it, so refresh both.
hub.onStatusChanged(async (notification) => {
  await orders.applyNotification(notification)
  await catalog.refreshStock()
})

onMounted(async () => {
  try {
    await Promise.all([catalog.load(), orders.load()])
  }
  catch (error) {
    toast.add({ title: 'Não foi possível carregar', description: describeProblem(problemOf(error), 'Gateway indisponível?'), color: 'error' })
  }
})

function toggle(orderId: string) {
  expanded.value = expanded.value === orderId ? null : orderId
}
</script>

<template>
  <div class="space-y-4">
    <div>
      <h1 class="text-2xl font-bold">
        Pedidos
      </h1>
      <p class="text-sm text-(--ui-text-muted)">
        O pedido é aceito na hora (202) e processado de forma assíncrona: Ordering → Inventory → Payment → Inventory.
      </p>
    </div>

    <div class="grid gap-4 lg:grid-cols-5">
      <div class="space-y-4 lg:col-span-2">
        <NewOrderForm />
        <HubEventLog />
      </div>

      <UCard class="lg:col-span-3">
        <template #header>
          <div class="flex items-center justify-between">
            <h2 class="font-semibold">
              Pedidos recentes
            </h2>
            <UButton size="sm" color="neutral" variant="ghost" icon="i-lucide-refresh-cw" :loading="orders.loading" @click="orders.load()" />
          </div>
        </template>

        <p v-if="orders.orders.length === 0" class="text-sm text-(--ui-text-muted)">
          Nenhum pedido ainda.
        </p>
        <ul class="divide-y divide-(--ui-border)">
          <li v-for="order in orders.orders" :key="order.id" class="py-3">
            <button type="button" class="flex w-full items-center gap-3 text-left" @click="toggle(order.id)">
              <UIcon :name="expanded === order.id ? 'i-lucide-chevron-down' : 'i-lucide-chevron-right'" />
              <div class="flex-1">
                <p class="font-medium">
                  {{ order.items.map(item => `${item.quantity}× ${item.productName}`).join(', ') }}
                </p>
                <p class="text-xs text-(--ui-text-muted)">
                  #{{ order.id.slice(0, 8) }} · {{ order.customerName }} · {{ formatDateTime(order.createdAt) }}
                  <span v-if="order.simulatePaymentFailure"> · falha simulada</span>
                </p>
              </div>
              <span class="text-sm font-medium">{{ formatCurrency(order.total) }}</span>
              <OrderStatusBadge :status="order.status" />
            </button>
            <div v-if="expanded === order.id" class="mt-3 grid gap-4 ps-7 sm:grid-cols-2">
              <OrderTimeline :history="order.history" />
              <div class="space-y-2 text-sm">
                <UAlert v-if="order.failureReason" color="error" variant="subtle" icon="i-lucide-triangle-alert" :description="order.failureReason" />
                <div v-for="item in order.items" :key="item.productId" class="flex justify-between">
                  <span>{{ item.quantity }}× {{ item.productName }}</span>
                  <span>{{ formatCurrency(item.lineTotal) }}</span>
                </div>
              </div>
            </div>
          </li>
        </ul>
      </UCard>
    </div>
  </div>
</template>
