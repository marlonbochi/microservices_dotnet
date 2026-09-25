# ADR 0002: MassTransit 8 over RabbitMQ

- **Status**: Accepted
- **Date**: 2026-09-24

## Context

We need pub/sub, retries, dead-lettering, persistent sagas, an outbox and tests that run without a broker.

## Decision

MassTransit **8.5** (Apache-2.0) with the RabbitMQ transport. Version 9 moved to a commercial license, so v8 is pinned in `Directory.Packages.props`.

## Consequences

- Batteries included: sagas, outbox, test harness, OpenTelemetry.
- Automatic topology.
- Dependency on one library; moving to v9 or Wolverine later would take effort.
