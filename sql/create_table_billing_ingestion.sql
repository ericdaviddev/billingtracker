create table billing.ingestion_runs (
    ingestion_run_id uuid primary key default gen_random_uuid(),

    client_id uuid not null references billing.clients(client_id),
    source_system_id uuid not null references billing.source_systems(source_system_id),

    run_type text not null, -- full_sync, incremental_sync, reconciliation

    started_at timestamptz not null default now(),
    completed_at timestamptz,

    status text not null, -- running, completed, failed, partial

    records_received integer default 0,
    records_inserted integer default 0,
    records_updated integer default 0,
    records_failed integer default 0,

    cursor_value text,
    error_message text
);