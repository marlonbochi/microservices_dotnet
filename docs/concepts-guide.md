# Concepts guide: where each idea lives in the code

Use this as a study path: read the concept, then open the file.

## Microservices fundamentals

| Concept | What it is | Where to look |
|---------|------------|---------------|
| Bounded context | Each service models one part of the business with its own language | `src/Services/*` |
| Database per service | No service touches another service's database | `CatalogDbContext`, `InventoryDbContext`, `OrderingDbContext` (schemas `catalog`, `inventory`, `ordering`) |
| API Gateway | Single entry point: routing, CORS, WebSockets | [`Store.Gateway/appsettings.json`](../src/Gateway/Store.Gateway/appsettings.json) |
| Shared contracts | Messages act as a versionable, asynchronous "public API" | [`Store.Contracts`](../src/BuildingBlocks/Store.Contracts) |
| Data replication | Inventory keeps its own copy of the product name/SKU | [`ProductCreatedConsumer`](../src/Services/Inventory/Inventory.Infrastructure/Messaging/Consumers/CatalogConsumers.cs) |
| UI composition | One screen combines data from two services | [`frontend/app/stores/catalog.ts`](../frontend/app/stores/catalog.ts) (`productsWithStock`) |

## Messaging

| Concept | Where to look |
|---------|---------------|
| Publishing an event through an Application port | [`CreateProductHandler`](../src/Services/Catalog/Catalog.Application/Products/Create/CreateProductHandler.cs) → `IIntegrationEventPublisher` |
| MassTransit adapter | [`OutboxEventPublisher`](../src/Services/Catalog/Catalog.Infrastructure/Messaging/OutboxEventPublisher.cs) |
| Thin consumer (only translates message → command) | [`StockConsumers.cs`](../src/Services/Inventory/Inventory.Infrastructure/Messaging/Consumers/StockConsumers.cs) |
| Saga / state machine | [`OrderStateMachine`](../src/Services/Ordering/Ordering.Infrastructure/Messaging/Sagas/OrderStateMachine.cs) |
| Compensation | `PaymentDeclined → ReleaseStock` in the saga + [`ReleaseStockHandler`](../src/Services/Inventory/Inventory.Application/Reservations/SettleReservation.cs) |
| Business idempotency | `StockReservation` keyed by `OrderId`; [`ReserveStockHandler`](../src/Services/Inventory/Inventory.Application/Reservations/ReserveStock.cs) recognises duplicates |
| Outbox / Inbox | Each Infrastructure `DependencyInjection.cs`; tables in the migrations |
| Retry + dead-letter | [`MessagingExtensions`](../src/BuildingBlocks/Store.ServiceDefaults/Messaging/MessagingExtensions.cs) |

## Domain (tactical DDD)

| Concept | Where to look |
|---------|---------------|
| Aggregate with invariants | [`StockItem`](../src/Services/Inventory/Inventory.Domain/Stock/StockItem.cs): reserved never exceeds on-hand |
| Value object | [`Sku`](../src/Services/Catalog/Catalog.Domain/Products/Sku.cs) |
| State machine in the domain | [`Order.AllowedTransitions`](../src/Services/Ordering/Ordering.Domain/Orders/Order.cs) |
| Domain event | `OrderStatusChangedDomainEvent` → dispatched in `OrderingDbContext.SaveChangesAsync` |
| Result pattern (expected failure ≠ exception) | [`Result`/`Error`](../src/BuildingBlocks/Store.SharedKernel) → mapped to Problem Details in [`ResultExtensions`](../src/BuildingBlocks/Store.ServiceDefaults/Http/ResultExtensions.cs) |
| Snapshot of external data | `OrderItem` copies name and price from Catalog at order time |

## Resilience and consistency

| Concept | Where to look |
|---------|---------------|
| HTTP circuit breaker / retry / timeout | `AddStandardResilienceHandler()` in [`Ordering.Infrastructure/DependencyInjection.cs`](../src/Services/Ordering/Ordering.Infrastructure/DependencyInjection.cs) |
| Fail fast with a friendly message | [`CatalogHttpClient`](../src/Services/Ordering/Ordering.Infrastructure/Catalog/CatalogHttpClient.cs) → 503 |
| Optimistic concurrency | `rowversion` in [`StockItemConfiguration`](../src/Services/Inventory/Inventory.Infrastructure/Persistence/Configurations/StockItemConfiguration.cs) |
| All-or-nothing | `ReserveStockHandler.TryReserveAllAsync` validates every line before reserving |
| Visible eventual consistency | "Syncing via event..." badge in `frontend/app/pages/products.vue` |

## Operations

| Concept | Where to look |
|---------|---------------|
| Health checks (liveness/readiness) | [`ServiceDefaultsExtensions`](../src/BuildingBlocks/Store.ServiceDefaults/ServiceDefaultsExtensions.cs) |
| Distributed tracing | Same file; `MassTransit` source + instrumentations; view it in the Aspire Dashboard |
| Problem Details (RFC 9457) | `AddProblemDetails` + `GlobalExceptionHandler` |
| Per-environment configuration | `appsettings.json` + environment variables in `docker-compose.yml` |
| Automatic migrations (dev only) | [`DatabaseMigrationExtensions`](../src/BuildingBlocks/Store.ServiceDefaults/DatabaseMigrationExtensions.cs) |

## Suggested exercises

1. Stop Payment (`docker compose stop payment-worker`), place an order and watch it wait at
   "Stock reserved". Start the service again and watch the order continue on its own.
2. Stop Catalog and try to place an order: note the 503 and the circuit breaker in the logs.
3. Create a product with 1 unit and place two orders almost at the same time: one is confirmed, the
   other rejected.
4. Add a payment timeout to the saga (`Schedule`) and a `PaymentTimedOut` state.
5. Add a **Notification** service that consumes `OrderSubmitted` and "sends e-mails" (writes a log). No
   existing service has to change: that is Open/Closed at the architecture level.
