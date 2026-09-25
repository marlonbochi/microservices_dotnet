<script setup lang="ts">
import type { ProductWithStock } from '~/stores/catalog'

const props = defineProps<{ product: ProductWithStock | null }>()
const open = defineModel<boolean>('open', { default: false })

const catalog = useCatalogStore()
const toast = useToast()
const quantity = ref(5)
const saving = ref(false)

async function submit() {
  if (!props.product) {
    return
  }
  saving.value = true
  try {
    await catalog.restock(props.product.id, quantity.value)
    toast.add({ title: 'Stock updated', color: 'success' })
    open.value = false
  }
  catch (error) {
    toast.add({ title: 'Restock failed', description: describeProblem(problemOf(error)), color: 'error' })
  }
  finally {
    saving.value = false
  }
}
</script>

<template>
  <UModal v-model:open="open" :title="`Restock: ${product?.name ?? ''}`" description="Synchronous call to Inventory (through the gateway).">
    <template #body>
      <div class="flex items-end gap-3">
        <UFormField label="Quantity" class="flex-1">
          <UInputNumber v-model="quantity" :min="1" :max="100000" class="w-full" />
        </UFormField>
        <UButton :loading="saving" icon="i-lucide-plus" @click="submit">
          Add
        </UButton>
      </div>
    </template>
  </UModal>
</template>
