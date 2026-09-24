# ADR 0003: Saga orquestrada para o processo de pedido

- **Status**: Aceito
- **Data**: 2026-09-24

## Contexto

O pedido envolve estoque e pagamento, em serviços distintos, e transações distribuídas (2PC) não são viáveis.

## Decisão

Uma saga **orquestrada** (`OrderStateMachine`) no Ordering, persistida no `OrderingDb` com concorrência otimista, publica comandos e reage a eventos. Compensação: `PaymentDeclined → ReleaseStock`.

## Consequências

- Fluxo inteiro legível em um só lugar (ótimo para aprender).
- Fácil de testar com o harness.
- O orquestrador conhece os passos; na alternativa (coreografia), cada serviço reagiria a eventos dos outros, com menos acoplamento central, mas o fluxo ficaria implícito.
