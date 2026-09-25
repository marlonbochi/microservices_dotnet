<script setup lang="ts">
const config = useRuntimeConfig().public

const tools = [
  { title: 'RabbitMQ Management', text: 'Exchanges, one queue per consumer and _error (dead-letter) queues. Login guest/guest.', icon: 'i-lucide-rabbit', url: config.rabbitMqUrl },
  { title: 'Aspire Dashboard', text: 'Distributed traces (one order crossing every service), structured logs and metrics.', icon: 'i-lucide-activity', url: config.dashboardUrl },
  { title: 'Catalog API (Scalar)', text: 'Interactive OpenAPI docs of the catalog service.', icon: 'i-lucide-book-open', url: 'http://localhost:5101/scalar' },
  { title: 'Inventory API (Scalar)', text: 'Interactive OpenAPI docs of the inventory service.', icon: 'i-lucide-book-open', url: 'http://localhost:5102/scalar' },
  { title: 'Ordering API (Scalar)', text: 'Interactive OpenAPI docs of the ordering service.', icon: 'i-lucide-book-open', url: 'http://localhost:5103/scalar' },
]

const flow = [
  ['Frontend', 'POST /api/ordering/orders', 'Gateway (YARP)'],
  ['Ordering', 'GET /api/catalog/products/batch (HTTP + resilience)', 'Catalog'],
  ['Ordering', 'OrderSubmitted (outbox)', 'Saga'],
  ['Saga', 'ReserveStock', 'Inventory'],
  ['Inventory', 'StockReserved | StockReservationFailed', 'Saga'],
  ['Saga', 'ProcessPayment', 'Payment'],
  ['Payment', 'PaymentApproved | PaymentDeclined', 'Saga'],
  ['Saga', 'CommitStock | ReleaseStock (compensation)', 'Inventory'],
  ['Inventory', 'StockCommitted | StockReleased', 'Saga'],
  ['Ordering', 'OrderStatusChanged (SignalR)', 'Frontend'],
]
</script>

<template>
  <div class="space-y-6">
    <div>
      <h1 class="text-2xl font-bold">
        Architecture
      </h1>
      <p class="text-sm text-(--ui-text-muted)">
        Tools to look inside the distributed system. See also the repository's <code>docs/</code> folder.
      </p>
    </div>

    <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
      <UCard v-for="tool in tools" :key="tool.title">
        <div class="space-y-2">
          <UIcon :name="tool.icon" class="size-6 text-(--ui-primary)" />
          <h2 class="font-semibold">
            {{ tool.title }}
          </h2>
          <p class="text-sm text-(--ui-text-muted)">
            {{ tool.text }}
          </p>
          <UButton :to="tool.url" target="_blank" variant="soft" trailing-icon="i-lucide-external-link">
            Open
          </UButton>
        </div>
      </UCard>
    </div>

    <UCard>
      <template #header>
        <h2 class="font-semibold">
          Life of an order
        </h2>
      </template>
      <ol class="space-y-2 text-sm">
        <li v-for="([from, message, to], index) in flow" :key="index" class="flex flex-wrap items-center gap-2">
          <UBadge color="neutral" variant="outline">
            {{ from }}
          </UBadge>
          <UIcon name="i-lucide-arrow-right" />
          <code class="rounded bg-(--ui-bg-elevated) px-1.5 py-0.5">{{ message }}</code>
          <UIcon name="i-lucide-arrow-right" />
          <UBadge color="primary" variant="subtle">
            {{ to }}
          </UBadge>
        </li>
      </ol>
    </UCard>
  </div>
</template>
