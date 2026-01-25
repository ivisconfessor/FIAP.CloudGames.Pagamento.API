# Guia de Teste Local - RabbitMQ com Pagamentos

## 📋 Pré-requisitos

- Docker e Docker Compose instalados
- .NET 8 SDK instalado
- VS Code ou IDE C# compatível

## 🚀 Passo 1: Iniciar os Serviços Docker

```bash
# Na raiz do projeto
docker-compose up -d

# Verificar status dos serviços
docker-compose ps

# Logs em tempo real
docker-compose logs -f rabbitmq
```

**Serviços iniciados:**
- **RabbitMQ**: `http://localhost:15672` (user: `guest`, password: `guest`)
- **SQL Server**: `localhost:1433` (sa/YourPassword@123) *(opcional, se configurado)*

## 🔍 Passo 2: Acessar RabbitMQ Management

1. Abra o navegador: `http://localhost:15672`
2. Login com `guest/guest`
3. Navegue para **Queues** e **Exchanges** para monitorar as mensagens

## 🏃 Passo 3: Executar a API

```bash
# Na pasta src/
cd src
dotnet run

# Ou via VS Code: Pressione F5
```

A API estará disponível em: `http://localhost:5000` (ou conforme configurado)

## 📝 Passo 4: Testar o Fluxo de Pagamentos

### 4.1 Autenticação (obter token JWT)

Primeiro, você precisa de um token JWT válido. Para testes locais, você pode:

1. Contatar o time da API de Usuários para gerar um token
2. Usar Postman para simular a autenticação

**Exemplo de token Mock** (substitua pelo seu token real):
```
Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 4.2 Criar um Pagamento

**Request:**
```bash
curl -X POST http://localhost:5000/api/payments \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "gameId": "550e8400-e29b-41d4-a716-446655440000",
    "paymentMethod": "creditcard"
  }'
```

**Resposta esperada:**
```json
{
  "id": "payment-id-uuid",
  "userId": "user-id-uuid",
  "gameId": "game-id-uuid",
  "amount": 99.90,
  "status": "Pending",
  "method": "creditcard",
  "transactionId": null,
  "errorMessage": null,
  "createdAt": "2026-01-25T10:00:00Z",
  "processedAt": null
}
```

### 4.3 Observar o Evento no RabbitMQ

1. Vá para `http://localhost:15672`
2. Clique em **Queues**
3. Você verá uma queue criada automaticamente (ex: `FIAP.CloudGames.Pagamento.API:PaymentCreatedEvent`)
4. Veja o **Count** aumentar (mensagens na fila)

### 4.4 Processar o Pagamento

**Request:**
```bash
curl -X POST http://localhost:5000/api/payments/{payment-id}/process \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

## 🔄 Monitoramento em Tempo Real

### Ver logs da API
```bash
# Terminal 1 - Logs da aplicação
dotnet run
```

### Ver logs do RabbitMQ
```bash
# Terminal 2
docker-compose logs -f rabbitmq
```

### Monitorar fila do RabbitMQ
```bash
# Terminal 3
docker-compose exec rabbitmq rabbitmqctl list_queues name messages consumers
```

## 📊 Fluxo Esperado de Eventos

```
1. POST /api/payments
   ↓
   → Salva pagamento no DB (Status: Pending)
   → Publica PaymentCreatedEvent para RabbitMQ
   ↓
   RabbitMQ → PaymentCreatedEventConsumer (processa em background)

2. POST /api/payments/{id}/process
   ↓
   → Muda status para Processing
   → Publica PaymentProcessingEvent
   ↓
   RabbitMQ → PaymentProcessingEventConsumer

3. Se sucesso:
   → Publica PaymentCompletedEvent + GamePurchasedEvent
   ↓
   RabbitMQ → PaymentCompletedEventConsumer + GamePurchasedEventConsumer

4. Se falha:
   → Publica PaymentFailedEvent
   ↓
   RabbitMQ → PaymentFailedEventConsumer
```

## ✅ Verificar Funcionamento

### 1. Logs esperados na aplicação:
```
Criando novo pagamento para usuário {UserId} e jogo {GameId}
Pagamento criado com sucesso. ID: {PaymentId}
Processando pagamento {PaymentId}
Evento PaymentCreated recebido. PaymentId: ...
Pagamento {PaymentId} processado com sucesso no consumer
```

### 2. No RabbitMQ Management:
- ✅ Novas queues criadas automaticamente
- ✅ Mensagens sendo processadas (count reduzindo)
- ✅ Nenhuma mensagem na Dead Letter Queue (DLQ)

### 3. Resiliência:
```bash
# Parar RabbitMQ
docker-compose stop rabbitmq

# Fazer request POST /api/payments
# Mensagem será retentada automaticamente

# Reiniciar RabbitMQ
docker-compose start rabbitmq

# Mensagens serão processadas automaticamente
```

## 🐛 Troubleshooting

### RabbitMQ não inicia
```bash
docker-compose down
docker volume prune
docker-compose up -d
```

### Conexão recusada na porta 5672
```bash
# Verificar se RabbitMQ está rodando
docker-compose ps

# Ver logs
docker-compose logs rabbitmq
```

### Consumers não recebem mensagens
1. Verifique se os consumers estão registrados em `DependencyResolverConfigurationExtensions.cs`
2. Verifique `Program.cs` para garantir que `app.Run()` está sendo executado
3. Veja os logs para erros de configuração

### Mensagens na Dead Letter Queue
1. Vá para RabbitMQ Management
2. Acesse a queue Dead Letter
3. Analise o erro na mensagem rejeitada
4. Corrija o consumer e reinicie a API

## 🛑 Parar Tudo

```bash
# Parar containers sem deletar dados
docker-compose stop

# Parar e remover tudo (apaga dados)
docker-compose down
```

## 📚 Próximos Passos

Após testar localmente com sucesso:

1. **Configurar em Staging**: Usar managed RabbitMQ no Google Cloud (Cloud Pub/Sub ou similar)
2. **Implementar Dead Letter Queue (DLQ)**: Já configurado, mas adicione handlers customizados
3. **Adicionar métricas**: Integrar com Prometheus/Grafana
4. **Testes de carga**: Simular múltiplos pagamentos simultâneos
5. **Sincronização com outros microsserviços**: Criar consumers em Jogo e Usuário APIs

---

**Dúvidas?** Consulte os logs e o RabbitMQ Management para diagnosticar!
