<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import type { ProductWithStock } from '~/stores/catalog'

const catalog = useCatalogStore()
const toast = useToast()

const formOpen = ref(false)
const restockOpen = ref(false)
const selected = ref<ProductWithStock | null>(null)

const columns: TableColumn<ProductWithStock>[] = [
  { accessorKey: 'sku', header: 'SKU' },
  { accessorKey: 'name', header: 'Produto' },
  { accessorKey: 'price', header: 'Preço' },
  { id: 'stock', header: 'Estoque (Inventory)' },
  { id: 'actions', header: '' },
]

onMounted(async () => {
  try {
    await catalog.load()
  }
  catch (error) {
    toast.add({ title: 'Não foi possível carregar', description: describeProblem(problemOf(error), 'Gateway indisponível?'), color: 'error' })
  }
})

function openCreate() {
  selected.value = null
  formOpen.value = true
}

function openEdit(product: ProductWithStock) {
  selected.value = product
  formOpen.value = true
}

function openRestock(product: ProductWithStock) {
  selected.value = product
  restockOpen.value = true
}
</script>

<template>
  <div class="space-y-4">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold">
          Produtos
        </h1>
        <p class="text-sm text-(--ui-text-muted)">
          Dados do produto vêm do <b>Catalog</b>; quantidades vêm do <b>Inventory</b>. Duas APIs, uma tela.
        </p>
      </div>
      <div class="flex gap-2">
        <UButton color="neutral" variant="outline" icon="i-lucide-refresh-cw" :loading="catalog.loading" @click="catalog.load()">
          Atualizar
        </UButton>
        <UButton icon="i-lucide-plus" @click="openCreate">
          Novo produto
        </UButton>
      </div>
    </div>

    <UCard>
      <UTable :data="catalog.productsWithStock" :columns="columns" :loading="catalog.loading" empty="Nenhum produto cadastrado.">
        <template #name-cell="{ row }">
          <div>
            <p class="font-medium">
              {{ row.original.name }}
            </p>
            <p class="text-xs text-(--ui-text-muted)">
              {{ row.original.description }}
            </p>
          </div>
        </template>
        <template #price-cell="{ row }">
          {{ formatCurrency(row.original.price) }}
        </template>
        <template #stock-cell="{ row }">
          <div v-if="row.original.stock" class="flex flex-wrap gap-1">
            <UBadge :color="row.original.stock.available > 0 ? 'success' : 'error'" variant="subtle">
              Disponível: {{ row.original.stock.available }}
            </UBadge>
            <UBadge v-if="row.original.stock.quantityReserved > 0" color="warning" variant="subtle">
              Reservado: {{ row.original.stock.quantityReserved }}
            </UBadge>
            <UBadge color="neutral" variant="outline">
              Físico: {{ row.original.stock.quantityOnHand }}
            </UBadge>
          </div>
          <UBadge v-else color="neutral" variant="subtle" icon="i-lucide-loader-circle">
            Sincronizando via evento...
          </UBadge>
        </template>
        <template #actions-cell="{ row }">
          <div class="flex justify-end gap-1">
            <UButton size="sm" color="neutral" variant="ghost" icon="i-lucide-pencil" aria-label="Editar" @click="openEdit(row.original)" />
            <UButton size="sm" variant="soft" icon="i-lucide-package-plus" :disabled="!row.original.stock" @click="openRestock(row.original)">
              Repor
            </UButton>
          </div>
        </template>
      </UTable>
    </UCard>

    <ProductFormModal v-model:open="formOpen" :product="selected" />
    <RestockModal v-model:open="restockOpen" :product="selected" />
  </div>
</template>
