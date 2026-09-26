#!/bin/sh
set -e

echo "Waiting for Vault to be ready at $VAULT_ADDR..."
until vault status > /dev/null 2>&1; do
  echo "Vault is not ready yet, retrying in 1s..."
  sleep 1
done

echo "Vault is online. Seeding initial secrets for ShuffleSeries microservices..."

# 1. Shared Infrastructure Secrets (RabbitMQ, Redis, JWT)
vault kv put -mount=secret shuffleseries/shared \
  "MessageBroker:Host=localhost" \
  "MessageBroker:Port=5672" \
  "MessageBroker:Username=${RABBITMQ_USER:-guest_123}" \
  "MessageBroker:Password=${RABBITMQ_PASSWORD:-guest_123}" \
  "Redis:Password=${REDIS_PASSWORD:-SuperSecretRedisPassword2026!!}" \
  "Jwt:Secret=SuperSecretSecretKeyForJwtAuthenticationTokens2026!!"

# 2. Catalog Service Secrets (PostgreSQL Connection String)
vault kv put -mount=secret shuffleseries/catalog \
  "ConnectionStrings:Database=Host=localhost;Port=5432;Database=${POSTGRES_DB_NAME:-shuffleseries_db};Username=${POSTGRES_DB_USER:-admin};Password=${POSTGRES_DB_PASSWORD:-SuperSecretSecurePassword2026!!};Maximum Pool Size=50;"

echo "Vault initial secrets seeded successfully."
