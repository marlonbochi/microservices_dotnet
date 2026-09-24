<!--
Sync Impact Report
- Version change: 1.0.0 → 1.0.1 (PATCH: clarified allowed shared libraries in Principle III)
- Previous: (template) → 1.0.0
- Modified principles: all placeholders replaced (initial ratification)
- Added principles: I. Clean Code, II. SOLID & Clean Architecture, III. Service Autonomy
  (Database-per-Service), IV. Asynchronous Messaging First, V. Test Discipline,
  VI. Observability & Resilience, VII. Simplicity & Explicitness
- Added sections: Technology Constraints, Development Workflow & Quality Gates, Governance
- Removed sections: none
- Templates requiring updates: none (templates read the constitution at runtime) ✅
- Deferred TODOs: none
-->

# Microservices .NET Store Constitution

## Core Principles

### I. Clean Code

- Names MUST reveal intent (`ReserveStockConsumer`, not `Handler2`). Abbreviations are
  forbidden except well-known ones (`Id`, `Dto`, `Api`).
- Methods MUST do one thing and stay short (target ≤ 20 lines). Deep nesting (> 2 levels)
  MUST be replaced by guard clauses or extraction.
- No magic numbers or strings: use constants, enums or options classes.
- Comments explain *why*, never *what*. Dead code and commented-out code MUST NOT be committed.
- Code MUST compile with zero warnings (`TreatWarningsAsErrors=true`) and nullable reference
  types enabled.

Rationale: this repository is a learning reference; readability is the primary feature.

### II. SOLID & Clean Architecture

- **S**: each class has one reason to change (an endpoint maps HTTP, a handler orchestrates a
  use case, an aggregate enforces invariants, a repository persists).
- **O**: behaviour is extended by adding new handlers/consumers, not by editing switch statements.
- **L**: abstractions (e.g. `IPaymentGateway`) MUST be substitutable by fakes in tests
  without changing behaviour contracts.
- **I**: interfaces are small and role-specific (`IProductRepository`, `IUnitOfWork`), never
  "god" interfaces.
- **D**: inner layers own abstractions; outer layers implement them. Dependency direction is
  `Api → Application → Domain` and `Infrastructure → Application/Domain`. The Domain project
  MUST NOT reference any framework package (EF Core, MassTransit, ASP.NET).
- Business invariants live in the Domain (rich aggregates with private setters and factory
  methods), never in controllers/endpoints.
- Layering rules MUST be enforced by automated architecture tests.

### III. Service Autonomy (Database-per-Service)

- Each microservice owns its data in its own database; no service reads or writes another
  service's database.
- Services MUST NOT share domain models, persistence or business logic. Only three kinds of
  shared libraries are allowed: **message contracts** (immutable records, no dependencies), a
  **shared kernel** of framework-free primitives (`Entity`, `Result`, `Error`) and **service
  defaults** (host cross-cutting setup: telemetry, health checks, problem details).
- Each service is independently buildable, testable, containerized and deployable.
- External clients reach services only through the API Gateway.

### IV. Asynchronous Messaging First

- State changes that other services care about MUST be published as integration events
  through the message broker.
- Publishing MUST use the **Transactional Outbox** so the database change and the message are
  committed atomically; consumers MUST be idempotent (inbox / de-duplication).
- Long-running cross-service business processes (e.g. order placement) MUST be coordinated by
  an explicit **saga** with compensating actions, never by distributed transactions.
- Synchronous HTTP between services is allowed only for queries that need fresh data, and MUST
  be protected by resilience policies (timeout, retry, circuit breaker).

### V. Test Discipline

- Domain logic MUST have unit tests (happy path + invariant violations).
- Every consumer and saga transition MUST be covered by messaging tests using an in-memory
  test harness.
- Every public HTTP endpoint MUST be covered by an integration test running against real
  infrastructure (containers) or an in-memory host.
- Architecture tests MUST guard layer dependencies.
- Tests follow Arrange-Act-Assert and are named `Method_Scenario_ExpectedResult`.

### VI. Observability & Resilience

- Every service MUST expose `/health/live` and `/health/ready` endpoints.
- Every service MUST emit structured logs, traces and metrics via OpenTelemetry, with trace
  context propagated across HTTP and messages so one order can be followed end-to-end.
- Errors returned by APIs MUST use RFC 9457 Problem Details.
- Message consumers MUST use retry with back-off and dead-letter queues for poison messages.

### VII. Simplicity & Explicitness

- YAGNI: no speculative abstractions. Prefer the framework's built-in capabilities
  (Minimal APIs, built-in DI, `IOptions`) over extra libraries.
- Avoid libraries with restrictive/commercial licenses for core flow (e.g. no MediatR ≥ 13,
  no MassTransit ≥ 9); explicit handler classes are preferred.
- Configuration is explicit and environment-driven (`appsettings.*.json` + environment
  variables); no hidden conventions.

## Technology Constraints

- Backend: .NET 10 (LTS), C# latest, ASP.NET Core Minimal APIs, EF Core 10 with SQL Server,
  MassTransit 8 over RabbitMQ, YARP API Gateway, OpenTelemetry.
- Frontend: Nuxt 4 (Vue 3, TypeScript strict) with Nuxt UI and Pinia.
- Runtime: everything runs locally with `docker compose up`.
- Tests: xUnit, FluentAssertions-compatible assertions (Shouldly), Testcontainers,
  MassTransit Test Harness, NetArchTest.
- Documentation is written in Portuguese (pt-BR); code, identifiers and commit messages in
  English.

## Development Workflow & Quality Gates

- Work follows Spec Kit: constitution → specify → plan → tasks → implement.
- Commits are small, focused and use Conventional Commits (`feat:`, `fix:`, `test:`,
  `docs:`, `chore:`, `refactor:`). Each completed task phase is committed and pushed.
- A change is "done" only when: it builds with zero warnings, all tests pass, and docs are
  updated.
- Architecture decisions with trade-offs are recorded as ADRs under `docs/adr/`.

## Governance

- This constitution supersedes other practices in the repository. Plans MUST include a
  "Constitution Check" that verifies compliance; any violation MUST be justified in the plan's
  Complexity Tracking table.
- Amendments are made via pull request updating this file, with a Sync Impact Report and a
  semantic version bump (MAJOR: principle removal/redefinition; MINOR: new principle or
  section; PATCH: clarification).
- Reviews MUST check compliance with principles I–VII.

**Version**: 1.0.1 | **Ratified**: 2026-09-24 | **Last Amended**: 2026-09-24
