<script setup lang="ts">
const config = useRuntimeConfig().public

const tools = [
  { title: 'RabbitMQ Management', text: 'Exchanges, filas por consumidor e filas _error (dead-letter). Login guest/guest.', icon: 'i-lucide-rabbit', url: config.rabbitMqUrl },
  { title: 'Aspire Dashboard', text: 'Traces distribuídos (um pedido atravessando todos os serviços), logs estruturados e métricas.', icon: 'i-lucide-activity', url: config.dashboardUrl },
  { title: 'Catalog API (Scalar)', text: 'Documentação OpenAPI interativa do serviço de catálogo.', icon: 'i-lucide-book-open', url: 'http://localhost:5101/scalar' },
  { title: 'Inventory API (Scalar)', text: 'Documentação OpenAPI interativa do serviço de estoque.', icon: 'i-lucide-book-open', url: 'http://localhost:5102/scalar' },
  { title: 'Ordering API (Scalar)', text: 'Documentação OpenAPI interativa do serviço de pedidos.', icon: 'i-lucide-book-open', url: 'http://localhost:5103/scalar' },
]

const flow = [
  ['Frontend', 'POST /api/ordering/orders', 'Gateway (YARP)'],
  ['Ordering', 'GET /api/catalog/products/batch (HTTP + resiliência)', 'Catalog'],
  ['Ordering', 'OrderSubmitted (outbox)', 'Saga'],
  ['Saga', 'ReserveStock', 'Inventory'],
  ['Inventory', 'StockReserved | StockReservationFailed', 'Saga'],
  ['Saga', 'ProcessPayment', 'Payment'],
  ['Payment', 'PaymentApproved | PaymentDeclined', 'Saga'],
  ['Saga', 'CommitStock | ReleaseStock (compensação)', 'Inventory'],
  ['Inventory', 'StockCommitted | StockReleased', 'Saga'],
  ['Ordering', 'OrderStatusChanged (SignalR)', 'Frontend'],
]
</script>

<template>
  <div class="space-y-6">
    <div>
      <h1 class="text-2xl font-bold">
        Arquitetura
      </h1>
      <p class="text-sm text-(--ui-text-muted)">
        Ferramentas para enxergar o sistema distribuído por dentro. Veja também a pasta <code>docs/</code> do repositório.
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
            Abrir
          </UButton>
        </div>
      </UCard>
    </div>

    <UCard>
      <template #header>
        <h2 class="font-semibold">
          Fluxo de um pedido
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
