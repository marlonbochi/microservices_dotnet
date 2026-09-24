<script setup lang="ts">
const steps = [
  { icon: 'i-lucide-package-plus', title: '1. Cadastre produtos', text: 'O Catalog salva o produto e publica ProductCreated. O Inventory consome o evento e cria o estoque.', to: '/products' },
  { icon: 'i-lucide-shopping-cart', title: '2. Faça pedidos', text: 'O Ordering valida preços no Catalog (HTTP) e inicia a saga: reserva de estoque → pagamento → confirmação.', to: '/orders' },
  { icon: 'i-lucide-network', title: '3. Observe tudo', text: 'Veja filas no RabbitMQ e o trace distribuído de cada pedido no Aspire Dashboard.', to: '/architecture' },
]
</script>

<template>
  <div class="space-y-6">
    <div>
      <h1 class="text-2xl font-bold">
        Loja de estudo de microsserviços
      </h1>
      <p class="text-(--ui-text-muted)">
        Frontend Nuxt conversando com 4 microsserviços .NET através de um API Gateway, integrados por mensageria.
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
            Abrir
          </UButton>
        </div>
      </UCard>
    </div>
  </div>
</template>
