import type {
  CreateProductRequest,
  Order,
  PlaceOrderRequest,
  Product,
  ProblemDetails,
  StockItem,
  UpdateProductRequest,
} from '~/types/api'

/**
 * Typed client for the API Gateway. The frontend only knows the gateway, never the individual
 * services: routing to Catalog, Inventory or Ordering is the gateway's job.
 */
export function useApi() {
  const { apiBase } = useRuntimeConfig().public
  const request = $fetch.create({ baseURL: apiBase })

  return {
    products: {
      list: (search?: string) => request<Product[]>('/api/catalog/products', { query: { search } }),
      create: (body: CreateProductRequest) => request<Product>('/api/catalog/products', { method: 'POST', body }),
      update: (id: string, body: UpdateProductRequest) =>
        request<Product>(`/api/catalog/products/${id}`, { method: 'PUT', body }),
    },
    stock: {
      list: () => request<StockItem[]>('/api/inventory/stock'),
      restock: (productId: string, quantity: number) =>
        request<StockItem>(`/api/inventory/stock/${productId}/restock`, { method: 'POST', body: { quantity } }),
    },
    orders: {
      list: () => request<Order[]>('/api/ordering/orders'),
      get: (id: string) => request<Order>(`/api/ordering/orders/${id}`),
      place: (body: PlaceOrderRequest) => request<Order>('/api/ordering/orders', { method: 'POST', body }),
    },
  }
}

/** Extracts Problem Details from a $fetch error. */
export function problemOf(error: unknown): ProblemDetails | undefined {
  return (error as { data?: ProblemDetails })?.data
}
