#!/bin/sh
set -e

echo "Waiting for Vault to be ready at $VAULT_ADDR..."
until vault status > /dev/null 2>&1; do
  echo "Vault is not ready yet, retrying in 1s..."
  sleep 1
done

echo "Vault is online. Seeding initial secrets for ShuffleSeries microservices from environment..."

# 1. Shared Infrastructure Secrets & Runtime Config (RabbitMQ, Redis, JWT, Logging Sinks, OpenTelemetry, CORS)
vault kv put -mount=secret shuffleseries/shared \
  "MessageBroker:Host=${RABBITMQ_HOST:-rabbitmq}" \
  "MessageBroker:Port=${RABBITMQ_PORT:-5672}" \
  "MessageBroker:Username=${RABBITMQ_USER:-guest_123}" \
  "MessageBroker:Password=${RABBITMQ_PASSWORD:-guest_123}" \
  "Redis:Host=${REDIS_HOST:-redis}" \
  "Redis:Port=${REDIS_PORT:-6379}" \
  "Redis:Password=${REDIS_PASSWORD:-SuperSecretRedisPassword2026!!}" \
  "Redis:InstanceName=${REDIS_INSTANCE_NAME:-shuffleseries:}" \
  "Jwt:Secret=${JWT_SECRET:-SuperSecretSecretKeyForJwtAuthenticationTokens2026!!}" \
  "Jwt:Issuer=${JWT_ISSUER:-ShuffleSeries.Identity}" \
  "Jwt:Audience=${JWT_AUDIENCE:-ShuffleSeries.Microservices}" \
  "Jwt:ExpirationMinutes=${JWT_EXPIRATION_MINUTES:-60}" \
  "Logging:Sinks:Console:Enabled=${LOGGING_CONSOLE_ENABLED:-true}" \
  "Logging:Sinks:Console:UseJsonFormat=${LOGGING_CONSOLE_JSON:-true}" \
  "Logging:Sinks:File:Enabled=${LOGGING_FILE_ENABLED:-false}" \
  "Logging:Sinks:File:Path=${LOGGING_FILE_PATH:-logs/shuffleseries-.json}" \
  "Logging:Sinks:File:UseJsonFormat=${LOGGING_FILE_JSON:-true}" \
  "Logging:Sinks:PostgreSql:Enabled=${LOGGING_POSTGRESQL_ENABLED:-false}" \
  "Logging:Sinks:PostgreSql:TableName=${LOGGING_POSTGRESQL_TABLE:-AppLogs}" \
  "OpenTelemetry:Enabled=${OTEL_ENABLED:-true}" \
  "OpenTelemetry:OtlpEndpoint=${OTEL_EXPORTER_OTLP_ENDPOINT:-http://otel-collector:4317}" \
  "Cors:AllowedOrigins=${CORS_ALLOWED_ORIGINS:-*}"

# 2. Catalog Service Specific Secrets & Config
vault kv put -mount=secret shuffleseries/catalog \
  "ConnectionStrings:Database=Host=${POSTGRES_HOST:-postgres};Port=${POSTGRES_PORT:-5432};Database=${POSTGRES_DB_NAME:-shuffleseries_db};Username=${POSTGRES_DB_USER:-admin};Password=${POSTGRES_DB_PASSWORD:-SuperSecretSecurePassword2026!!};Maximum Pool Size=50;" \
  "Logging:Sinks:File:Path=logs/catalog-.json"

# 3. API Gateway Specific Config
vault kv put -mount=secret shuffleseries/apigateway \
  "Logging:Sinks:File:Path=logs/apigateway-.json"

# 4. Identity Service Specific Secrets & Config
vault kv put -mount=secret shuffleseries/identity \
  "ConnectionStrings:Database=Host=${POSTGRES_HOST:-postgres};Port=${POSTGRES_PORT:-5432};Database=${POSTGRES_IDENTITY_DB_NAME:-shuffleseries_identity};Username=${POSTGRES_DB_USER:-admin};Password=${POSTGRES_DB_PASSWORD:-SuperSecretSecurePassword2026!!};Maximum Pool Size=50;" \
  "Logging:Sinks:File:Path=logs/identity-.json" \
  "ExternalAuth:ValidateSignatures=${EXTERNAL_AUTH_VALIDATE_SIGNATURES:-false}" \
  "ExternalAuth:Google:ClientId=${GOOGLE_CLIENT_ID:-}" \
  "ExternalAuth:Apple:ClientId=${APPLE_CLIENT_ID:-}"

echo "Vault initial secrets and configuration seeded successfully."
