import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'
import type { OrderStatusNotification } from '~/types/api'

type ConnectionStatus = 'disconnected' | 'connecting' | 'connected'

let connection: HubConnection | undefined
const status = ref<ConnectionStatus>('disconnected')
const listeners = new Set<(notification: OrderStatusNotification) => void>()

/**
 * Real-time channel to the Ordering service (SignalR, through the gateway).
 * One shared connection for the whole app; components subscribe with onStatusChanged.
 */
export function useOrderHub() {
  const { apiBase } = useRuntimeConfig().public

  async function connect() {
    if (connection && connection.state !== HubConnectionState.Disconnected) {
      return
    }

    connection = new HubConnectionBuilder()
      .withUrl(`${apiBase}/hubs/orders`, { withCredentials: true })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on('OrderStatusChanged', (notification: OrderStatusNotification) =>
      listeners.forEach(listener => listener(notification)))
    connection.onreconnecting(() => { status.value = 'connecting' })
    connection.onreconnected(() => { status.value = 'connected' })
    connection.onclose(() => { status.value = 'disconnected' })

    status.value = 'connecting'
    try {
      await connection.start()
      status.value = 'connected'
    }
    catch {
      status.value = 'disconnected'
    }
  }

  function onStatusChanged(listener: (notification: OrderStatusNotification) => void) {
    listeners.add(listener)
    onScopeDispose(() => listeners.delete(listener))
  }

  return { status: readonly(status), connect, onStatusChanged }
}
