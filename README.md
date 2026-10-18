# Plataforma de Pedidos

Três microsserviços .NET que conversam por mensageria (RabbitMQ), com o pedido acompanhado em tempo real no navegador
via SignalR e cada requisição rastreada de ponta a ponta com OpenTelemetry. Front-end em Next.js.

**Demo:** https://jvcamargo10.github.io/plataforma-pedidos/
(na demo, os serviços e o RabbitMQ são simulados no navegador, com as mesmas regras do back-end)

## Arquitetura

```
 Next.js ──POST /api/pedidos──► Pedidos.Api ──(outbox)──► RabbitMQ  exchange "pedidos" (topic)
    ▲                               ▲                        │
    │                               │ PagamentoAprovado/     │ PedidoCriado
    │ SignalR "evento"              │ PagamentoRecusado      ▼
 Notificacoes.Api ◄── todos os eventos ──────────────── Pagamentos.Worker
```

| Serviço | Papel |
|---|---|
| **Pedidos.Api** | REST (minimal API) + EF Core/SQLite. Valida o pedido, grava pedido e evento na mesma transação (outbox) e confirma ou cancela conforme o pagamento. |
| **Pagamentos.Worker** | Worker que consome `PedidoCriado`, aplica as regras do gateway simulado e publica o resultado. Recebe só os 4 últimos dígitos do cartão. |
| **Notificacoes.Api** | Consome todos os eventos e repassa ao navegador por SignalR. |
| **Compartilhado** | Contratos dos eventos, envelope de mensagem, cliente RabbitMQ, consumidor genérico e telemetria. |

## Decisões que valem conversa

- **Outbox:** o pedido e o evento `PedidoCriado` são gravados no mesmo `SaveChanges`; um `BackgroundService` publica depois.
  Se o RabbitMQ cair, nenhum pedido fica sem evento.
- **Consumidor idempotente:** Pedidos guarda o id de cada mensagem processada e checa o estado do pedido antes de mudar.
  Pagamentos gera o id da resposta de forma determinística a partir do pedido, então uma reentrega não confirma duas vezes.
- **Fila de mortas:** mensagem que falha vai para `<fila>.mortas` (dead-letter exchange) em vez de travar a fila.
- **Rastreamento distribuído:** o `traceparent` viaja no envelope; no Jaeger, um pedido aparece como um único trace
  passando por Pedidos → RabbitMQ → Pagamentos → Pedidos → Notificações.
- **Dados sensíveis:** o número completo do cartão só existe na validação (Luhn) do serviço de Pedidos.

## Como rodar

```bash
docker compose up --build          # RabbitMQ, Jaeger e os três serviços
cd frontend && npm install
NEXT_PUBLIC_URL_PEDIDOS=http://localhost:5101 NEXT_PUBLIC_URL_HUB=http://localhost:5103/hubs/pedidos npm run dev
```

- Loja: http://localhost:3000
- Painel do RabbitMQ: http://localhost:15672 (guest/guest)
- Traces no Jaeger: http://localhost:16686

Cartões de teste: `4111 1111 1111 1111` aprova; `4000 0000 0000 0002` é recusado pelo emissor. Pedidos acima de R$ 5.000 são recusados.

## Testes

```bash
dotnet test backend/PlataformaPedidos.slnx   # xUnit: regras, outbox, idempotência, API com WebApplicationFactory
cd frontend && npm test                       # Jest + React Testing Library
```

Os testes do back-end rodam sem RabbitMQ: o serviço sobe com o barramento em memória e as mensagens são entregues aos
manipuladores pelo mesmo despachante usado em produção.

## Stack

C#, .NET 10, ASP.NET Core, EF Core, RabbitMQ.Client, SignalR, OpenTelemetry, Docker Compose, xUnit ·
Next.js 16, React 19, TypeScript, @microsoft/signalr, Jest, React Testing Library · GitHub Actions

---
Projeto de portfólio de [João Camargo](https://jvcamargo10.github.io/portfolio/). Produtos e pedidos são fictícios.
