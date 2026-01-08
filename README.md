# FIAP Cloud Games - Microsserviço de Pagamentos

Microsserviço responsável pelo processamento de pagamentos e gerenciamento de compras de jogos da plataforma FIAP Cloud Games.

## 🚀 Funcionalidades

- **Processamento de Pagamentos**: Criação e processamento assíncrono de pagamentos
- **Múltiplos Métodos de Pagamento**: Suporte para cartão de crédito, débito, PIX, boleto e PayPal
- **Gerenciamento de Compras**: Registro e listagem de jogos comprados por usuário
- **Analytics de Receita**: Métricas de receita e distribuição por método de pagamento
- **Event Sourcing**: Registro completo de todos os eventos de pagamento
- **Observabilidade**: Logs estruturados e rastreamento distribuído

## 🏗️ Arquitetura

Este microsserviço segue os princípios de:

- **Domain-Driven Design (DDD)**
- **Clean Architecture**
- **Event Sourcing** para auditoria completa
- **Processamento Assíncrono** de pagamentos
- **Observabilidade** com traces distribuídos

## 📋 Endpoints

### Protegidos (requer autenticação)

#### Pagamentos
- `POST /api/payments` - Criar novo pagamento
- `POST /api/payments/{id}/process` - Processar pagamento
- `GET /api/payments/{id}` - Obter pagamento por ID
- `GET /api/payments` - Listar pagamentos do usuário

#### Compras
- `GET /api/users/games` - Listar jogos comprados pelo usuário

#### Analytics
- `GET /api/payments/analytics/revenue` - Obter métricas de receita

### Públicos
- `GET /api/health` - Health check do serviço
- `GET /api/events/{aggregateId}` - Obter eventos de pagamento (Autenticado)

## 🔧 Tecnologias Utilizadas

- **.NET 8.0**
- **Entity Framework Core** (In-Memory Database)
- **JWT Bearer Authentication**
- **FluentValidation** para validação de entrada
- **Serilog** para logging estruturado
- **OpenTelemetry** para observabilidade
- **HttpClient** para comunicação entre microsserviços
- **Swagger/OpenAPI** para documentação

## 🏃 Como Executar

### Pré-requisitos

- .NET 8.0 SDK

### Executar localmente

```bash
cd src
dotnet restore
dotnet run
```

A API estará disponível em:
- HTTP: http://localhost:5003
- HTTPS: https://localhost:7003
- Swagger: http://localhost:5003/swagger

### Executar com Docker

```bash
docker build -t fiap-cloudgames-pagamento-api .
docker run -p 5003:80 fiap-cloudgames-pagamento-api
```

## 💳 Métodos de Pagamento

O microsserviço suporta os seguintes métodos:

- **CreditCard** - Cartão de Crédito
- **DebitCard** - Cartão de Débito
- **Pix** - PIX
- **Boleto** - Boleto Bancário
- **PayPal** - PayPal

## 🔄 Fluxo de Pagamento

1. **Criação**: Usuário cria um pagamento informando o jogo e método de pagamento
2. **Validação**: Sistema valida se o jogo existe e se o usuário ainda não o possui
3. **Processamento**: Pagamento é processado de forma assíncrona
4. **Finalização**: 
   - Sucesso: Jogo é adicionado à biblioteca do usuário
   - Falha: Pagamento é marcado como falho com mensagem de erro

## 📊 Status de Pagamento

- **Pending** - Aguardando processamento
- **Processing** - Em processamento
- **Completed** - Pagamento concluído com sucesso
- **Failed** - Pagamento falhou
- **Cancelled** - Pagamento cancelado

## 📊 Event Sourcing

Todos os eventos relacionados a pagamentos são registrados:

- `PaymentCreatedEvent` - Quando um pagamento é criado
- `PaymentProcessingEvent` - Quando um pagamento entra em processamento
- `PaymentCompletedEvent` - Quando um pagamento é concluído
- `PaymentFailedEvent` - Quando um pagamento falha
- `GamePurchasedEvent` - Quando um jogo é comprado com sucesso

Os eventos podem ser consultados através do endpoint `/api/events/{aggregateId}`.

## 🔍 Observabilidade

### Logs

Logs estruturados são gerados com Serilog, incluindo:
- Informações de requisição
- Eventos de negócio
- Processos de pagamento
- Erros e exceções

### Traces

OpenTelemetry é utilizado para rastreamento distribuído, permitindo:
- Rastreamento de requisições entre microsserviços
- Análise de performance de processamento de pagamentos
- Identificação de gargalos

## 🌐 Integração com outros Microsserviços

Este microsserviço se comunica com:

- **FIAP.CloudGames.Usuario.API** (porta 5001) - Para autenticação e autorização
- **FIAP.CloudGames.Jogo.API** (porta 5002) - Para validar jogos e obter preços

As URLs são configuráveis através do `appsettings.json`:

```json
"ServiceUrls": {
  "UsuarioAPI": "http://localhost:5001",
  "JogoAPI": "http://localhost:5002"
}
```

## 💰 Analytics de Receita

O endpoint de analytics fornece:

- **Receita Total** de pagamentos concluídos
- **Distribuição por Status** (pending, processing, completed, failed)
- **Distribuição por Método de Pagamento** com totais e contagens

Exemplo de resposta:

```json
{
  "totalRevenue": 1249.50,
  "paymentsByStatus": [
    { "status": "Completed", "count": 15 },
    { "status": "Failed", "count": 2 }
  ],
  "paymentsByMethod": [
    { "method": "CreditCard", "count": 10, "total": 899.50 },
    { "method": "Pix", "count": 5, "total": 350.00 }
  ]
}
```

## 🔐 Segurança

- Autenticação via JWT Bearer tokens
- Validação de propriedade de pagamentos
- Validação de duplicidade de compras
- Logs de todas as tentativas de acesso

## 📝 Licença

Este projeto é parte do Tech Challenge da FIAP - Pós-Tech.
