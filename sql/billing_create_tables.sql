-- ============================================================================
-- BillingTracker Multi-Tenant Ingestion Platform
-- Core Relational Schema
-- ============================================================================
--
-- Purpose:
--     This schema models a multi-tenant healthcare/billing ingestion platform
--     designed for practicing:
--
--         * relational schema design
--         * ingestion architecture
--         * canonical data modeling
--         * idempotent upserts
--         * operational observability
--         * reconciliation workflows
--         * reporting queries
--         * ETL-style synchronization patterns
--
-- Architectural Principles:
--
--     * Canonical internal UUID identities are separated from external IDs.
--     * Multi-tenant ownership is enforced through client_id.
--     * External source systems are modeled explicitly.
--     * Tables support future incremental sync and reconciliation workflows.
--     * Composite unique constraints support idempotent ingestion.
--
-- ============================================================================
-- Prerequisites
-- ============================================================================
--
-- Required before running this script:
--
-- create schema if not exists billing;
-- create extension if not exists pgcrypto;
--
-- pgcrypto is required for gen_random_uuid().
-- ============================================================================


-- ============================================================================
-- billing.clients
-- ============================================================================
-- Purpose:
--     Represents tenant organizations/customers using the platform.
--
-- Depends On:
--     pgcrypto extension
-- ============================================================================

create table billing.clients
(
    client_id   uuid                     default gen_random_uuid() not null
        primary key,
    client_name text                                               not null,

    -- Standard operational auditing timestamps.
    created_at  timestamp with time zone default now()             not null,
    updated_at  timestamp with time zone default now()             not null
);

alter table billing.clients
    owner to billing;


-- ============================================================================
-- billing.locations
-- ============================================================================
-- Purpose:
--     Represents physical or operational locations owned by a tenant.
--
-- Architectural Notes:
--     managed_by_platform supports filtering/reporting scenarios where
--     only platform-managed locations participate in workflows.
--
-- Depends On:
--     billing.clients
-- ============================================================================

create table billing.locations
(
    location_id         uuid                     default gen_random_uuid() not null
        primary key,

    location_name       text                                               not null,

    client_id           uuid                                               not null
        constraint locations_clients_client_id_fk
            references billing.clients,

    managed_by_platform boolean                  default true              not null,

    created_at          timestamp with time zone default now()             not null,
    updated_at          timestamp with time zone default now()             not null
);

alter table billing.locations
    owner to billing;


-- ============================================================================
-- billing.source_systems
-- ============================================================================
-- Purpose:
--     Represents external systems/vendors integrated into the platform.
--
-- Examples:
--     Epic
--     AthenaHealth
--     Stripe
--     Legacy PMS systems
--
-- Depends On:
--     pgcrypto extension
-- ============================================================================

create table billing.source_systems
(
    source_system_id   uuid                     default gen_random_uuid() not null
        primary key,

    source_system_name text                                               not null,

    created_at         timestamp with time zone default now()             not null,
    updated_at         timestamp with time zone default now()             not null
);

alter table billing.source_systems
    owner to billing;


-- ============================================================================
-- billing.client_source_mappings
-- ============================================================================
-- Purpose:
--     Maps tenant organizations to external source-system identities.
--
-- Architectural Notes:
--     Supports scenarios where the same tenant has different identities
--     across multiple external systems.
--
-- Depends On:
--     billing.clients
--     billing.source_systems
-- ============================================================================

create table billing.client_source_mappings
(
    mapping_id         uuid                     default gen_random_uuid() not null
        primary key,

    client_id          uuid                                               not null
        constraint client_source_mappings_clients_client_id_fk
            references billing.clients,

    source_system_id   uuid                                               not null
        constraint client_source_mappings_source_systems_source_system_id_fk
            references billing.source_systems,

    external_client_id text                                               not null,

    created_at         timestamp with time zone default now()             not null,
    updated_at         timestamp with time zone default now()             not null,

    -- Composite uniqueness supports idempotent ingestion and prevents
    -- duplicate source-system mappings.
    constraint client_source_mappings_unique_constraint
        unique (client_id, source_system_id, external_client_id)
);

alter table billing.client_source_mappings
    owner to billing;


-- ============================================================================
-- billing.guarantors
-- ============================================================================
-- Purpose:
--     Represents financially responsible parties.
--
-- Architectural Notes:
--     Canonical internal IDs are intentionally separated from external IDs.
--     This enables multi-source synchronization and deduplication.
--
-- Depends On:
--     billing.clients
--     billing.source_systems
-- ============================================================================

create table billing.guarantors
(
    guarantor_id          uuid                     default gen_random_uuid() not null
        constraint guarantors_pk
            primary key,

    first_name            text                                               not null,
    last_name             text                                               not null,

    client_id             uuid                                               not null
        constraint guarantors_clients_client_id_fk
            references billing.clients,

    source_system_id      uuid                                               not null
        constraint guarantors_source_systems_source_system_id_fk
            references billing.source_systems,

    external_guarantor_id text                                               not null,

    email                 text,
    phone                 text,

    -- Tracks last update timestamp received from external source system.
    source_updated_at     timestamp with time zone                           not null,

    created_at            timestamp with time zone default now()             not null,
    updated_at            timestamp with time zone default now()             not null,

    constraint guarantors_unique_constraint
        unique (client_id, source_system_id, external_guarantor_id)
);

