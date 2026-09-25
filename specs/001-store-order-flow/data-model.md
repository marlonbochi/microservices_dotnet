# Data Model: Store Order Flow

Each bounded context owns its own database. IDs are `Guid` (v7, time-ordered) generated in the
domain. Money is `decimal(18,2)` in USD.

## Catalog (`CatalogDb`)

### Product (aggregate root)

| Field | Type | Rules |
|-------|------|-------|
| Id | Guid | PK |
| Sku | string(50) | required, unique, `^[A-Za-z0-9-]+$`, stored upper-case |
| Name | string(200) | required, trimmed |
| Description | string(1000)? | optional |
| Price | decimal(18,2) | > 0 |
| CreatedAt / UpdatedAt | DateTimeOffset | set by domain |

Behaviour: `Product.Create(sku, name, description, price)`, `Update(name, description, price)`.
Initial stock is **not** a Catalog concern: it is only forwarded in the `ProductCreated` event.

## Inventory (`InventoryDb`)

### StockItem (aggregate root)

| Field | Type | Rules |
|-------|------|-------|
| ProductId | Guid | PK (same id as Catalog product) |
| Sku | string(50) | replicated from `ProductCreated` |
| ProductName | string(200) | replicated from `ProductCreated` / `ProductUpdated` |
| QuantityOnHand | int | ≥ 0 |
| QuantityReserved | int | ≥ 0, ≤ QuantityOnHand |
| Available | int (computed) | `QuantityOnHand − QuantityReserved` (not stored) |
| RowVersion | rowversion | optimistic concurrency token |

Behaviour: `Reserve(qty)` (fails if `qty > Available`), `ReleaseReservation(qty)`,
`CommitReservation(qty)` (on hand and reserved decrease), `Restock(qty > 0)`.

### StockReservation (aggregate root)

| Field | Type | Rules |
|-------|------|-------|
| OrderId | Guid | PK (one reservation per order → idempotency) |
| Status | enum | `Reserved`, `Committed`, `Released` |
| Lines | owned collection | (ProductId, Quantity > 0) |
| CreatedAt | DateTimeOffset | |

State transitions: `Reserved → Committed` (payment approved) | `Reserved → Released` (payment
declined). Any other transition is ignored (idempotent) or rejected.

## Ordering (`OrderingDb`)

### Order (aggregate root)

| Field | Type | Rules |
|-------|------|-------|
| Id | Guid | PK |
| CustomerName | string(200) | required |
| CustomerEmail | string(254) | valid e-mail |
| Status | enum | see below |
| FailureReason | string(500)? | set for Rejected/Cancelled |
| Total | decimal(18,2) | Σ line totals, computed at creation |
| SimulatePaymentFailure | bool | demo flag |
| CreatedAt | DateTimeOffset | |
| Items | owned collection `OrderItem` | ≥ 1 |
| History | owned collection `OrderStatusChange` | append-only timeline |

**OrderItem**: ProductId, ProductName, UnitPrice (> 0), Quantity (≥ 1), LineTotal (computed).
Duplicate product ids in a request are merged by summing quantities.

**OrderStatusChange**: Status, OccurredAt, Note?.

**Order status state machine**:

```text
Submitted ──▶ StockReserved ──▶ PaymentApproved ──▶ Confirmed      (stock committed)
    │               │
    ▼               ▼
 Rejected     PaymentDeclined ──▶ Cancelled                        (stock released)
(no stock)
```

`Order.ChangeStatus(newStatus, note)` validates transitions, appends history, raises
`OrderStatusChangedDomainEvent`. Transitions to the current status are ignored (idempotent).

### OrderState (saga instance – MassTransit)

| Field | Type |
|-------|------|
| CorrelationId | Guid (= OrderId, PK) |
| CurrentState | string(64) |
| Total | decimal(18,2) |
| SimulatePaymentFailure | bool |
| FailureReason | string(500)? |
| RowVersion | rowversion |

Plus MassTransit outbox tables (`InboxState`, `OutboxMessage`, `OutboxState`) in each service DB
that uses the outbox.
