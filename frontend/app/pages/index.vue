<script setup lang="ts">
const steps = [
  { icon: 'i-lucide-package-plus', title: '1. Add products', text: 'Catalog stores the product and publishes ProductCreated. Inventory consumes the event and creates the stock record.', to: '/products' },
  { icon: 'i-lucide-shopping-cart', title: '2. Place orders', text: 'Ordering reads prices from Catalog (HTTP) and starts the saga: reserve stock → charge payment → confirm.', to: '/orders' },
  { icon: 'i-lucide-network', title: '3. Watch it all', text: 'See the queues in RabbitMQ and the distributed trace of every order in the Aspire Dashboard.', to: '/architecture' },
]
</script>

<template>
  <div class="space-y-6">
    <div>
      <h1 class="text-2xl font-bold">
        Microservices learning store
      </h1>
      <p class="text-(--ui-text-muted)">
        A Nuxt frontend talking to 4 .NET microservices through an API gateway, integrated by messaging.
      </p>
    </div>
    <div class="grid gap-4 md:grid-cols-3">
      <UCard v-for="step in steps" :key="step.title">
        <div class="space-y-2">
          <UIcon :name="step.icon" class="size-8 text-(--ui-primary)" />
          <h2 class="font-semibold">
            {{ step.title }}
          </h2>
          <p class="text-sm text-(--ui-text-muted)">
            {{ step.text }}
          </p>
          <UButton :to="step.to" variant="soft" trailing-icon="i-lucide-arrow-right">
            Open
          </UButton>
        </div>
      </UCard>
    </div>
  </div>
</template>
