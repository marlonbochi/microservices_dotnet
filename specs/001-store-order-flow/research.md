# Research: Store Order Flow

All Technical Context items were resolved; each decision below records rationale and alternatives.

## R1. Runtime and API style

- **Decision**: .NET 10 LTS, ASP.NET Core Minimal APIs grouped in endpoint classes implementing a
  small `IEndpoint` interface.
- **Rationale**: latest LTS; Minimal APIs are the modern default, low ceremony, native OpenAPI
  (`Microsoft.AspNetCore.OpenApi`) and endpoint filters for validation.
- **Alternatives**: MVC controllers (more ceremony); FastEndpoints (extra dependency).

## R2. Messaging library and broker

- **Decision**: MassTransit **8.5.x** over **RabbitMQ 4 (management image)**.
- **Rationale**: MassTransit v8 is Apache-2.0, the de-facto .NET service bus; provides saga state
  machines, EF Core transactional outbox/inbox, retry, dead-letter (`_error`) queues, a test
  harness and OpenTelemetry instrumentation out of the box. v9 moved to a commercial license, so the
  constitution pins v8.
- **Alternatives**: Wolverine (good, less widespread); raw RabbitMQ.Client (would require
  re-implementing outbox, retries and sagas — too low level); Azure Service Bus (not local).

## R3. Reliable messaging (outbox / inbox)

- **Decision**: MassTransit EF Core outbox in every service that owns a database.
  - **Bus outbox** (API → broker): Catalog and Ordering APIs publish inside the same
    `SaveChanges` as the aggregate change; a delivery service forwards outbox rows to RabbitMQ.
  - **Consumer outbox + inbox** (broker → consumer): Inventory and Ordering consumers de-duplicate
    by `MessageId` and publish their own events atomically with their state change.
- **Rationale**: solves the dual-write problem (DB committed but message lost, or vice-versa);
  provides idempotency required by FR-015.
- **Alternatives**: hand-written outbox table + background worker (instructive but duplicative).

## R4. Order process coordination

- **Decision**: Orchestrated saga `OrderStateMachine` (MassTransit Automatonymous) persisted in
  `OrderingDb` via the EF Core saga repository with optimistic concurrency (`rowversion`).
  States: `AwaitingStock → AwaitingPayment → Confirmed | Rejected | Cancelled`.
- **Rationale**: a single place describes the whole flow, including compensation — ideal to study.
  A state activity updates the `Order` aggregate in the same transaction.
- **Alternatives**: choreography (each service reacts to others' events) — less coupling to the
  orchestrator but the flow becomes implicit and hard to follow for a learner. Documented in ADR.

## R5. Stock concurrency (no overselling)

- **Decision**: `StockItem` aggregate with a SQL Server `rowversion` concurrency token; the
  `ReserveStock` consumer uses MassTransit retry (exponential with jitter) on
  `DbUpdateConcurrencyException`. Reservations are all-or-nothing and unique per `OrderId`.
- **Rationale**: keeps invariants in the domain (`Available >= quantity`) while the database
  guarantees that two concurrent writers cannot both win.
- **Alternatives**: pessimistic locks (`UPDLOCK`) – harder to reason about; atomic `UPDATE ... WHERE`
  – fast but moves the rule out of the domain.

## R6. Synchronous query Ordering → Catalog

- **Decision**: typed `HttpClient` (`ICatalogClient`) with `AddStandardResilienceHandler()`
  (timeout, retry, circuit breaker) calling `GET /api/catalog/products/batch?ids=...`.
- **Rationale**: order prices must be fresh and authoritative at placement time (FR-007); the call
  is a query so it does not break autonomy; failure is surfaced as a 503 Problem Details (edge
  case "catalog unreachable").
- **Alternatives**: local replicated price cache via `ProductUpdated` (eventual consistency on
  price — acceptable but less clear for learning); gRPC (faster, more setup).

## R7. API Gateway

- **Decision**: YARP 2.3 with routes `/api/catalog/**`, `/api/inventory/**`, `/api/ordering/**`,
  `/hubs/orders/**` (WebSockets) and CORS for the frontend origin.
- **Alternatives**: Ocelot (less maintained); Nginx (no .NET code to study).

## R8. Real-time status (FR-014)

- **Decision**: SignalR hub `/hubs/orders` in Ordering. The `Order` aggregate raises
  `OrderStatusChangedDomainEvent`; the DbContext dispatches domain events **after** a successful
  `SaveChanges`, and a handler pushes the update to connected clients.
- **Alternatives**: polling (simpler, less instructive); Server-Sent Events.

## R9. Persistence and migrations

- **Decision**: EF Core 10 code-first migrations per service (committed in `Infrastructure/
  Persistence/Migrations`), applied on startup when `Database:ApplyMigrationsOnStartup=true`
  (enabled in Docker) with a retry loop while SQL Server boots. Money as `decimal(18,2)`.
- **Alternatives**: migration bundles / a dedicated migrator job (better for production, noted as
  future improvement).

## R10. Observability

- **Decision**: `Store.ServiceDefaults.AddServiceDefaults()` configures OpenTelemetry (ASP.NET
  Core, HttpClient, SqlClient, MassTransit `ActivitySource`, runtime metrics, logs) exporting OTLP
  to the **.NET Aspire Dashboard** container; health checks `/health/live` (process) and
  `/health/ready` (DB + bus).
- **Alternatives**: Jaeger + Prometheus + Grafana (more containers/RAM); Seq (logs only).

## R11. Testing strategy

- **Decision**:
  - Unit: domain aggregates and application handlers (NSubstitute fakes).
  - Messaging: MassTransit `ITestHarness` for consumers and every saga transition.
  - Integration: `WebApplicationFactory` + Testcontainers SQL Server (real DB, in-memory bus
    harness) per API.
  - Architecture: NetArchTest rules for layer direction.
  - xUnit v3 + Shouldly (Apache/BSD licensed; FluentAssertions 8 is commercial).
- **Alternatives**: EF InMemory provider (does not honor `rowversion`/SQL semantics — rejected).

## R12. Frontend

- **Decision**: Nuxt 4 (`app/` directory), Nuxt UI 4 (Tailwind 4 based, free), Pinia stores,
  `$fetch` wrappers in composables, `@microsoft/signalr` for live updates, UI text in pt-BR. Runs as
  SSR Node server in Docker; browser calls the gateway directly (`NUXT_PUBLIC_API_BASE`).
- **Alternatives**: Vuetify / PrimeVue (heavier), plain Vue + Vite (no conventions).

## R13. Local ports

| Component | Host port |
|-----------|-----------|
| Frontend (Nuxt) | 3100 |
| API Gateway | 5100 |
| Catalog / Inventory / Ordering APIs (debug only) | 5101 / 5102 / 5103 |
| SQL Server | 14330 |
| RabbitMQ AMQP / Management UI | 5672 / 15672 |
| Aspire Dashboard UI | 18888 |

Chosen to avoid ports already used on the developer machine (3000, 3001, 5000, 5173, 5432).
