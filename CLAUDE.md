# CLAUDE.md

Guidance for coding agents working in this repository.

## Project

Microservices learning store: .NET 10 services (Catalog, Inventory, Ordering, Payment) + YARP gateway +
Nuxt 4 frontend, integrated via MassTransit 8 / RabbitMQ, SQL Server per service, run with Docker Compose.
Spec-driven with Spec Kit: read `.specify/memory/constitution.md` (principles I–VII are binding) and
`specs/001-store-order-flow/` before changing behaviour.

## Commands

- Build: `dotnet build Store.slnx` (warnings are errors)
- Tests: `dotnet test` (Microsoft.Testing.Platform; integration tests need Docker)
- One project: `dotnet test --project tests/Ordering.UnitTests`
- New migration: `dotnet ef migrations add <Name> -p src/Services/<S>/<S>.Infrastructure -s src/Services/<S>/<S>.Api -o Persistence/Migrations`
- Run all: `docker compose up -d --build`
- Frontend: `cd frontend && npm install && npm run dev` / `npm test`

## Conventions

- Clean Architecture per service; Domain/Application must not reference EF Core, MassTransit or ASP.NET
  (enforced by `tests/Architecture.Tests`).
- One endpoint per class implementing `IEndpoint`; handlers implement `ICommandHandler`/`IQueryHandler`.
- Expected failures return `Result`/`Error` (mapped to Problem Details); exceptions only for bugs.
- Integration events go through `IIntegrationEventPublisher` (outbox). Message contracts live only in
  `Store.Contracts`. Never share domain code between services.
- Package versions only in `Directory.Packages.props`. MassTransit stays on v8 (license).
- Everything in English: code, commits (Conventional Commits), docs under `docs/`, ADRs and UI text.
