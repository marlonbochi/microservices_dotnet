# ADR 0004: Outbox/Inbox transacional

- **Status**: Aceito
- **Data**: 2026-09-24

## Contexto

Gravar no banco e publicar no broker são duas operações (dual write), e uma falha entre elas causa inconsistência.

## Decisão

Outbox do MassTransit com EF Core: *bus outbox* nas APIs e *consumer outbox + inbox* nos consumidores. Handlers também são idempotentes por chave de negócio.

## Consequências

- Atomicidade entre estado e mensagens.
- Deduplicação.
- Tabelas extras e pequena latência de entrega (polling/notificação).
