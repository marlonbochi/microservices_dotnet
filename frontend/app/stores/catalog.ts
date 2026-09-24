import { defineStore } from 'pinia'
import type { CreateProductRequest, Product, StockItem, UpdateProductRequest } from '~/types/api'

/** A catalog product joined with its stock (composition done in the UI: two services, one screen). */
export interface ProductWithStock extends Product {
  stock: StockItem | null
}

const STOCK_SYNC_ATTEMPTS = 10
const STOCK_SYNC_INTERVAL_MS = 500

export const useCatalogStore = defineStore('catalog', () => {
  const api = useApi()
  const products = ref<Product[]>([])
  const stock = ref<StockItem[]>([])
  const loading = ref(false)

  const productsWithStock = computed<ProductWithStock[]>(() => {
    const stockById = new Map(stock.value.map(item => [item.productId, item]))
    return products.value.map(product => ({ ...product, stock: stockById.get(product.id) ?? null }))
  })

  async function load() {
    loading.value = true
    try {
      ;[products.value, stock.value] = await Promise.all([api.products.list(), api.stock.list()])
    }
    finally {
      loading.value = false
    }
  }

  async function refreshStock() {
    stock.value = await api.stock.list()
  }

  /**
   * The stock record is created asynchronously by Inventory when it consumes ProductCreated,
   * so right after creating a product we poll until it shows up (eventual consistency!).
   */
  async function waitForStock(productId: string) {
    for (let attempt = 0; attempt < STOCK_SYNC_ATTEMPTS; attempt++) {
      await refreshStock()
      if (stock.value.some(item => item.productId === productId)) {
        return
      }
      await new Promise(resolve => setTimeout(resolve, STOCK_SYNC_INTERVAL_MS))
    }
  }

  async function create(request: CreateProductRequest) {
    const product = await api.products.create(request)
    products.value = [...products.value, product].sort((a, b) => a.name.localeCompare(b.name))
    void waitForStock(product.id)
    return product
  }

  async function update(id: string, request: UpdateProductRequest) {
    const updated = await api.products.update(id, request)
    products.value = products.value.map(product => (product.id === id ? updated : product))
    return updated
  }

  async function restock(productId: string, quantity: number) {
    const updated = await api.stock.restock(productId, quantity)
    stock.value = stock.value.map(item => (item.productId === productId ? updated : item))
  }

  return { products, stock, loading, productsWithStock, load, refreshStock, create, update, restock }
})
