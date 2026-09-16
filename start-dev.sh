#!/bin/bash
set -e

# Configuration
ENV_FILE=".env.dev"
SERVICES="db redis api_public api_admin"

if [ ! -f "$ENV_FILE" ]; then
  echo "### Error: $ENV_FILE not found."
  exit 1
fi

echo "### Building and starting services ($SERVICES)..."
docker compose --env-file "$ENV_FILE" up --build -d $SERVICES

echo "### Waiting for postgres_db to be healthy..."
until [ "$(docker inspect -f '{{.State.Health.Status}}' postgres_db 2>/dev/null)" = "healthy" ]; do
  sleep 2
done
echo "### postgres_db is healthy."

echo "### Waiting for APIs to apply migrations and become healthy..."
sleep 5
curl -is http://localhost:9000/health/ready | head -1
curl -is http://localhost:9001/health | head -1

cat <<EOF

### Done!
API Public : http://localhost:9000 : http://localhost:9001
PostgreSQL : localhost:5432
Redis      : localhost:6379

Logs: docker compose logs -f api_public api_admin
Stop: docker compose stop $SERVICES
EOF
