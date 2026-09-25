# ADR 0001: Microservices with a database per service

- **Status**: Accepted
- **Date**: 2026-09-24

## Context

We want to study microservices with real boundaries. Sharing a database between services creates hidden coupling: changing a table breaks another team.

## Decision

Four services (Catalog, Inventory, Ordering, Payment), each with its own database (`CatalogDb`, `InventoryDb`, `OrderingDb`); Payment is stateless. Integration happens only through APIs and messages. In development a single SQL Server container hosts the three databases.

## Consequences

- Autonomy and independent deployment.
- Explicit boundaries.
- Eventual consistency must be handled.
- Cross-service queries need composition (UI/gateway) or local data replicas.
