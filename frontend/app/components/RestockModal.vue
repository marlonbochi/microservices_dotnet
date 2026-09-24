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
    toast.add({ title: 'Estoque atualizado', color: 'success' })
    open.value = false
  }
  catch (error) {
    toast.add({ title: 'Falha ao repor estoque', description: describeProblem(problemOf(error)), color: 'error' })
  }
  finally {
    saving.value = false
  }
}
</script>

<template>
  <UModal v-model:open="open" :title="`Repor estoque: ${product?.name ?? ''}`" description="Chamada síncrona ao Inventory (via gateway).">
    <template #body>
      <div class="flex items-end gap-3">
        <UFormField label="Quantidade" class="flex-1">
          <UInputNumber v-model="quantity" :min="1" :max="100000" class="w-full" />
        </UFormField>
        <UButton :loading="saving" icon="i-lucide-plus" @click="submit">
          Adicionar
        </UButton>
      </div>
    </template>
  </UModal>
</template>
