# Arquitetura - RabbitMQ com Pagamentos

## 🏗️ Arquitetura da Solução

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          API Pagamentos (ASP.NET 8)                          │
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                         HTTP Endpoints                                │  │
│  │                                                                        │  │
│  │  POST /api/payments              (Criar pagamento)                   │  │
│  │  POST /api/payments/{id}/process (Processar pagamento)              │  │
│  │  GET  /api/payments/{id}         (Obter pagamento)                  │  │
│  │  GET  /api/payments              (Listar pagamentos do usuário)     │  │
│  │  GET  /api/users/games           (Listar jogos do usuário)          │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                          ▼                                                   │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                      Domain Layer (Events)                            │  │
│  │                                                                        │  │
│  │  • PaymentCreatedEvent                                               │  │
│  │  • PaymentProcessingEvent                                            │  │
│  │  • PaymentCompletedEvent                                             │  │
│  │  • PaymentFailedEvent                                                │  │
│  │  • GamePurchasedEvent                                                │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│       ▼ Persist               ▼ Publish                                      │
│  ┌────────────────┐      ┌─────────────────┐                               │
│  │  Event Store   │      │   RabbitMQ      │                               │
│  │  (In-Memory)   │      │  (MassTransit)  │                               │
│  └────────────────┘      └─────────────────┘                               │
│                                  ▼                                           │
│                        ┌──────────────────────┐                             │
│                        │  Consumer Handlers   │                             │
│                        │  (Background Jobs)   │                             │
│                        │                      │                             │
│                        │ • PaymentCreated...  │                             │
│                        │ • PaymentCompleted.. │                             │
│                        │ • PaymentFailed...   │                             │
│                        │ • GamePurchased...   │                             │
│                        └──────────────────────┘                             │
│                                  │                                           │
│                                  ▼                                           │
│                        (Integração com outros                               │
│                         microsserviços)                                     │
└─────────────────────────────────────────────────────────────────────────────┘
         │
         │ HTTP/JWT
         ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                     Outros Microsserviços (APIs)                             │
│                                                                              │
│  • API de Usuários (autenticação, autorização)                              │
│  • API de Jogos (catálogo de jogos, preços)                                 │
│  • API de Notificações (emails, push notifications)                         │
│  • API de Analytics (processamento de eventos)                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

## 📊 Fluxo de Dados - Criar e Processar Pagamento

```
┌─────────────┐
│   Cliente   │ POST /api/payments
└──────┬──────┘ {gameId, paymentMethod}
       │ JWT Token
       ▼
┌────────────────────────────────────────┐
│   API Pagamentos (HTTP Endpoint)      │
├────────────────────────────────────────┤
│ 1. Validar JWT                        │
│ 2. Verificar jogo via API de Jogos    │
│ 3. Criar entidade Payment (Pending)   │
│ 4. Salvar no DB                       │
└────────────────────────────────────────┘
       │
       ▼
┌────────────────────────────────────────┐
│   Event Sourcing (Event Store)        │
├────────────────────────────────────────┤
│ Salvar: PaymentCreatedEvent           │
└────────────────────────────────────────┘
       │
       ▼
┌────────────────────────────────────────┐
│   RabbitMQ / MassTransit              │
├────────────────────────────────────────┤
│ Exchange: PaymentCreatedEvent          │
│ Queue: FIAP.CloudGames.Pagamento...   │
│ Routing: Broadcast                    │
└────────────────────────────────────────┘
       │
       ▼
┌────────────────────────────────────────┐
│ Consumer: PaymentCreatedEventConsumer │
├────────────────────────────────────────┤
│ • Log do evento                       │
│ • Validação adicional                 │
│ • Integração com anti-fraude          │
│ • Enviar notificação inicial          │
└────────────────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────────────────────────────┐
│  Cliente solicita: POST /api/payments/{id}/process        │
│  JWT Token                                                  │
└─────────────────────────────────────────────────────────────┘
       │
       ▼
┌────────────────────────────────────────┐
│   API Pagamentos (HTTP Endpoint)      │
├────────────────────────────────────────┤
│ 1. Validar JWT                        │
│ 2. Encontrar pagamento                │
│ 3. Marcar como Processing             │
│ 4. Chamar serviço de processamento    │
│ 5. Se sucesso: Completed              │
│    Se falha: Failed                   │
└────────────────────────────────────────┘
       │
       ▼
┌────────────────────────────────────────┐
│   Event Sourcing (Event Store)        │
├────────────────────────────────────────┤
│ Salvar: PaymentProcessingEvent        │
│ Salvar: PaymentCompletedEvent         │
│ Salvar: GamePurchasedEvent            │
│         (ou PaymentFailedEvent)       │
└────────────────────────────────────────┘
       │
       ▼
┌────────────────────────────────────────┐
│   RabbitMQ / MassTransit (3 eventos)  │
├────────────────────────────────────────┤
│ • PaymentProcessingEvent              │
│ • PaymentCompletedEvent               │
│ • GamePurchasedEvent                  │
└────────────────────────────────────────┘
       │
       ├─────────┬──────────┬────────────┐
       ▼         ▼          ▼            ▼
    ┌───┐   ┌───────┐  ┌──────┐  ┌──────────┐
    │Pay│   │Game   │  │Not.  │  │Analytics │
    │Com│   │Updated│  │User  │  │(opcional)│
    │Ctd│   │       │  │      │  │          │
    └───┘   └───────┘  └──────┘  └──────────┘
```

