# ADR 0004: Transactional outbox and inbox

- **Status**: Accepted
- **Date**: 2026-09-24

## Context

Writing to the database and publishing to the broker are two separate operations (dual write); a crash in between leaves them inconsistent.

## Decision

MassTransit's EF Core outbox: *bus outbox* in the APIs and *consumer outbox + inbox* in consumers, running at `ReadCommitted`. Handlers are also idempotent by business key.

## Consequences

- State and messages are committed atomically.
- Duplicate deliveries are dropped.
- Extra tables and a small delivery latency.
