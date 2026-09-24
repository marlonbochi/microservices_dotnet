# HTTP API Contracts (through the Gateway `http://localhost:5100`)

All errors use RFC 9457 Problem Details (`application/problem+json`); validation errors add an
`errors` dictionary (`field → messages[]`). Each service also serves its OpenAPI document at
`/openapi/v1.json` and Scalar UI at `/scalar` (development).

## Catalog — `/api/catalog`

| Method | Path | Body / Query | Success | Errors |
|--------|------|--------------|---------|--------|
| GET | `/products` | `?search=` | 200 `ProductResponse[]` | |
| GET | `/products/{id}` | | 200 `ProductResponse` | 404 |
| GET | `/products/batch` | `?ids=guid&ids=guid` | 200 `ProductResponse[]` | 400 |
| POST | `/products` | `CreateProductRequest` | 201 `ProductResponse` + `Location` | 400, 409 (SKU exists) |
| PUT | `/products/{id}` | `UpdateProductRequest` | 200 `ProductResponse` | 400, 404 |

```jsonc
// CreateProductRequest
{ "sku": "KB-001", "name": "Teclado Mecânico", "description": "ABNT2", "price": 350.00, "initialStock": 10 }
// UpdateProductRequest
{ "name": "Teclado Mecânico RGB", "description": "ABNT2", "price": 399.90 }
// ProductResponse
{ "id": "guid", "sku": "KB-001", "name": "...", "description": "...", "price": 350.00,
  "createdAt": "2026-09-24T12:00:00Z", "updatedAt": "2026-09-24T12:00:00Z" }
```

## Inventory — `/api/inventory`

| Method | Path | Body | Success | Errors |
|--------|------|------|---------|--------|
| GET | `/stock` | | 200 `StockItemResponse[]` | |
| GET | `/stock/{productId}` | | 200 `StockItemResponse` | 404 |
| POST | `/stock/{productId}/restock` | `{ "quantity": 5 }` | 200 `StockItemResponse` | 400, 404 |

```jsonc
// StockItemResponse
{ "productId": "guid", "sku": "KB-001", "productName": "...", "quantityOnHand": 10,
  "quantityReserved": 2, "available": 8 }
```

## Ordering — `/api/ordering`

| Method | Path | Body | Success | Errors |
|--------|------|------|---------|--------|
| POST | `/orders` | `PlaceOrderRequest` | 202 `OrderResponse` + `Location` | 400, 422 (unknown product), 503 (catalog unavailable) |
| GET | `/orders` | `?take=50` | 200 `OrderSummaryResponse[]` (newest first) | |
| GET | `/orders/{id}` | | 200 `OrderResponse` | 404 |

```jsonc
// PlaceOrderRequest
{ "customerName": "Ana", "customerEmail": "ana@example.com",
  "items": [ { "productId": "guid", "quantity": 2 } ], "simulatePaymentFailure": false }
// OrderResponse
{ "id": "guid", "customerName": "Ana", "customerEmail": "ana@example.com", "status": "Confirmed",
  "failureReason": null, "total": 700.00, "createdAt": "...",
  "items": [ { "productId": "guid", "productName": "...", "unitPrice": 350.00, "quantity": 2, "lineTotal": 700.00 } ],
  "history": [ { "status": "Submitted", "occurredAt": "...", "note": null } ] }
```

## Real-time — SignalR hub `/hubs/orders`

| Direction | Method | Payload |
|-----------|--------|---------|
| server → client | `OrderStatusChanged` | `{ orderId, status, failureReason, occurredAt }` |

## Health (per service, not routed by the gateway)

`GET /health/live` → 200 when the process is up. `GET /health/ready` → 200 when DB and broker are
reachable, 503 otherwise.
