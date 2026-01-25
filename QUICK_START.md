# ⚡ Quick Start - RabbitMQ Local

> Comece a testar RabbitMQ em 5 minutos!

## 🚀 Início Rápido

### 1️⃣ Pré-requisitos (1 min)
```bash
# Verificar instalação
docker --version
docker-compose --version
dotnet --version  # 8.0+
```

### 2️⃣ Iniciar Infraestrutura (2 min)
```bash
cd FIAP.CloudGames.Pagamento.API

# Opção A: Script automatizado (recomendado)
chmod +x test-rabbitmq.sh
./test-rabbitmq.sh

# Opção B: Manual
docker-compose up -d
sleep 15  # Aguardar RabbitMQ
```

### 3️⃣ Executar a API (1 min)
```bash
# Terminal novo
cd src
dotnet run
# Aguardar: "Now listening on: http://localhost:5000"
```

### 4️⃣ Testar no Navegador (1 min)
```
✅ Health Check:
   http://localhost:5000/api/health

✅ RabbitMQ Management:
   http://localhost:15672
   User: guest
   Pass: guest

✅ Swagger da API:
   http://localhost:5000/swagger
```

---

## 📋 Testar com cURL

### Criar Pagamento
```bash
# Nota: Substitua YOUR_JWT_TOKEN por um token válido
curl -X POST http://localhost:5000/api/payments \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "gameId": "550e8400-e29b-41d4-a716-446655440000",
    "paymentMethod": "creditcard"
  }'

# Resposta (salve o ID):
# {
#   "id": "abc-123-def",
#   "status": "Pending",
#   ...
# }
```

### Processar Pagamento
```bash
curl -X POST http://localhost:5000/api/payments/abc-123-def/process \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## 👀 Monitorar em Tempo Real

### Terminal 1: Logs da API
```bash
cd src
dotnet run
# Veja em tempo real:
# [Information] Criando novo pagamento...
# [Information] Evento PaymentCreated recebido...
```

### Terminal 2: Logs do RabbitMQ
```bash
docker-compose logs -f rabbitmq
```

### Terminal 3: Fila do RabbitMQ
```bash
docker-compose exec rabbitmq rabbitmqctl list_queues name messages consumers
# Repita cada 10 segundos para ver mudanças
```

---

## 📊 RabbitMQ Management UI

Abra: http://localhost:15672 (guest/guest)

### Ver Queues
```
1. Clique em "Queues"
2. Procure por: FIAP.CloudGames.Pagamento.API:PaymentCreatedEvent
3. Veja o contador de mensagens aumentar
```

### Ver Mensagens
```
1. Clique na queue
2. Scroll para "Get messages"
3. Clique em "Get Message(s)"
4. Veja o JSON da mensagem
```

---

## ✅ Verificar se Está Funcionando

### Sinais de Sucesso ✅
- [x] API respondendo em `http://localhost:8080/api/health`
- [x] RabbitMQ acessível em `http://localhost:15672`
- [x] Queues aparecendo no RabbitMQ Management
- [x] Mensagens sendo processadas (contador diminui)
- [x] Logs da API mostrando eventos sendo recebidos
- [x] Nenhuma mensagem na Dead Letter Queue

### Se Não Funcionar ❌
```bash
# Reset total
docker-compose down -v
./test-rabbitmq.sh
dotnet run

# Ou ver logs detalhados
docker-compose logs -f
```

---

## 📚 Documentação Completa

Após validar que está funcionando, leia:

1. **[TESTING_RABBITMQ_LOCAL.md](TESTING_RABBITMQ_LOCAL.md)** - Guia detalhado
2. **[ARCHITECTURE.md](ARCHITECTURE.md)** - Diagramas e padrões
3. **[RABBITMQ_IMPLEMENTATION.md](RABBITMQ_IMPLEMENTATION.md)** - O que foi implementado

---

## 🛑 Parar Tudo

```bash
# Parar containers (mantém dados)
docker-compose stop

# Parar e limpar (apaga volumes)
docker-compose down -v

# Matar processo da API
# Ctrl+C no terminal do dotnet run
```

---

## 🎯 Próximos Passos

### Agora que funciona localmente:

1. **Testar com Postman**
   - Importe: `Postman_Collection.json`
   - Configure o JWT token em Environments
   - Execute os requests em sequência

2. **Implementar Lógica nos Consumers**
   - Abra `src/Infrastructure/Consumers/*.cs`
   - Veja os TODOs
   - Adicione sua lógica de negócio

3. **Integrar com Outros Microsserviços**
   - Crie consumers em API de Usuários
   - Crie consumers em API de Jogos
   - Sincronize dados em tempo real

4. **Migrar para Google Cloud**
   - Substitua RabbitMQ por Cloud Pub/Sub
   - Deploy no Cloud Run
   - Configure autoscaling

---

## 💡 Dicas

### Ver todas as queues
```bash
docker-compose exec rabbitmq rabbitmqctl list_queues
```

### Limpar uma queue
```bash
docker-compose exec rabbitmq rabbitmqctl purge_queue FIAP.CloudGames.Pagamento.API:PaymentCreatedEvent
```

### Ver consumers ativos
```bash
docker-compose exec rabbitmq rabbitmqctl list_consumers
```

### Aumentar log level
```bash
# Edit src/appsettings.Development.json
# Mude "Default": "Debug" para mais detalhes
```

---

**Pronto?** Comece agora com: `./test-rabbitmq.sh` 🎉

Dúvidas? Veja [TESTING_RABBITMQ_LOCAL.md](TESTING_RABBITMQ_LOCAL.md#troubleshooting)
