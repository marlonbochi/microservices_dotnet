// Contracts exposed by the API Gateway (see specs/001-store-order-flow/contracts/http-api.md).

export interface Product {
  id: string
  sku: string
  name: string
  description: string | null
  price: number
  createdAt: string
  updatedAt: string
}

export interface CreateProductRequest {
  sku: string
  name: string
  description?: string
  price: number
  initialStock: number
}

export interface UpdateProductRequest {
  name: string
  description?: string
  price: number
}

export interface StockItem {
  productId: string
  sku: string
  productName: string
  quantityOnHand: number
  quantityReserved: number
  available: number
}

export type OrderStatus =
  | 'Submitted'
  | 'StockReserved'
  | 'PaymentApproved'
  | 'PaymentDeclined'
  | 'Confirmed'
  | 'Rejected'
  | 'Cancelled'

export interface OrderItem {
  productId: string
  productName: string
  unitPrice: number
  quantity: number
  lineTotal: number
}

export interface OrderStatusChange {
  status: OrderStatus
  occurredAt: string
  note: string | null
}

export interface Order {
  id: string
  customerName: string
  customerEmail: string
  status: OrderStatus
  failureReason: string | null
  total: number
  simulatePaymentFailure: boolean
  createdAt: string
  items: OrderItem[]
  history: OrderStatusChange[]
}

export interface PlaceOrderRequest {
  customerName: string
  customerEmail: string
  items: { productId: string, quantity: number }[]
  simulatePaymentFailure: boolean
}

/** Payload pushed by the Ordering SignalR hub. */
export interface OrderStatusNotification {
  orderId: string
  status: OrderStatus
  failureReason: string | null
  occurredAt: string
}

/** RFC 9457 Problem Details returned by every service. */
export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  errors?: Record<string, string[]>
}
