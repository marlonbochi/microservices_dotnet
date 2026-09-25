# Quickstart & Validation: Store Order Flow

## Prerequisites

- Docker Desktop with **≥ 6 GB RAM** (Settings → Resources)
- For running tests/services outside Docker: .NET SDK 10, Node 22

## Start everything

```bash
docker compose up -d --build
docker compose ps        # all services "healthy"/"running"
```

| URL | What |
|-----|------|
| http://localhost:3100 | Store frontend (Products / Orders) |
| http://localhost:5100 | API Gateway |
| http://localhost:15672 | RabbitMQ Management (guest / guest) |
| http://localhost:18888 | Aspire Dashboard (traces, logs, metrics) |

## Validation scenarios

1. **Catalog + stock sync (US1)**: on *Products*, create SKU `KB-001`, price 350, stock 10 →
   within 5 s the row shows "Available: 10". Restock +5 → 15. Create `KB-001` again → error
   "A product with SKU 'KB-001' already exists".
2. **Happy order (US2)**: on *Orders*, order 3 × `KB-001` → timeline shows Submitted →
   StockReserved → PaymentApproved → Confirmed without reloading; product now 12 available.
3. **Insufficient stock (US3)**: order 999 units → Rejected ("insufficient stock"); stock
   unchanged.
4. **Compensation (US3)**: order 2 units with "Simulate payment failure" → PaymentDeclined →
   Cancelled; stock returns to the previous value.
5. **Observability (US4)**: Aspire Dashboard → Traces → pick `POST /api/ordering/orders` → the trace
   spans gateway, ordering, inventory and payment. RabbitMQ UI → Queues shows one queue per consumer.

API-only variant (same checks with curl):

```bash
curl -s -X POST localhost:5100/api/catalog/products -H 'content-type: application/json' \
  -d '{"sku":"KB-001","name":"Teclado","price":350,"initialStock":10}'
curl -s localhost:5100/api/inventory/stock
```

## Automated tests

```bash
dotnet test tests/Architecture.Tests
dotnet test --filter "Category!=Integration"   # unit + messaging (no Docker needed)
dotnet test                                    # everything (integration uses Testcontainers)
```

Expected: all tests green, zero build warnings.