## 🔄 Padrões de Resiliência

### 1. **Separação de Responsabilidades**
- ✅ Endpoint `POST /api/payments`: Apenas recebe e persiste
- ✅ Endpoint `POST /api/payments/{id}/process`: Processa assincronamente
- ✅ Consumers: Reagem aos eventos em background

### 2. **Garantia de Entrega (RabbitMQ)**
```
Mensagem Enviada
    │
    ├─ Acknowledged (Consumidor processou)
    │   └─ Sucesso: Mensagem removida da fila
    │
    └─ Rejected (Erro no processador)
        └─ Retry automático (3 tentativas)
            ├─ Sucesso na retry: Removida
            └─ Falha em todas: Dead Letter Queue (DLQ)
```

### 3. **Idempotência**
Todos os consumers devem ser idempotentes:
- Mesmo evento processado 2x = mesmo resultado
- Use PaymentId como chave para deduplicação
- Verificar se já foi processado antes de executar lógica

### 4. **Monitoramento**
```
Logs da Aplicação
    ↓
Serilog + Application Insights
    ↓
OpenTelemetry (Traces)
    ↓
RabbitMQ Management UI (Mensagens)
```

## 📦 Estrutura de Arquivos

```
FIAP.CloudGames.Pagamento.API/
├── docker-compose.yml                    # RabbitMQ + SQL Server local
├── test-rabbitmq.sh                      # Script de teste
├── TESTING_RABBITMQ_LOCAL.md             # Guia completo de teste
├── Postman_Collection.json               # Collection para testar
│
└── src/
    ├── Program.cs                        # Endpoints HTTP
    │
    ├── Application/
    │   ├── DTOs/
    │   │   └── PaymentDto.cs
    │   ├── Middleware/
    │   │   └── ErrorHandlingMiddleware.cs
    │   └── Validators/
    │       └── CreatePaymentDtoValidator.cs
    │
    ├── Domain/
    │   ├── Entities/
    │   │   ├── Payment.cs                # Entidade de domínio
    │   │   └── UserGame.cs
    │   └── Events/
    │       └── PaymentEvents.cs          # Eventos de domínio
    │
    └── Infrastructure/
        ├── Configurations/
        │   └── DependencyResolverConfigurationExtensions.cs
        ├── Consumers/                    # 🆕 Consumers do RabbitMQ
        │   ├── PaymentCreatedEventConsumer.cs
        │   ├── PaymentCompletedEventConsumer.cs
        │   ├── PaymentFailedEventConsumer.cs
        │   └── GamePurchasedEventConsumer.cs
        ├── Data/
        │   └── ApplicationDbContext.cs
        ├── EventSourcing/
        │   └── EventStore.cs
        └── Services/
            ├── ForwardAuthTokenHandler.cs
            ├── GameApiService.cs
            └── PaymentProcessingService.cs
```

## 🔐 Considerações de Segurança

1. **Autenticação JWT**: Todos os endpoints exceto `/health` requerem token
2. **Validação de entrada**: FluentValidation em todos os DTOs
3. **Autorização**: Usuário só vê seus próprios pagamentos
4. **Logging**: Sem dados sensíveis nos logs (não logar senhas/tokens)
5. **Encryption**: Dados sensíveis criptografados em trânsito (HTTPS em prod)

---

**Versão**: 1.0  
**Data**: Janeiro 2026  
**Status**: Pronto para testes locais
