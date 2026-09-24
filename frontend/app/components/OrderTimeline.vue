<script setup lang="ts">
import type { OrderStatusChange } from '~/types/api'

defineProps<{ history: OrderStatusChange[] }>()
</script>

<template>
  <ol class="relative ms-2 border-s border-(--ui-border)">
    <li v-for="(change, index) in history" :key="`${change.status}-${index}`" class="ms-4 pb-3 last:pb-0">
      <span
        class="absolute -start-1.5 mt-1.5 size-3 rounded-full"
        :class="orderStatusPresentation[change.status].final ? 'bg-(--ui-primary)' : 'bg-(--ui-border-accented)'"
      />
      <div class="flex flex-wrap items-center gap-2">
        <OrderStatusBadge :status="change.status" />
        <time class="text-xs text-(--ui-text-muted)">{{ formatDateTime(change.occurredAt) }}</time>
      </div>
      <p v-if="change.note" class="mt-1 text-sm text-(--ui-text-muted)">
        {{ change.note }}
      </p>
    </li>
  </ol>
</template>
