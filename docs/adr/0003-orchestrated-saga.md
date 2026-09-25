# ADR 0003: Orchestrated saga for the order process

- **Status**: Accepted
- **Date**: 2026-09-24

## Context

Placing an order involves stock and payment, owned by different services. Distributed transactions (2PC) are not an option.

## Decision

An **orchestrated** saga (`OrderStateMachine`) in Ordering, persisted in `OrderingDb` with optimistic concurrency, publishes commands and reacts to events. Compensation: `PaymentDeclined → ReleaseStock`.

## Consequences

- The whole flow reads in one place, which is great for learning.
- Easy to test with the harness.
- The orchestrator knows every step. With choreography each service would react to the others' events: less central coupling, but the flow becomes implicit.
