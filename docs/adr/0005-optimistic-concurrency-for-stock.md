# ADR 0005: Optimistic concurrency for stock

- **Status**: Accepted
- **Date**: 2026-09-24

## Context

Concurrent orders for the same product must never sell more than what is in stock (SC-004).

## Decision

`StockItem` has a `rowversion` column and a check constraint (`0 <= Reserved <= OnHand`). Conflicts raise `DbUpdateConcurrencyException` and MassTransit's retry re-processes the message with fresh data. The outbox transaction uses `ReadCommitted`: the default `RepeatableRead` held read locks until commit and deadlocked concurrent reservations of the same item.

## Consequences

- Business rules stay in the domain.
- No long-held locks.
- Under very high contention there are more retries; alternatives are a queue partitioned by product or an atomic `UPDATE ... WHERE available >= qty`.
