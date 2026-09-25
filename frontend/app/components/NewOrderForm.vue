<script setup lang="ts">
const catalog = useCatalogStore()
const orders = useOrdersStore()
const toast = useToast()

interface Line { productId: string | undefined, quantity: number }

const customerName = ref('Test Customer')
const customerEmail = ref('customer@example.com')
const simulatePaymentFailure = ref(false)
const lines = ref<Line[]>([{ productId: undefined, quantity: 1 }])
const submitting = ref(false)

const productOptions = computed(() => catalog.productsWithStock.map(product => ({
  label: `${product.name} — ${formatCurrency(product.price)} (avail. ${product.stock?.available ?? '?'})`,
  value: product.id,
})))

const estimatedTotal = computed(() => lines.value.reduce((total, line) => {
  const product = catalog.products.find(candidate => candidate.id === line.productId)
  return total + (product ? product.price * line.quantity : 0)
}, 0))

const canSubmit = computed(() => lines.value.some(line => line.productId) && customerName.value && customerEmail.value)

function addLine() {
  lines.value.push({ productId: undefined, quantity: 1 })
}

function removeLine(index: number) {
  lines.value.splice(index, 1)
}

async function submit() {
  submitting.value = true
  try {
    const order = await orders.place({
      customerName: customerName.value,
      customerEmail: customerEmail.value,
      simulatePaymentFailure: simulatePaymentFailure.value,
      items: lines.value
        .filter((line): line is { productId: string, quantity: number } => !!line.productId)
        .map(line => ({ productId: line.productId, quantity: line.quantity })),
    })
    toast.add({ title: 'Order sent (202 Accepted)', description: `Order ${order.id.slice(0, 8)} is being processed by the saga.`, color: 'info' })
    lines.value = [{ productId: undefined, quantity: 1 }]
  }
  catch (error) {
    toast.add({ title: 'Order not accepted', description: describeProblem(problemOf(error)), color: 'error' })
  }
  finally {
    submitting.value = false
  }
}
</script>

<template>
  <UCard>
    <template #header>
      <h2 class="font-semibold">
        New order
      </h2>
    </template>
    <div class="space-y-4">
      <div class="grid gap-3 sm:grid-cols-2">
        <UFormField label="Customer">
          <UInput v-model="customerName" class="w-full" />
        </UFormField>
        <UFormField label="E-mail">
          <UInput v-model="customerEmail" type="email" class="w-full" />
        </UFormField>
      </div>

      <div class="space-y-2">
        <div v-for="(line, index) in lines" :key="index" class="flex items-end gap-2">
          <UFormField :label="index === 0 ? 'Product' : undefined" class="flex-1">
            <USelectMenu v-model="line.productId" :items="productOptions" value-key="value" placeholder="Select..." class="w-full" />
          </UFormField>
          <UFormField :label="index === 0 ? 'Qty' : undefined" class="w-28">
            <UInputNumber v-model="line.quantity" :min="1" :max="1000" />
          </UFormField>
          <UButton color="neutral" variant="ghost" icon="i-lucide-trash-2" :disabled="lines.length === 1" aria-label="Remove" @click="removeLine(index)" />
        </div>
        <UButton size="sm" variant="link" icon="i-lucide-plus" @click="addLine">
          Add item
        </UButton>
      </div>

      <USwitch v-model="simulatePaymentFailure" label="Simulate payment failure" description="Forces PaymentDeclined → the saga compensates by releasing the stock." />

      <div class="flex items-center justify-between border-t border-(--ui-border) pt-4">
        <span class="text-sm text-(--ui-text-muted)">Estimated total: <b class="text-(--ui-text)">{{ formatCurrency(estimatedTotal) }}</b></span>
        <UButton icon="i-lucide-send" :loading="submitting" :disabled="!canSubmit" @click="submit">
          Place order
        </UButton>
      </div>
    </div>
  </UCard>
</template>
