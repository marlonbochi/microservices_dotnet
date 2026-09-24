# ADR 0001: Microsserviços com banco por serviço

- **Status**: Aceito
- **Data**: 2026-09-24

## Contexto

Queremos estudar microsserviços com fronteiras reais. Compartilhar banco entre serviços cria acoplamento oculto (mudar uma tabela quebra outro time).

## Decisão

Quatro serviços (Catalog, Inventory, Ordering, Payment), cada um com seu próprio banco (`CatalogDb`, `InventoryDb`, `OrderingDb`); Payment é stateless. Integração só por API/mensagens. Em dev, um único container SQL Server hospeda os três bancos.

## Consequências

- Autonomia e deploy independente.
- Fronteiras explícitas.
- Consistência eventual.
- Consultas que cruzam serviços exigem composição (UI/gateway) ou réplicas locais de dados.
