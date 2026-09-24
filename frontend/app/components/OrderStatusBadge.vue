<script setup lang="ts">
import type { OrderStatus } from '~/types/api'

const props = defineProps<{ status: OrderStatus }>()
const presentation = computed(() => orderStatusPresentation[props.status])
</script>

<template>
  <UBadge :color="presentation.color" variant="subtle" :icon="presentation.icon">
    {{ presentation.label }}
  </UBadge>
</template>
