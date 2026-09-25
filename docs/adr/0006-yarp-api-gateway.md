# ADR 0006: YARP API gateway

- **Status**: Accepted
- **Date**: 2026-09-24

## Context

The frontend should not know every service's address, and CORS and WebSockets should be handled in one place.

## Decision

YARP 2.3 with routes `/api/catalog`, `/api/inventory`, `/api/ordering` and `/hubs/orders`. Services already expose those prefixes, so no path transforms are needed.

## Consequences

- Plain .NET code, easy to study and extend (auth, rate limiting, aggregation).
- One more network hop; in production consider a managed gateway or an ingress controller.
