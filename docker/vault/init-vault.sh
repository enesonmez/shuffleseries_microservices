#!/bin/sh
set -e

echo "Waiting for Vault to be ready at $VAULT_ADDR..."
until vault status > /dev/null 2>&1; do
  echo "Vault is not ready yet, retrying in 1s..."
  sleep 1
done

echo "Vault is online. Seeding initial secrets for ShuffleSeries microservices..."

# 1. Shared Infrastructure Secrets & Runtime Config (RabbitMQ, Redis, JWT, Logging Sinks, OpenTelemetry, CORS)
vault kv put -mount=secret shuffleseries/shared \
  "MessageBroker:Host=rabbitmq" \
  "MessageBroker:Port=5672" \
  "MessageBroker:Username=${RABBITMQ_USER:-guest_123}" \
  "MessageBroker:Password=${RABBITMQ_PASSWORD:-guest_123}" \
  "Redis:Password=${REDIS_PASSWORD:-SuperSecretRedisPassword2026!!}" \
  "Jwt:Secret=SuperSecretSecretKeyForJwtAuthenticationTokens2026!!" \
  "Jwt:Issuer=ShuffleSeries.Identity" \
  "Jwt:Audience=ShuffleSeries.Microservices" \
  "Logging:Sinks:Console:Enabled=true" \
  "Logging:Sinks:Console:UseJsonFormat=true" \
  "Logging:Sinks:File:Enabled=false" \
  "Logging:Sinks:File:Path=logs/shuffleseries-.json" \
  "Logging:Sinks:File:UseJsonFormat=true" \
  "Logging:Sinks:PostgreSql:Enabled=false" \
  "Logging:Sinks:PostgreSql:TableName=AppLogs" \
  "OpenTelemetry:Enabled=true" \
  "OpenTelemetry:OtlpEndpoint=http://otel-collector:4317" \
  "Cors:AllowedOrigins=*"

# 2. Catalog Service Specific Secrets & Config
vault kv put -mount=secret shuffleseries/catalog \
  "ConnectionStrings:Database=Host=postgres;Port=5432;Database=${POSTGRES_DB_NAME:-shuffleseries_db};Username=${POSTGRES_DB_USER:-admin};Password=${POSTGRES_DB_PASSWORD:-SuperSecretSecurePassword2026!!};Maximum Pool Size=50;" \
  "Logging:Sinks:File:Path=logs/catalog-.json"

# 3. API Gateway Specific Config
vault kv put -mount=secret shuffleseries/apigateway \
  "Logging:Sinks:File:Path=logs/apigateway-.json"

echo "Vault initial secrets and configuration seeded successfully."
