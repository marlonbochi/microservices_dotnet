# ADR 0002: MassTransit 8 sobre RabbitMQ

- **Status**: Aceito
- **Data**: 2026-09-24

## Contexto

Precisamos de pub/sub, retry, dead-letter, sagas persistentes, outbox e testes sem broker.

## Decisão

MassTransit **8.5** (Apache-2.0) com transporte RabbitMQ. A versão 9 passou a ter licença comercial, então fixamos a v8 em `Directory.Packages.props`.

## Consequências

- Recursos prontos (saga, outbox, harness, OpenTelemetry).
- Topologia automática.
- Dependência de uma biblioteca; migração futura para v9 ou Wolverine exigiria esforço.
