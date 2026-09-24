import { defineStore } from 'pinia'
import type { Order, OrderStatusNotification, PlaceOrderRequest } from '~/types/api'

export interface HubEvent extends OrderStatusNotification {
  receivedAt: string
}

const MAX_EVENTS = 50

export const useOrdersStore = defineStore('orders', () => {
  const api = useApi()
  const orders = ref<Order[]>([])
  const events = ref<HubEvent[]>([])
  const loading = ref(false)

  async function load() {
    loading.value = true
    try {
      orders.value = await api.orders.list()
    }
    finally {
      loading.value = false
    }
  }

  async function place(request: PlaceOrderRequest) {
    const order = await api.orders.place(request)
    upsert(order)
    return order
  }

  /** Called for every SignalR push: log it and refresh that order (timeline included). */
  async function applyNotification(notification: OrderStatusNotification) {
    events.value = [{ ...notification, receivedAt: new Date().toISOString() }, ...events.value].slice(0, MAX_EVENTS)
    upsert(withNotification(await api.orders.get(notification.orderId), notification))
  }

  /**
   * The push can arrive a few milliseconds before the saga transaction commits, so the order we just
   * fetched may not contain the new status yet. The notification itself is the source of truth here.
   */
  function withNotification(order: Order, notification: OrderStatusNotification): Order {
    if (order.history.some(change => change.status === notification.status)) {
      return order
    }

    return {
      ...order,
      status: notification.status,
      failureReason: notification.failureReason ?? order.failureReason,
      history: [...order.history, { status: notification.status, occurredAt: notification.occurredAt, note: notification.failureReason }],
    }
  }

  function upsert(order: Order) {
    const others = orders.value.filter(existing => existing.id !== order.id)
    orders.value = [order, ...others].sort((a, b) => b.createdAt.localeCompare(a.createdAt))
  }

  return { orders, events, loading, load, place, applyNotification }
})
