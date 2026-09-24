# Message Contracts (RabbitMQ via MassTransit)

Defined once in `src/BuildingBlocks/Store.Contracts` as immutable C# records (namespace
`Store.Contracts.<Context>`). All messages carry `OrderId`/`ProductId` for correlation.
Delivery is **at-least-once**; consumers are idempotent (inbox + business keys).

## Catalog → Inventory (events)

| Message | Fields | Consumer effect |
|---------|--------|-----------------|
| `ProductCreated` | ProductId, Sku, Name, Price, InitialStock, OccurredAt | Inventory creates `StockItem` (idempotent by ProductId) |
| `ProductUpdated` | ProductId, Name, Price, OccurredAt | Inventory updates replicated `ProductName` |

## Ordering ↔ Saga ↔ Inventory / Payment

| Message | Kind | Producer → Consumer | Fields |
|---------|------|---------------------|--------|
| `OrderSubmitted` | event | Ordering API → OrderStateMachine | OrderId, CustomerEmail, Total, SimulatePaymentFailure, Items[ProductId, Quantity] |
| `ReserveStock` | command (published) | Saga → Inventory | OrderId, Items[ProductId, Quantity] |
| `StockReserved` | event | Inventory → Saga | OrderId |
| `StockReservationFailed` | event | Inventory → Saga | OrderId, Reason |
| `ProcessPayment` | command (published) | Saga → Payment | OrderId, Amount, SimulateFailure |
| `PaymentApproved` | event | Payment → Saga | OrderId, TransactionId |
| `PaymentDeclined` | event | Payment → Saga | OrderId, Reason |
| `CommitStock` | command (published) | Saga → Inventory | OrderId |
| `StockCommitted` | event | Inventory → Saga | OrderId |
| `ReleaseStock` | command (compensation) | Saga → Inventory | OrderId |
| `StockReleased` | event | Inventory → Saga | OrderId |

## Saga (OrderStateMachine)

```text
Initial        --OrderSubmitted-->          AwaitingStock     / publish ReserveStock
AwaitingStock  --StockReserved-->           AwaitingPayment   / publish ProcessPayment
AwaitingStock  --StockReservationFailed-->  Rejected (final)
AwaitingPayment--PaymentApproved-->         AwaitingCommit    / publish CommitStock
AwaitingPayment--PaymentDeclined-->         AwaitingRelease   / publish ReleaseStock (compensation)
AwaitingCommit --StockCommitted-->          Confirmed (final)
AwaitingRelease--StockReleased-->           Cancelled (final)
```

Every transition also updates the `Order` aggregate status (same DB transaction).

## Error handling

- Retry: exponential back-off (5 attempts, 100 ms → 5 s, jitter) — covers transient DB errors
  and optimistic concurrency conflicts.
- After retries are exhausted the message goes to `<queue>_error` (dead-letter) for inspection in
  the RabbitMQ Management UI.
