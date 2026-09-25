# Implementation Plan: Store Order Flow (Products, Stock and Orders)

**Branch**: `001-store-order-flow` | **Date**: 2026-09-24 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-store-order-flow/spec.md`

## Summary

A demo store split into four .NET 10 microservices (Catalog, Inventory, Ordering, Payment) behind
a YARP API Gateway, plus a Nuxt 4 frontend. Services own their SQL Server databases (EF Core 10)
and integrate asynchronously over RabbitMQ using MassTransit 8, with the transactional outbox/inbox
for reliable, idempotent messaging. The order lifecycle is orchestrated by a MassTransit saga
(state machine) in the Ordering service with compensation (stock release on payment decline).
Order status changes are pushed to the browser via SignalR. OpenTelemetry traces/metrics/logs go to
the .NET Aspire Dashboard. Everything runs with `docker compose up`.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (LTS); TypeScript 5 / Node 22 for the frontend

**Primary Dependencies**: ASP.NET Core Minimal APIs, EF Core 10 (SQL Server provider),
MassTransit 8.5 (RabbitMQ transport, EF Core outbox + saga repository), YARP 2.3,
FluentValidation 12, Microsoft.Extensions.Http.Resilience, OpenTelemetry 1.19, SignalR,
Scalar (OpenAPI UI). Frontend: Nuxt 4, Nuxt UI 4, Pinia, @microsoft/signalr.

**Storage**: SQL Server 2022 (one container, three databases: `CatalogDb`, `InventoryDb`,
`OrderingDb`). Payment is stateless.

**Testing**: xUnit v3, Shouldly, NSubstitute, MassTransit Test Harness, Testcontainers
(MsSql), Microsoft.AspNetCore.Mvc.Testing, NetArchTest. Frontend: Vitest.

**Target Platform**: Linux containers on Docker Desktop (developer machine, Apple Silicon/x64)

**Project Type**: Web application (microservices backend + SPA/SSR frontend) in a monorepo

**Performance Goals**: order confirmed < 5 s end-to-end (SC-003); 100 concurrent orders on the same
product without overselling (SC-004)

**Constraints**: single command startup; Docker memory ≥ 6 GB recommended (SQL Server needs 2 GB)

**Scale/Scope**: demo / learning scale — 5 backend deployables, 2 frontend screens, ~25 endpoints
and message types

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | How the design complies | Status |
|-----------|-------------------------|--------|
| I. Clean Code | `TreatWarningsAsErrors`, nullable enabled in `Directory.Build.props`; small endpoint/handler classes | ✅ |
| II. SOLID & Clean Architecture | Each service = `Domain` / `Application` / `Infrastructure` / `Api`; rich aggregates; NetArchTest enforces direction | ✅ |
| III. Service Autonomy | DB per service; shared libs limited to `Store.Contracts`, `Store.SharedKernel`, `Store.ServiceDefaults` | ✅ |
| IV. Messaging First | Integration events via MassTransit + EF outbox/inbox; order saga with compensation; one sync call (Ordering → Catalog) protected by standard resilience handler | ✅ |
| V. Test Discipline | Unit (domain), harness (consumers + saga), integration (Testcontainers + WebApplicationFactory), architecture tests | ✅ |
| VI. Observability & Resilience | `/health/live`, `/health/ready`; OTel → Aspire Dashboard; Problem Details; message retry + `_error` queues | ✅ |
| VII. Simplicity | No MediatR; Minimal APIs; MassTransit pinned to v8 (Apache-2.0) | ✅ |

**Post-design re-check (after Phase 1)**: ✅ no new violations. Payment is a single-project worker
(see Complexity Tracking).

## Project Structure

### Documentation (this feature)

```text
specs/001-store-order-flow/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (HTTP + message contracts)
└── tasks.md             # Phase 2 output (/speckit-tasks)
```

### Source Code (repository root)

```text
Store.slnx
Directory.Build.props            # common MSBuild settings (net10.0, nullable, warnings as errors)
Directory.Packages.props         # central package versions
docker-compose.yml               # sqlserver, rabbitmq, aspire-dashboard, services, gateway, frontend
src/
├── BuildingBlocks/
│   ├── Store.Contracts/         # integration events & commands (records, no deps)
│   ├── Store.SharedKernel/      # Entity, AggregateRoot, IDomainEvent, Result, Error
│   └── Store.ServiceDefaults/   # OTel, health checks, problem details, MassTransit defaults
├── Services/
│   ├── Catalog/
│   │   ├── Catalog.Domain/          # Product aggregate
│   │   ├── Catalog.Application/     # use cases, validators, ports (IProductRepository...)
│   │   ├── Catalog.Infrastructure/  # EF Core DbContext, migrations, repositories, publisher
│   │   └── Catalog.Api/             # Minimal API endpoints, Program.cs, Dockerfile
│   ├── Inventory/  (same 4 layers; consumers in Infrastructure/Messaging)
│   ├── Ordering/   (same 4 layers; saga state machine + SignalR hub)
│   └── Payment/
│       └── Payment.Worker/          # stateless consumer + fake payment gateway
└── Gateway/
    └── Store.Gateway/               # YARP reverse proxy + CORS
tests/
├── Catalog.UnitTests/  Catalog.IntegrationTests/
├── Inventory.UnitTests/ Inventory.IntegrationTests/
├── Ordering.UnitTests/  Ordering.IntegrationTests/
├── Payment.UnitTests/
└── Architecture.Tests/
frontend/                        # Nuxt 4 app (app/pages/products.vue, app/pages/orders.vue ...)
docs/                            # documentation, diagrams and ADRs (English)
```

**Structure Decision**: monorepo with one folder per bounded context under `src/Services`, each
following Clean Architecture; building blocks restricted to the three allowed shared libraries;
tests mirror services; the frontend is an independent Nuxt project.

## Communication Design

```text
Browser ──HTTP/WS──▶ Gateway (YARP :5100) ──▶ catalog-api | inventory-api | ordering-api (/hubs/orders)
ordering-api ──HTTP (resilient)──▶ catalog-api     (fresh prices at order time)

RabbitMQ (MassTransit, publish/subscribe):
  Catalog   ─ ProductCreated / ProductUpdated ─▶ Inventory
  Ordering  ─ OrderSubmitted ─▶ OrderStateMachine (saga, in Ordering)
  Saga      ─ ReserveStock ─▶ Inventory ─ StockReserved | StockReservationFailed ─▶ Saga
  Saga      ─ ProcessPayment ─▶ Payment ─ PaymentApproved | PaymentDeclined ─▶ Saga
  Saga      ─ CommitStock (approved) | ReleaseStock (declined, compensation) ─▶ Inventory
```

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Payment has a single project instead of 4 layers | Stateless fake gateway with one consumer; no domain model or persistence | Four near-empty projects would add noise without teaching anything |
| Saga + domain aggregate both model order status | Saga = process state (technical), Order = business record exposed by the API; saga activity keeps them in sync in the same transaction | Using only the saga would leak MassTransit state into the API/read model |
