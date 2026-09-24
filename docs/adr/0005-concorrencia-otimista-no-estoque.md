# ADR 0005: Concorrência otimista no estoque

- **Status**: Aceito
- **Data**: 2026-09-24

## Contexto

Pedidos simultâneos para o mesmo produto não podem vender além do estoque (SC-004).

## Decisão

`StockItem` com `rowversion` + *check constraint* (`0 <= Reserved <= OnHand`). Conflitos geram `DbUpdateConcurrencyException`; o retry do MassTransit reprocessa com dados novos.

## Consequências

- Regras continuam no domínio.
- Sem locks longos.
- Sob contenção muito alta, há mais tentativas; alternativas: fila particionada por produto ou `UPDATE ... WHERE available >= qty` atômico.
