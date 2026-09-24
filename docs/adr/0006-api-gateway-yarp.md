# ADR 0006: API Gateway com YARP

- **Status**: Aceito
- **Data**: 2026-09-24

## Contexto

O frontend não deve conhecer endereços de cada serviço; precisamos de CORS e WebSockets centralizados.

## Decisão

YARP 2.3 com rotas `/api/catalog`, `/api/inventory`, `/api/ordering` e `/hubs/orders`. Os serviços já expõem o prefixo, então não há transformação de caminho.

## Consequências

- Código .NET simples de estudar e estender (auth, rate limiting, agregação).
- Mais um salto de rede; em produção, considerar um gateway gerenciado ou ingress.
