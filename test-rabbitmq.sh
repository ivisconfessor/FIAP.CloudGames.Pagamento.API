#!/bin/bash

# Script para testar RabbitMQ localmente
# Uso: ./test-rabbitmq.sh

set -e

echo "🚀 Iniciando testes de RabbitMQ para FIAP Cloud Games Pagamentos API"
echo "=================================================================="

# Cores para output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Variáveis
DOCKER_COMPOSE_FILE="docker-compose.yml"
API_URL="http://localhost:8080"
RABBITMQ_MANAGEMENT="http://localhost:15672"
RABBITMQ_USER="guest"
RABBITMQ_PASS="guest"

# Função para imprimir sucesso
success() {
    echo -e "${GREEN}✓ $1${NC}"
}

# Função para imprimir erro
error() {
    echo -e "${RED}✗ $1${NC}"
}

# Função para imprimir aviso
warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

# 1. Verificar Docker
echo ""
echo "1️⃣  Verificando Docker..."
if ! command -v docker &> /dev/null; then
    error "Docker não está instalado!"
    exit 1
fi
success "Docker encontrado"

# 2. Verificar Docker Compose
echo ""
echo "2️⃣  Verificando Docker Compose..."
if ! command -v docker-compose &> /dev/null; then
    error "Docker Compose não está instalado!"
    exit 1
fi
success "Docker Compose encontrado"

# 3. Iniciar containers
echo ""
echo "3️⃣  Iniciando containers..."
docker-compose down 2>/dev/null || true
docker-compose up -d
success "Containers iniciados"

# 4. Esperar RabbitMQ estar pronto
echo ""
echo "4️⃣  Aguardando RabbitMQ estar pronto..."
max_attempts=30
attempt=0
while [ $attempt -lt $max_attempts ]; do
    if docker-compose exec -T rabbitmq rabbitmq-diagnostics -q ping &>/dev/null; then
        success "RabbitMQ está pronto!"
        break
    fi
    echo "   Tentativa $((attempt+1))/$max_attempts..."
    sleep 2
    attempt=$((attempt+1))
done

if [ $attempt -eq $max_attempts ]; then
    error "RabbitMQ não respondeu após $max_attempts tentativas"
    exit 1
fi

# 5. Verificar conexão com API
echo ""
echo "5️⃣  Aguardando API estar pronta..."
warning "Pressione F5 ou execute 'dotnet run' em outro terminal na pasta 'src/'"
warning "Aguardando API responder em ${API_URL}..."

max_attempts=30
attempt=0
while [ $attempt -lt $max_attempts ]; do
    if curl -s "${API_URL}/api/health" &>/dev/null; then
        success "API está pronta!"
        break
    fi
    echo "   Tentativa $((attempt+1))/$max_attempts..."
    sleep 2
    attempt=$((attempt+1))
done

if [ $attempt -eq $max_attempts ]; then
    warning "API não respondeu. Inicie-a manualmente com: dotnet run"
fi

# 6. Informações de acesso
echo ""
echo "=================================================================="
echo -e "${GREEN}✓ Ambiente de teste preparado!${NC}"
echo "=================================================================="
echo ""
echo "📊 RabbitMQ Management:"
echo "   URL: ${RABBITMQ_MANAGEMENT}"
echo "   User: ${RABBITMQ_USER}"
echo "   Password: ${RABBITMQ_PASS}"
echo ""
echo "🚀 API:"
echo "   URL: ${API_URL}"
echo "   Health: ${API_URL}/api/health"
echo "   Swagger: ${API_URL}/swagger"
echo ""
echo "📋 Próximos passos:"
echo "   1. Abra ${RABBITMQ_MANAGEMENT} no navegador"
echo "   2. Vá para 'Queues' para monitorar mensagens"
echo "   3. Use Postman com a collection: Postman_Collection.json"
echo "   4. Crie um pagamento via POST ${API_URL}/api/payments"
echo "   5. Processe o pagamento via POST ${API_URL}/api/payments/{id}/process"
echo ""
echo "📚 Documentação completa em: TESTING_RABBITMQ_LOCAL.md"
echo ""
echo "🛑 Para parar os containers:"
echo "   docker-compose down"
echo ""
echo "🗑️  Para limpar tudo (incluindo volumes):"
echo "   docker-compose down -v"
echo ""
