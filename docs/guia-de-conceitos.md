# Guia de conceitos: onde está cada ideia no código

Use este guia como roteiro de estudo: leia o conceito e abra o arquivo indicado.

## Fundamentos de microsserviços

| Conceito | O que é | Onde ver |
|----------|---------|----------|
| Bounded context | Cada serviço modela uma parte do negócio com sua própria linguagem | `src/Services/*` |
| Database per service | Nenhum serviço acessa o banco de outro | `CatalogDbContext`, `InventoryDbContext`, `OrderingDbContext` (schemas `catalog`, `inventory`, `ordering`) |
| API Gateway | Porta única de entrada, roteamento, CORS, WebSockets | [`Store.Gateway/appsettings.json`](../src/Gateway/Store.Gateway/appsettings.json) |
| Contratos compartilhados | Mensagens versionáveis como "API pública" assíncrona | [`Store.Contracts`](../src/BuildingBlocks/Store.Contracts) |
| Replicação de dados | Inventory guarda uma cópia do nome/SKU do produto | [`ProductCreatedConsumer`](../src/Services/Inventory/Inventory.Infrastructure/Messaging/Consumers/CatalogConsumers.cs) |
| Composição na UI | Uma tela junta dados de dois serviços | [`frontend/app/stores/catalog.ts`](../frontend/app/stores/catalog.ts) (`productsWithStock`) |

## Mensageria

| Conceito | Onde ver |
|----------|----------|
| Publicar evento via porta da Application | [`CreateProductHandler`](../src/Services/Catalog/Catalog.Application/Products/Create/CreateProductHandler.cs) → `IIntegrationEventPublisher` |
| Adaptador MassTransit | [`OutboxEventPublisher`](../src/Services/Catalog/Catalog.Infrastructure/Messaging/OutboxEventPublisher.cs) |
| Consumer fino (só traduz mensagem → comando) | [`StockConsumers.cs`](../src/Services/Inventory/Inventory.Infrastructure/Messaging/Consumers/StockConsumers.cs) |
| Saga / máquina de estados | [`OrderStateMachine`](../src/Services/Ordering/Ordering.Infrastructure/Messaging/Sagas/OrderStateMachine.cs) |
| Compensação | `PaymentDeclined → ReleaseStock` na saga + [`ReleaseStockHandler`](../src/Services/Inventory/Inventory.Application/Reservations/SettleReservation.cs) |
| Idempotência de negócio | `StockReservation` com Id = `OrderId`; [`ReserveStockHandler`](../src/Services/Inventory/Inventory.Application/Reservations/ReserveStock.cs) reconhece duplicatas |
| Outbox / Inbox | `DependencyInjection.cs` de cada Infrastructure; tabelas nas migrations |
| Retry + dead-letter | [`MessagingExtensions`](../src/BuildingBlocks/Store.ServiceDefaults/Messaging/MessagingExtensions.cs) |

## Domínio (DDD tático)

| Conceito | Onde ver |
|----------|----------|
| Agregado com invariantes | [`StockItem`](../src/Services/Inventory/Inventory.Domain/Stock/StockItem.cs): reservado nunca passa do físico |
| Value object | [`Sku`](../src/Services/Catalog/Catalog.Domain/Products/Sku.cs) |
| Máquina de estados no domínio | [`Order.AllowedTransitions`](../src/Services/Ordering/Ordering.Domain/Orders/Order.cs) |
| Domain event | `OrderStatusChangedDomainEvent` → despachado em `OrderingDbContext.SaveChangesAsync` |
| Result pattern (erro esperado ≠ exceção) | [`Result`/`Error`](../src/BuildingBlocks/Store.SharedKernel) → mapeado para Problem Details em [`ResultExtensions`](../src/BuildingBlocks/Store.ServiceDefaults/Http/ResultExtensions.cs) |
| Snapshot de dados externos | `OrderItem` copia nome e preço do Catalog no momento do pedido |

## Resiliência e consistência

| Conceito | Onde ver |
|----------|----------|
| Circuit breaker / retry / timeout HTTP | `AddStandardResilienceHandler()` em [`Ordering.Infrastructure/DependencyInjection.cs`](../src/Services/Ordering/Ordering.Infrastructure/DependencyInjection.cs) |
| Falha rápida com mensagem amigável | [`CatalogHttpClient`](../src/Services/Ordering/Ordering.Infrastructure/Catalog/CatalogHttpClient.cs) → 503 |
| Concorrência otimista | `rowversion` em [`StockItemConfiguration`](../src/Services/Inventory/Inventory.Infrastructure/Persistence/Configurations/StockItemConfiguration.cs) |
| Tudo-ou-nada | `ReserveStockHandler.TryReserveAllAsync` valida todas as linhas antes de reservar |
| Consistência eventual visível | Badge "Sincronizando via evento..." em `frontend/app/pages/products.vue` |

## Operação

| Conceito | Onde ver |
|----------|----------|
| Health checks (liveness/readiness) | [`ServiceDefaultsExtensions`](../src/BuildingBlocks/Store.ServiceDefaults/ServiceDefaultsExtensions.cs) |
| Tracing distribuído | mesmo arquivo; fonte `MassTransit` + instrumentações; veja no Aspire Dashboard |
| Problem Details (RFC 9457) | `AddProblemDetails` + `GlobalExceptionHandler` |
| Configuração por ambiente | `appsettings.json` + variáveis de ambiente em `docker-compose.yml` |
| Migrations automáticas (dev) | [`DatabaseMigrationExtensions`](../src/BuildingBlocks/Store.ServiceDefaults/DatabaseMigrationExtensions.cs) |

## Exercícios sugeridos

1. Derrube o Payment (`docker compose stop payment-worker`), faça um pedido e veja-o parado em
   "Estoque reservado". Suba o serviço de novo e veja o pedido continuar sozinho.
2. Derrube o Catalog e tente fazer um pedido: note o 503 e o circuit breaker nos logs.
3. Crie um produto com estoque 1 e faça dois pedidos quase ao mesmo tempo: um é confirmado e o outro é rejeitado.
4. Implemente um timeout de pagamento na saga (`Schedule`) e um estado `PaymentTimedOut`.
5. Adicione um serviço **Notification** que consome `OrderSubmitted` e "envia e-mails" (log): note que
   nenhum serviço existente precisa mudar. Isso é o Open/Closed no nível da arquitetura.