alter table billing.guarantors
    owner to billing;


-- ============================================================================
-- billing.dependents
-- ============================================================================
-- Purpose:
--     Represents dependents associated with guarantors.
--
-- Depends On:
--     billing.clients
--     billing.source_systems
--     billing.guarantors
-- ============================================================================

create table billing.dependents
(
    dependent_id          uuid                     default gen_random_uuid() not null
        constraint dependents_pk
            primary key,

    client_id             uuid                                               not null
        constraint dependents_clients_client_id_fk
            references billing.clients,

    source_system_id      uuid                                               not null
        constraint dependents_source_systems_source_system_id_fk
            references billing.source_systems,

    guarantor_id          uuid                                               not null
        constraint dependents_guarantors_guarantor_id_fk
            references billing.guarantors,

    external_dependent_id text                                               not null,

    first_name            text                                               not null,
    last_name             text                                               not null,

    date_of_birth         date,

    source_updated_at     timestamp with time zone                           not null,

    created_at            timestamp with time zone default now()             not null,
    updated_at            timestamp with time zone default now()             not null,

    constraint dependents_unique_constraint
        unique (client_id, source_system_id, external_dependent_id)
);

alter table billing.dependents
    owner to billing;


-- ============================================================================
-- billing.payments
-- ============================================================================
-- Purpose:
--     Represents canonical payment records synchronized from external systems.
--
-- Architectural Notes:
--     payment_status models business lifecycle state.
--     external_payment_id supports source-system reconciliation.
--     Composite uniqueness enables idempotent upsert workflows.
--
-- Depends On:
--     billing.clients
--     billing.locations
--     billing.source_systems
--     billing.guarantors
--     billing.dependents
-- ============================================================================

create table billing.payments
(
    payment_id          uuid                     default gen_random_uuid() not null
        primary key,

    location_id         uuid                                               not null
        constraint payments_locations_location_id_fk
            references billing.locations,

    client_id           uuid                                               not null
        constraint payments_clients_client_id_fk
            references billing.clients,

    payment_amount      numeric(12, 2)           default 0.00              not null,

    payment_date        timestamp with time zone                           not null,

    -- Example values:
    -- pending
    -- posted
    -- failed
    -- reversed
    payment_status      text                                               not null,

    created_at          timestamp with time zone default now()             not null,
    updated_at          timestamp with time zone default now()             not null,

    source_system_id    uuid                                               not null
        constraint payments_source_systems_source_system_id_fk
            references billing.source_systems,

    source_updated_at   timestamp with time zone                           not null,

    guarantor_id        uuid                                               not null
        constraint payments_guarantors_guarantor_id_fk
            references billing.guarantors,

    dependent_id        uuid                                               not null
        constraint payments_dependents_dependent_id_fk
            references billing.dependents,

    external_payment_id text                                               not null,

    constraint payments_unique_constraint
        unique (client_id, source_system_id, external_payment_id)
);

alter table billing.payments
    owner to billing;


-- ============================================================================
-- billing.ingestion_runs
-- ============================================================================
-- Purpose:
--     Tracks ingestion execution history.
--
-- Architectural Notes:
--     This table becomes the operational backbone for:
--
--         * observability
--         * retry handling
--         * reconciliation
--         * operational dashboards
--         * incremental synchronization
--         * ingestion auditing
--
-- Depends On:
--     billing.clients
--     billing.source_systems
-- ============================================================================

create table billing.ingestion_runs (
    ingestion_run_id uuid primary key default gen_random_uuid(),

    client_id uuid not null references billing.clients(client_id),
    source_system_id uuid not null references billing.source_systems(source_system_id),

    -- Example values:
    -- full_sync
    -- incremental_sync
    -- reconciliation
    run_type text not null,

    started_at timestamptz not null default now(),
    completed_at timestamptz,

    -- Example values:
    -- running
    -- completed
    -- failed
    -- partial
    status text not null,

    records_received integer default 0,
    records_inserted integer default 0,
    records_updated integer default 0,
    records_failed integer default 0,

    -- Supports cursor-based incremental synchronization.
    cursor_value text,

    error_message text
);

comment on table billing.ingestion_runs is
    'Tracks ingestion execution history for observability, reconciliation, retry workflows, and incremental synchronization.';

-- Operational indexes.
create index if not exists idx_ingestion_runs_client_id
    on billing.ingestion_runs (client_id);

create index if not exists idx_ingestion_runs_source_system_id
    on billing.ingestion_runs (source_system_id);

create index if not exists idx_ingestion_runs_status
    on billing.ingestion_runs (status);

create index if not exists idx_ingestion_runs_started_at
    on billing.ingestion_runs (started_at desc);

create index if not exists idx_ingestion_runs_client_source_started
    on billing.ingestion_runs (client_id, source_system_id, started_at desc);

alter table billing.ingestion_runs
    owner to billing;


