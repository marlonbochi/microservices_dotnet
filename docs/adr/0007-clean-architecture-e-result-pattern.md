# ADR 0007: Clean Architecture por serviço e Result pattern

- **Status**: Aceito
- **Data**: 2026-09-24

## Contexto

O repositório é material de estudo de Clean Code e SOLID.

## Decisão

Camadas Domain/Application/Infrastructure/Api por serviço, verificadas por NetArchTest. Erros esperados usam `Result`/`Error` (sem exceções para fluxo de controle) e viram Problem Details. Sem MediatR: handlers explícitos atrás de `ICommandHandler`/`IQueryHandler`.

## Consequências

- Dependências explícitas e testáveis.
- Mais projetos e arquivos do que uma API monolítica simples.
