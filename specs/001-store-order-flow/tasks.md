---

description: "Task list for 001-store-order-flow"
---

# Tasks: Store Order Flow (Products, Stock and Orders)

**Input**: Design documents from `/specs/001-store-order-flow/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Requested (user asked for a full .NET test suite; constitution Principle V).

**Organization**: grouped by user story so each can be implemented and tested independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependencies)
- **[Story]**: US1..US4 from spec.md

---

## Phase 1: Setup (Shared Infrastructure)

- [X] T001 Create `Store.slnx`, `Directory.Build.props` (net10.0, nullable, TreatWarningsAsErrors), `Directory.Packages.props`, `global.json`, `.editorconfig`, `.gitignore`, `.dockerignore`
- [X] T002 [P] Create projects for every service layer under `src/Services/{Catalog,Inventory,Ordering}/*` and `src/Services/Payment/Payment.Worker` with project references following Clean Architecture
- [X] T003 [P] Create building block projects `src/BuildingBlocks/{Store.Contracts,Store.SharedKernel,Store.ServiceDefaults}`
- [X] T004 [P] Create gateway project `src/Gateway/Store.Gateway`
- [X] T005 [P] Create test projects under `tests/` (unit, integration, architecture) with shared test packages
- [X] T006 Add `dotnet-ef` local tool manifest `.config/dotnet-tools.json`

## Phase 2: Foundational (Blocking Prerequisites)

- [X] T007 [P] SharedKernel: `Entity`, `AggregateRoot` (domain events), `IDomainEvent`, `Result`/`Result<T>`, `Error`/`ErrorType`, `DomainException` in `src/BuildingBlocks/Store.SharedKernel/`
- [X] T008 [P] Contracts: all records from `contracts/messages.md` in `src/BuildingBlocks/Store.Contracts/{Catalog,Ordering,Inventory,Payment}/`
- [X] T009 ServiceDefaults: `AddServiceDefaults()` (OpenTelemetry, health checks, problem details, service discovery config), `MapDefaultEndpoints()`, `IEndpoint` + `MapEndpoints()`, `ValidationFilter<T>`, `ResultExtensions.ToProblem()`, MassTransit RabbitMQ bus defaults (`AddStoreMessaging`) in `src/BuildingBlocks/Store.ServiceDefaults/`
- [X] T010 [P] Architecture tests (layer direction for all three layered services) in `tests/Architecture.Tests/LayerDependencyTests.cs`
- [X] T011 `docker-compose.yml` with sqlserver (healthcheck), rabbitmq (management), aspire-dashboard; `.env.example`

**Checkpoint**: solution builds with zero warnings; architecture tests pass.

## Phase 3: User Story 1 – Manage the product catalog (P1) 🎯 MVP

**Goal**: create/list/update products and see stock synchronised by events; restock.

**Independent Test**: create product with stock 10 → inventory lists 10 available; restock +5 → 15.

### Tests for US1

- [X] T012 [P] [US1] Unit tests `Product` aggregate in `tests/Catalog.UnitTests/Domain/ProductTests.cs`
- [X] T013 [P] [US1] Unit tests create/update handlers + validators in `tests/Catalog.UnitTests/Application/`
- [X] T014 [P] [US1] Unit tests `StockItem` aggregate in `tests/Inventory.UnitTests/Domain/StockItemTests.cs`
- [X] T015 [P] [US1] Harness tests `ProductCreatedConsumer`/`ProductUpdatedConsumer` in `tests/Inventory.UnitTests/Messaging/`
- [X] T016 [P] [US1] Integration tests Catalog API (Testcontainers SQL) in `tests/Catalog.IntegrationTests/ProductEndpointsTests.cs`
- [X] T017 [P] [US1] Integration tests Inventory API (stock list/restock) in `tests/Inventory.IntegrationTests/StockEndpointsTests.cs`

### Implementation for US1

- [X] T018 [P] [US1] `Product` aggregate + `Sku` value object + errors in `src/Services/Catalog/Catalog.Domain/`
- [X] T019 [US1] Catalog application: ports (`IProductRepository`, `IUnitOfWork`, `IIntegrationEventPublisher`), DTOs, validators, handlers (Create, Update, GetById, List, GetBatch) in `src/Services/Catalog/Catalog.Application/`
- [X] T020 [US1] Catalog infrastructure: `CatalogDbContext`, configurations, repository, MassTransit bus outbox publisher, DI, migrations in `src/Services/Catalog/Catalog.Infrastructure/`
- [X] T021 [US1] Catalog API endpoints, `Program.cs`, appsettings, Dockerfile in `src/Services/Catalog/Catalog.Api/`
- [X] T022 [P] [US1] `StockItem` aggregate + errors in `src/Services/Inventory/Inventory.Domain/`
- [X] T023 [US1] Inventory application: register product stock, rename, restock, queries in `src/Services/Inventory/Inventory.Application/`
- [X] T024 [US1] Inventory infrastructure: `InventoryDbContext` (rowversion), repositories, consumers `ProductCreated`/`ProductUpdated`, consumer outbox/inbox, migrations in `src/Services/Inventory/Inventory.Infrastructure/`
- [X] T025 [US1] Inventory API endpoints, `Program.cs`, Dockerfile in `src/Services/Inventory/Inventory.Api/`
- [X] T026 [US1] Gateway: YARP routes/clusters + CORS + Dockerfile in `src/Gateway/Store.Gateway/`; add catalog, inventory, gateway to compose
- [X] T027 [US1] Frontend scaffold (Nuxt 4 + Nuxt UI + Pinia), layout/nav, API composable, Products page (list, create, edit, restock) in `frontend/`; Dockerfile; add to compose

**Checkpoint**: US1 fully functional via UI and curl.

## Phase 4: User Story 2 – Place an order and watch it (P1)

**Goal**: place order → saga → confirmed; live timeline.

**Independent Test**: stock 10, order 3 → Confirmed, stock 7.

### Tests for US2

- [X] T028 [P] [US2] Unit tests `Order` aggregate (totals, transitions, history, merge duplicates) in `tests/Ordering.UnitTests/Domain/OrderTests.cs`
- [X] T029 [P] [US2] Unit tests `PlaceOrderHandler` (catalog fake, unknown product, catalog unavailable) in `tests/Ordering.UnitTests/Application/`
- [X] T030 [P] [US2] Saga harness tests happy path in `tests/Ordering.UnitTests/Sagas/OrderStateMachineTests.cs`
- [X] T031 [P] [US2] Harness tests `ReserveStockConsumer` / `CommitStockConsumer` in `tests/Inventory.UnitTests/Messaging/`
- [X] T032 [P] [US2] Harness tests `ProcessPaymentConsumer` approve path in `tests/Payment.UnitTests/`
- [X] T033 [P] [US2] Integration tests Ordering API (place, get, list) in `tests/Ordering.IntegrationTests/OrderEndpointsTests.cs`

### Implementation for US2

- [X] T034 [P] [US2] `Order` aggregate, `OrderItem`, `OrderStatusChange`, `OrderStatus`, domain event in `src/Services/Ordering/Ordering.Domain/`
- [X] T035 [US2] Ordering application: `ICatalogClient`, `IOrderRepository`, `IOrderNotifier`, place/get/list handlers, validator in `src/Services/Ordering/Ordering.Application/`
- [X] T036 [US2] Ordering infrastructure: DbContext (domain event dispatch after save), saga `OrderStateMachine` + `OrderState` + sync activity, EF saga repository, bus outbox, resilient `CatalogHttpClient`, SignalR notifier, migrations in `src/Services/Ordering/Ordering.Infrastructure/`
- [X] T037 [US2] Ordering API endpoints + `OrdersHub` + Dockerfile in `src/Services/Ordering/Ordering.Api/`
- [X] T038 [US2] Inventory: `StockReservation` aggregate, reserve (all-or-nothing) + commit handlers and consumers
- [X] T039 [US2] Payment worker: `IPaymentGateway` + `FakePaymentGateway`, `ProcessPaymentConsumer`, Dockerfile in `src/Services/Payment/Payment.Worker/`
- [X] T040 [US2] Gateway route for ordering + hub (WebSockets); compose entries for ordering + payment
- [X] T041 [US2] Frontend Orders page: cart builder, place order, orders list, detail timeline, SignalR live updates

**Checkpoint**: US1 + US2 work end-to-end.

## Phase 5: User Story 3 – Shortage and compensation (P2)

**Independent Test**: order 50 of 10 → Rejected; order 2 with failure flag → Cancelled and stock restored.

- [X] T042 [P] [US3] Saga harness tests: stock failure → Rejected; payment declined → ReleaseStock → Cancelled in `tests/Ordering.UnitTests/Sagas/`
- [X] T043 [P] [US3] Harness tests reserve failure (all-or-nothing), release, duplicate message idempotency in `tests/Inventory.UnitTests/Messaging/`
- [X] T044 [P] [US3] Payment decline test in `tests/Payment.UnitTests/`
- [X] T045 [P] [US3] Integration concurrency test: 100 concurrent reservations for 10 units → exactly 10 succeed (real SQL) in `tests/Inventory.IntegrationTests/ConcurrencyTests.cs`
- [X] T046 [US3] Inventory `ReleaseStock` handler/consumer; failure reason messages
- [X] T047 [US3] Frontend: failure flag toggle, failure reason display, status colours

## Phase 6: User Story 4 – Observe the distributed system (P3)

- [X] T048 [US4] Aspire dashboard wiring (OTLP env vars) for all services in compose; MassTransit activity source
- [X] T049 [P] [US4] Integration test readiness endpoint healthy in each API integration suite
- [X] T050 [US4] Frontend "Arquitetura" page with links to dashboards and live flow explanation

## Phase 7: Polish & Cross-Cutting

- [X] T051 [P] `README.md` (pt-BR) with overview, how to run, URLs, repo map
- [X] T052 [P] `docs/` : arquitetura.md (C4 + Mermaid), mensageria-e-saga.md, guia-de-conceitos.md (concept → file map), testes.md, api.md
- [X] T053 [P] ADRs in `docs/adr/` (0001 microservices + DB per service, 0002 MassTransit v8 + RabbitMQ, 0003 saga orchestration, 0004 outbox, 0005 optimistic concurrency, 0006 YARP gateway)
- [X] T054 [P] GitHub Actions CI `.github/workflows/ci.yml` (build, unit, architecture, integration tests; frontend build)
- [ ] T055 Full `docker compose up --build` validation following quickstart.md (backend + frontend validated end-to-end with infra in Docker and services via `dotnet run`; full image build pending free disk space)
- [X] T056 Update `CLAUDE.md` agent context with stack and commands

---

## Dependencies & Execution Order

- Setup (1) → Foundational (2) → US1 (3) → US2 (4) → US3 (5) → US4 (6) → Polish (7)
- US2 depends on US1 (products and stock must exist). US3 extends US2's saga/consumers.
  US4 is mostly configuration and can start after Phase 2.

## Parallel Opportunities

- T002–T005 in parallel; T007/T008/T010 in parallel.
- US1: Catalog (T018–T021) and Inventory (T022–T025) tracks in parallel; all test tasks [P].
- US2: domain (T034), payment worker (T039) and frontend (T041) in parallel after contracts exist.

## Implementation Strategy

1. MVP = Phases 1–3 (catalog + stock sync + products screen).
2. Add US2 (orders + saga + live timeline) → demoable core.
3. Add US3 (compensation + concurrency) → complete learning story.
4. US4 + Polish (docs, ADRs, CI).
Commit and push after each task group.
