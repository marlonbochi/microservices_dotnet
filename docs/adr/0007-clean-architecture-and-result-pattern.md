# ADR 0007: Clean Architecture per service and the Result pattern

- **Status**: Accepted
- **Date**: 2026-09-24

## Context

The repository is study material for Clean Code and SOLID.

## Decision

Domain/Application/Infrastructure/Api layers per service, enforced by NetArchTest. Expected failures use `Result`/`Error` (no exceptions for control flow) and become Problem Details. No MediatR: explicit handlers behind `ICommandHandler`/`IQueryHandler`.

## Consequences

- Explicit, testable dependencies.
- More projects and files than a simple monolithic API.
