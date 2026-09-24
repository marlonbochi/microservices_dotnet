# Microservices Store — .NET 10 + Nuxt 4

Monorepo de estudo que mostra, com código real e testado, **como microsserviços funcionam na prática**:
serviços independentes, banco por serviço, API Gateway, mensageria com RabbitMQ, saga com compensação,
outbox/inbox, concorrência otimista, tempo real com SignalR e observabilidade com OpenTelemetry.

> Tudo foi construído com **Spec Kit** (spec-driven development): veja a
> [constituição](.specify/memory/constitution.md) e a pasta [`specs/001-store-order-flow`](specs/001-store-order-flow)
> (spec → plano → tarefas).

```mermaid
flowchart LR
    UI[Nuxt 4 SPA<br/>:3100] -->|HTTP + WebSocket| GW[API Gateway<br/>YARP :5100]
    GW --> CAT[Catalog API]
    GW --> INV[Inventory API]
    GW --> ORD[Ordering API<br/>+ Saga + SignalR]
    ORD -->|HTTP resiliente| CAT
    CAT & INV & ORD & PAY[Payment Worker] <-->|MassTransit| MQ[(RabbitMQ)]
    CAT --- DB1[(CatalogDb)]
    INV --- DB2[(InventoryDb)]
    ORD --- DB3[(OrderingDb)]
    CAT & INV & ORD & PAY & GW -.->|OTLP| OBS[Aspire Dashboard]
```

## Como executar

Pré-requisitos: Docker Desktop com **≥ 6 GB de RAM** e **~15 GB livres em disco**.

```bash
docker compose up -d --build
```

| URL | O que é |
|-----|---------|
| http://localhost:3100 | Loja (Produtos, Pedidos, Arquitetura) |
| http://localhost:5100 | API Gateway |
| http://localhost:5101/scalar · 5102 · 5103 | Documentação OpenAPI de cada serviço (Scalar) |
| http://localhost:15672 | RabbitMQ Management (guest / guest) |
| http://localhost:18888 | Aspire Dashboard (traces, logs, métricas) |

Roteiro sugerido de exploração: [`specs/001-store-order-flow/quickstart.md`](specs/001-store-order-flow/quickstart.md).

### Rodando localmente sem Docker para os serviços

```bash
docker compose up -d sqlserver rabbitmq aspire-dashboard   # só a infraestrutura
dotnet run --project src/Services/Catalog/Catalog.Api       # :5101
dotnet run --project src/Services/Inventory/Inventory.Api   # :5102
dotnet run --project src/Services/Ordering/Ordering.Api     # :5103
dotnet run --project src/Services/Payment/Payment.Worker    # :5104
dotnet run --project src/Gateway/Store.Gateway              # :5100
cd frontend && npm install && npm run dev                   # :3100
```

## Testes

```bash
dotnet test                                    # tudo (integração usa Testcontainers → Docker)
dotnet test --project tests/Architecture.Tests # regras de arquitetura
dotnet test --project tests/Ordering.UnitTests # domínio + saga (sem Docker)
cd frontend && npm test                        # vitest
```

Detalhes em [docs/testes.md](docs/testes.md).

## Mapa do repositório

| Caminho | Conteúdo |
|---------|----------|
| `src/BuildingBlocks/Store.Contracts` | Mensagens trocadas entre serviços (records imutáveis) |
| `src/BuildingBlocks/Store.SharedKernel` | `Entity`, `AggregateRoot`, `Result`, `Error` (sem frameworks) |
| `src/BuildingBlocks/Store.ServiceDefaults` | OpenTelemetry, health checks, Problem Details, MassTransit |
| `src/Services/Catalog` | Produtos (Domain / Application / Infrastructure / Api) |
| `src/Services/Inventory` | Estoque e reservas (concorrência otimista) |
| `src/Services/Ordering` | Pedidos, **saga** (`OrderStateMachine`) e hub SignalR |
| `src/Services/Payment` | Worker que simula um gateway de pagamento |
| `src/Gateway/Store.Gateway` | API Gateway com YARP |
| `frontend/` | Nuxt 4 + Nuxt UI + Pinia |
| `tests/` | Unitários, mensageria (harness), integração (Testcontainers), arquitetura |
| `docs/` | Documentação e ADRs |
| `specs/` | Artefatos do Spec Kit |

## Documentação

- [Arquitetura](docs/arquitetura.md): visão geral, camadas e decisões
- [Mensageria e saga](docs/mensageria-e-saga.md): o fluxo de um pedido, passo a passo
- [Guia de conceitos](docs/guia-de-conceitos.md): cada conceito de microsserviços e **onde ele está no código**
- [Testes](docs/testes.md): a estratégia de testes
- [ADRs](docs/adr): registros de decisões de arquitetura

## Stack

.NET 10 · ASP.NET Core Minimal APIs · EF Core 10 · SQL Server 2022 · MassTransit 8 · RabbitMQ 4 ·
YARP · SignalR · OpenTelemetry · Aspire Dashboard · FluentValidation · xUnit v3 · Testcontainers ·
NetArchTest · Nuxt 4 · Nuxt UI 4 · Pinia · Docker Compose
