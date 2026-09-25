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
  { accessorKey: 'name', header: 'Product' },
  { accessorKey: 'price', header: 'Price' },
  { id: 'stock', header: 'Stock (Inventory)' },
  { id: 'actions', header: '' },
]

onMounted(async () => {
  try {
    await catalog.load()
  }
  catch (error) {
    toast.add({ title: 'Could not load', description: describeProblem(problemOf(error), 'Is the gateway down?'), color: 'error' })
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
          Products
        </h1>
        <p class="text-sm text-(--ui-text-muted)">
          Product data comes from <b>Catalog</b>; quantities come from <b>Inventory</b>. Two APIs, one screen.
        </p>
      </div>
      <div class="flex gap-2">
        <UButton color="neutral" variant="outline" icon="i-lucide-refresh-cw" :loading="catalog.loading" @click="catalog.load()">
          Refresh
        </UButton>
        <UButton icon="i-lucide-plus" @click="openCreate">
          New product
        </UButton>
      </div>
    </div>

    <UCard>
      <UTable :data="catalog.productsWithStock" :columns="columns" :loading="catalog.loading" empty="No products yet.">
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
              Available: {{ row.original.stock.available }}
            </UBadge>
            <UBadge v-if="row.original.stock.quantityReserved > 0" color="warning" variant="subtle">
              Reserved: {{ row.original.stock.quantityReserved }}
            </UBadge>
            <UBadge color="neutral" variant="outline">
              On hand: {{ row.original.stock.quantityOnHand }}
            </UBadge>
          </div>
          <UBadge v-else color="neutral" variant="subtle" icon="i-lucide-loader-circle">
            Syncing via event...
          </UBadge>
        </template>
        <template #actions-cell="{ row }">
          <div class="flex justify-end gap-1">
            <UButton size="sm" color="neutral" variant="ghost" icon="i-lucide-pencil" aria-label="Edit" @click="openEdit(row.original)" />
            <UButton size="sm" variant="soft" icon="i-lucide-package-plus" :disabled="!row.original.stock" @click="openRestock(row.original)">
              Restock
            </UButton>
          </div>
        </template>
      </UTable>
    </UCard>

    <ProductFormModal v-model:open="formOpen" :product="selected" />
    <RestockModal v-model:open="restockOpen" :product="selected" />
  </div>
</template>
