# BillingTracker

BillingTracker is a backend/data-platform project focused on modeling the kinds of problems that appear in billing and ingestion systems: multi-tenant ownership, external source synchronization, payment lifecycle tracking, idempotent imports, operational reconciliation, and reporting-oriented relational design.

The project currently centers on the PostgreSQL schema and seed data. The next phase is moving from static schema design into ingestion behavior: incremental synchronization, idempotent upserts, reconciliation workflows, and operational tracking.

## Project Goals

BillingTracker is intended to explore backend/platform design concerns that show up in integration-heavy systems, especially in healthcare, fintech, billing, and data synchronization domains.

The main goals are to model and reason about:

- Multi-tenant relational design
- Canonical internal IDs vs. external source-system IDs
- Idempotent ingestion and retry-safe imports
- Incremental synchronization and cursor/checkpoint tracking
- Payment lifecycle modeling
- Current state vs. event history
- Operational ingestion tracking
- Error capture and replay-oriented debugging
- Reporting and reconciliation queries

## Current Tech Stack

- PostgreSQL
- SQL scripts
- DataGrip
- GitHub

Planned additions include:

- ASP.NET Core API
- Background ingestion worker
- Docker-based local environment
- Queue-based ingestion workflow
- Observability/logging concepts

## Current Repository Structure

```text
billingtracker/
├── sql/
│   ├── 00_create_schema.sql
│   ├── billing_create_tables.sql
│   ├── seed_data_inserts.sql
│   └── simple_queries.sql
└── README.md
```

The exact structure may evolve as the project moves from schema design into application and ingestion workflows.

## Schema Overview

The current schema includes core business tables and operational ingestion tables.

### Business/Core Tables

- `billing.clients`
- `billing.locations`
- `billing.source_systems`
- `billing.client_source_mappings`
- `billing.guarantors`
- `billing.dependents`
- `billing.payments`
- `billing.payment_events`

### Operational/Ingestion Tables

- `billing.ingestion_runs`
- `billing.ingestion_errors`
- `billing.source_sync_state`

## Key Design Concepts

### Multi-Tenant Boundaries

Most business records are associated with a `client_id`, which represents the tenant organization that owns the data.

This makes tenant ownership explicit in the schema and supports future reporting, filtering, ingestion, and authorization boundaries.

### Canonical Identity vs. External Identity

The schema separates internal canonical UUIDs from external source-system IDs.

For example, a payment has an internal `payment_id`, but also an `external_payment_id` and a `source_system_id`.

This supports scenarios where:

- Different vendors use overlapping external IDs
- The same tenant integrates with multiple source systems
- Imports need to be retried safely
- External records need to be mapped into canonical internal records

### Idempotent Imports

Several tables use composite unique constraints to support retry-safe ingestion.

Example:

```sql
unique (client_id, source_system_id, external_payment_id)
```

This allows ingestion logic to use PostgreSQL upsert patterns such as:

```sql
insert into billing.payments (...)
values (...)
on conflict (client_id, source_system_id, external_payment_id)
do update set ...;
```

The goal is to make repeated imports safe without creating duplicate canonical records.

### Current State vs. Event History

The `billing.payments` table represents the current known state of a payment.

The `billing.payment_events` table records payment lifecycle events over time, such as payment creation, posting, reversal, or refund activity.

This separation supports both operational queries and future audit/reconciliation workflows.

### Ingestion Run Tracking

The `billing.ingestion_runs` table tracks import/synchronization attempts by client and source system.

It is designed to answer operational questions such as:

- When did a tenant last sync from a source system?
- Did the run complete successfully?
- How many records were received, inserted, updated, or failed?
- What cursor/checkpoint was used?

### Ingestion Error Capture

The `billing.ingestion_errors` table captures failed records or operational ingestion issues.

It includes fields such as:

- entity type
- external ID
- error code
- error message
- source payload

The `payload` column is stored as `jsonb` so the original source data can be retained for debugging, replay, or reprocessing scenarios.

### Incremental Sync State

The `billing.source_sync_state` table tracks synchronization checkpoints per tenant, source system, and entity type.

This supports incremental sync strategies such as:

- `updated_since`
- cursor-based pagination
- checkpoint recovery
- entity-specific synchronization progress

## Example Query Areas

The seed data and query scripts are intended to support reporting and reconciliation scenarios such as:

- Payments for managed locations only
- Guarantor payment totals
- Failed or reversed payment analysis
- Latest sync run per tenant/source system
- Ingestion error review
- Source-system synchronization checkpoints
- Duplicate-prevention/idempotency checks

## Running the SQL Scripts

At this stage, the project is primarily SQL-based.

A typical setup flow is:

```sql
-- 1. Create schema and required extensions
\i sql/00_create_schema.sql

-- 2. Create tables and indexes
\i sql/billing_create_tables.sql

-- 3. Insert seed data
\i sql/seed_data_inserts.sql

-- 4. Run sample queries
\i sql/simple_queries.sql
```

The scripts can also be opened and run directly from a PostgreSQL IDE such as DataGrip.

## Current Status

The project is in its initial schema and relational modeling phase.

Completed so far:

- Billing schema setup
- Core tenant/source/payment tables
- Composite uniqueness rules for idempotent ingestion
- Operational ingestion tables
- Seed data for query and reporting scenarios
- Initial reporting/query scripts

Planned next areas:

- Idempotent upsert examples
- Simulated source-system payloads
- Incremental sync workflow
- Ingestion worker prototype
- Reconciliation queries
- API layer
- Docker-based local setup

## Design Philosophy

This project intentionally starts with the data model because billing and ingestion systems are heavily shaped by identity, ownership, source-system boundaries, retry behavior, and operational visibility.

The schema is expected to evolve as ingestion behavior becomes more concrete. The goal is not to present a finished billing platform, but to build a realistic backend foundation that can support platform-oriented design discussions and implementation work.

