<script setup lang="ts">
import { z } from 'zod'
import type { FormSubmitEvent } from '@nuxt/ui'
import type { ProductWithStock } from '~/stores/catalog'

const props = defineProps<{ product?: ProductWithStock | null }>()
const open = defineModel<boolean>('open', { default: false })

const catalog = useCatalogStore()
const toast = useToast()
const isEdit = computed(() => !!props.product)

// Mirrors the server-side rules (the API validates again: never trust the client).
const schema = z.object({
  sku: z.string().trim().min(1, 'SKU is required').max(50).regex(/^[A-Za-z0-9-]+$/, 'Use letters, digits or dashes'),
  name: z.string().trim().min(1, 'Name is required').max(200),
  description: z.string().max(1000).optional(),
  price: z.number({ error: 'Price is required' }).positive('Price must be greater than zero'),
  initialStock: z.number().int().min(0, 'Stock cannot be negative'),
})
type Schema = z.output<typeof schema>

const state = reactive<Partial<Schema>>({})
const saving = ref(false)

watch(open, (isOpen) => {
  if (!isOpen) {
    return
  }
  Object.assign(state, {
    sku: props.product?.sku ?? '',
    name: props.product?.name ?? '',
    description: props.product?.description ?? '',
    price: props.product?.price ?? undefined,
    initialStock: 10,
  })
})

async function onSubmit(event: FormSubmitEvent<Schema>) {
  saving.value = true
  try {
    const { sku, name, description, price, initialStock } = event.data
    if (props.product) {
      await catalog.update(props.product.id, { name, description, price })
      toast.add({ title: 'Product updated', description: 'ProductUpdated published for Inventory.', color: 'success' })
    }
    else {
      await catalog.create({ sku, name, description, price, initialStock })
      toast.add({ title: 'Product created', description: 'Waiting for Inventory to consume ProductCreated...', color: 'success' })
    }
    open.value = false
  }
  catch (error) {
    toast.add({ title: 'Could not save', description: describeProblem(problemOf(error)), color: 'error' })
  }
  finally {
    saving.value = false
  }
}
</script>

<template>
  <UModal v-model:open="open" :title="isEdit ? 'Edit product' : 'New product'">
    <template #body>
      <UForm :schema="schema" :state="state" class="space-y-4" @submit="onSubmit">
        <UFormField label="SKU" name="sku" required>
          <UInput v-model="state.sku" placeholder="KB-001" :disabled="isEdit" class="w-full" />
        </UFormField>
        <UFormField label="Name" name="name" required>
          <UInput v-model="state.name" placeholder="Mechanical keyboard" class="w-full" />
        </UFormField>
        <UFormField label="Description" name="description">
          <UTextarea v-model="state.description" :rows="2" class="w-full" />
        </UFormField>
        <div class="grid grid-cols-2 gap-4">
          <UFormField label="Price ($)" name="price" required>
            <UInputNumber v-model="state.price" locale="en-US" :min="0" :step="0.01" :format-options="{ minimumFractionDigits: 2 }" class="w-full" />
          </UFormField>
          <UFormField v-if="!isEdit" label="Initial stock" name="initialStock" required>
            <UInputNumber v-model="state.initialStock" :min="0" class="w-full" />
          </UFormField>
        </div>
        <div class="flex justify-end gap-2">
          <UButton color="neutral" variant="ghost" @click="open = false">
            Cancel
          </UButton>
          <UButton type="submit" :loading="saving">
            Save
          </UButton>
        </div>
      </UForm>
    </template>
  </UModal>
</template>
