#!/usr/bin/env bash
set -euo pipefail

export PGPASSWORD="${BILLINGTRACKER_DB_PASSWORD:-billing}"

psql \
  --host="${BILLINGTRACKER_DB_HOST:-db}" \
  --port="${BILLINGTRACKER_DB_PORT:-5432}" \
  --username="${BILLINGTRACKER_DB_USER:-billing}" \
  --dbname="${BILLINGTRACKER_DB_NAME:-billingtracker}" \
  --file="sql/00_create_schema.sql"

if [ -f "sql/seed.sql" ]; then
  psql \
    --host="${BILLINGTRACKER_DB_HOST:-db}" \
    --port="${BILLINGTRACKER_DB_PORT:-5432}" \
    --username="${BILLINGTRACKER_DB_USER:-billing}" \
    --dbname="${BILLINGTRACKER_DB_NAME:-billingtracker}" \
    --file="sql/seed_data_inserts.sql"
fi