-- ============================================================================
-- billing.ingestion_errors
-- ============================================================================
-- Purpose:
--     Stores failed ingestion records and operational ingestion issues.
--
-- Architectural Notes:
--     Supports replay/reprocessing workflows and operational debugging.
--     payload jsonb preserves original source payloads.
--
-- Depends On:
--     billing.ingestion_runs
-- ============================================================================

create table billing.ingestion_errors
(
    ingestion_error_id uuid                     default gen_random_uuid() not null
        primary key,

    ingestion_run_id   uuid
        references billing.ingestion_runs,

    entity_type        text                                               not null,
    external_id        text,

    error_code         text,
    error_message      text,

    -- Preserves original source payload for replay/debugging.
    payload            jsonb,

    created_at         timestamp with time zone default now()             not null
);

create index if not exists idx_ingestion_errors_ingestion_run_id
    on billing.ingestion_errors (ingestion_run_id);

create index if not exists idx_ingestion_errors_entity_type
    on billing.ingestion_errors (entity_type);

create index if not exists idx_ingestion_errors_created_at
    on billing.ingestion_errors (created_at desc);

alter table billing.ingestion_errors
    owner to billing;


-- ============================================================================
-- billing.source_sync_state
-- ============================================================================
-- Purpose:
--     Tracks synchronization checkpoint/cursor state per tenant/source.
--
-- Architectural Notes:
--     Critical for incremental synchronization workflows.
--
-- Depends On:
--     billing.clients
--     billing.source_systems
-- ============================================================================

create table billing.source_sync_state
(
    source_sync_state_id    uuid default gen_random_uuid() not null
        primary key,

    client_id               uuid                           not null
        references billing.clients,

    source_system_id        uuid                           not null
        references billing.source_systems,

    entity_type             text                           not null,

    last_successful_cursor  text,

    last_successful_sync_at timestamp with time zone,

    unique (client_id, source_system_id, entity_type)
);

create unique index if not exists ux_source_sync_state_client_source_entity
    on billing.source_sync_state (client_id, source_system_id, entity_type);

create index if not exists idx_source_sync_state_last_successful_sync_at
    on billing.source_sync_state (last_successful_sync_at desc);

alter table billing.source_sync_state
    owner to billing;


-- ============================================================================
-- billing.payment_events
-- ============================================================================
-- Purpose:
--     Stores immutable payment lifecycle events.
--
-- Architectural Notes:
--     payment_events enables auditability and event-history reconstruction.
--     This table complements current-state payment records.
--
-- Depends On:
--     billing.payments
-- ============================================================================

create table billing.payment_events
(
    payment_event_id uuid default gen_random_uuid() not null
        primary key,

    payment_id       uuid                           not null
        references billing.payments,

    -- Example values:
    -- created
    -- posted
    -- reversed
    -- refunded
    event_type       text                           not null,

    amount           numeric(12, 2),

    occurred_at      timestamp with time zone       not null,

    -- Preserves raw source-system event payloads.
    source_payload   jsonb
);

create index if not exists idx_payment_events_payment_id
    on billing.payment_events (payment_id);

create index if not exists idx_payment_events_event_type
    on billing.payment_events (event_type);

create index if not exists idx_payment_events_occurred_at
    on billing.payment_events (occurred_at desc);

create index if not exists idx_payment_events_payment_occurred
    on billing.payment_events (payment_id, occurred_at desc);

alter table billing.payment_events
    owner to billing;

-- ============================================================================
-- billing.location_source_mappings
-- ============================================================================
-- Purpose:
--     Maps external source-system location identifiers to canonical internal
--     platform locations.
--
-- Architectural Notes:
--     Supports ingestion workflows where external payment records reference
--     source-specific location IDs that must be resolved to internal location_id
--     values before canonical payment records can be inserted or updated.
--
--     This preserves the separation between external identity and canonical
--     internal identity, matching the same ingestion pattern used for clients,
--     guarantors, dependents, and payments.
--
-- Depends On:
--     billing.clients
--     billing.source_systems
--     billing.locations
-- ============================================================================
create table billing.location_source_mappings
(
    location_source_mapping_id uuid default gen_random_uuid() not null
        primary key,

    client_id uuid not null
        references billing.clients(client_id),

    source_system_id uuid not null
        references billing.source_systems(source_system_id),

    location_id uuid not null
        references billing.locations(location_id),

    external_location_id text not null,

    source_updated_at timestamp with time zone,

    created_at timestamp with time zone default now() not null,
    updated_at timestamp with time zone default now() not null,

    constraint location_source_mappings_unique_constraint
        unique (client_id, source_system_id, external_location_id)
);

comment on table billing.location_source_mappings is
    'Maps external source-system location identifiers to canonical internal platform locations for ingestion and reconciliation workflows.';

create index if not exists idx_location_source_mappings_client_id
    on billing.location_source_mappings(client_id);

create index if not exists idx_location_source_mappings_source_system_id
    on billing.location_source_mappings(source_system_id);

create index if not exists idx_location_source_mappings_location_id
    on billing.location_source_mappings(location_id);

alter table billing.location_source_mappings
    owner to billing;