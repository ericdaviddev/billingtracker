#!/usr/bin/env bash
set -euo pipefail

DROP SCHEMA IF EXISTS billing CASCADE;
CREATE SCHEMA billing;
CREATE EXTENSION IF NOT EXISTS pgcrypto;

export PGPASSWORD="${BILLINGTRACKER_DB_PASSWORD:-billing}"

DB_HOST="${BILLINGTRACKER_DB_HOST:-db}"
DB_PORT="${BILLINGTRACKER_DB_PORT:-5432}"
DB_NAME="${BILLINGTRACKER_DB_NAME:-billingtracker}"
DB_USER="${BILLINGTRACKER_DB_USER:-billing}"

run_sql() {
  local file="$1"

  if [ ! -f "$file" ]; then
    echo "Missing SQL file: $file"
    exit 1
  fi

  echo "Running $file"

  psql \
    --set ON_ERROR_STOP=on \
    --host="$DB_HOST" \
    --port="$DB_PORT" \
    --username="$DB_USER" \
    --dbname="$DB_NAME" \
    --file="$file"
}

run_sql "sql/00_create_schema.sql"
run_sql "sql/billing_create_tables.sql"
run_sql "sql/seed_data_inserts.sql"

echo "Database reset complete."
EOF

chmod +x scripts/reset-db.sh