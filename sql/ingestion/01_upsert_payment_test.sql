-- Demonstrates idempotent payment ingestion using:
--   client_id + source_system_id + external_payment_id
--
-- Scenarios tested:
--   1. first insert creates the payment
--   2. rerun with same payload does not create duplicate
--   3. newer source_updated_at updates the row
--   4. older source_updated_at does not overwrite newer data

-- Test sequence:
--
-- First run
-- 'DX-PAY-INGEST-0001'::text as external_payment_id
--
-- Expected: new payment inserted.
--
-- Second run with same exact payload
--
-- Same external_payment_id.
--
-- Expected: no duplicate payment. It should hit the conflict rule.
--
-- Third run with same external ID but newer source_updated_at and changed amount/status
-- 'DX-PAY-INGEST-0001'::text as external_payment_id,
-- 175.25::numeric(12,2) as payment_amount,
-- 'posted'::text as payment_status,
-- '2026-05-24T13:45:00Z'::timestamptz as source_updated_at
--
-- Expected: same payment row updates.
--
-- Fourth run with same external ID but older source_updated_at
-- 99.00::numeric(12,2) as payment_amount,
-- '2026-05-24T11:00:00Z'::timestamptz as source_updated_at
--
-- Expected: old payload does not overwrite the newer payment.
--
-- After each run, verify with:
-- ingestion/02_verify_payment_upsert.sql

-- select
--     payment_id,
--     external_payment_id,
--     payment_amount,
--     payment_status,
--     source_updated_at,
--     created_at,
--     updated_at
-- from billing.payments
-- where external_payment_id = 'DX-PAY-INGEST-0001';
--
-- And confirm no duplicates:
--
-- select
--     client_id,
--     source_system_id,
--     external_payment_id,
--     count(*)
-- from billing.payments
-- where external_payment_id = 'DX-PAY-INGEST-0001'
-- group by client_id, source_system_id, external_payment_id;
--
-- Expected count: 1.

with incoming_payment as (
    select
        'dentrix-client-100'::text as external_client_id,
        'Dentrix'::text as source_system_name,
        'DX-PAY-INGEST-0001'::text as external_payment_id,
        'G-DX-1001'::text as external_guarantor_id,
        'D-DX-1001'::text as external_dependent_id,
        'DX-LOC-MIDTOWN'::text as external_location_id,
        99.99::numeric(12,2) as payment_amount,
        '2026-05-24T12:30:00Z'::timestamptz as payment_date,
        'posted'::text as payment_status,
        '2026-05-24T11:45:00Z'::timestamptz as source_updated_at
),
     resolved_payment as (
         select
             c.client_id,
             ss.source_system_id,
             l.location_id,
             g.guarantor_id,
             d.dependent_id,
             ip.external_payment_id,
             ip.payment_amount,
             ip.payment_date,
             ip.payment_status,
             ip.source_updated_at
         from incoming_payment ip
                  join billing.source_systems ss
                       on ss.source_system_name = ip.source_system_name
                  join billing.client_source_mappings csm
                       on csm.source_system_id = ss.source_system_id
                           and csm.external_client_id = ip.external_client_id
                  join billing.clients c
                       on c.client_id = csm.client_id
                  join billing.guarantors g
                       on g.client_id = c.client_id
                           and g.source_system_id = ss.source_system_id
                           and g.external_guarantor_id = ip.external_guarantor_id
                  join billing.dependents d
                       on d.client_id = c.client_id
                           and d.source_system_id = ss.source_system_id
                           and d.external_dependent_id = ip.external_dependent_id
                  join billing.location_source_mappings lsm
                       on lsm.client_id = c.client_id
                           and lsm.source_system_id = ss.source_system_id
                           and lsm.external_location_id = ip.external_location_id
                  join billing.locations l
                       on l.location_id = lsm.location_id
     )
/*select *
from resolved_payment;*/

insert into billing.payments (
    client_id,
    location_id,
    source_system_id,
    guarantor_id,
    dependent_id,
    external_payment_id,
    payment_amount,
    payment_date,
    payment_status,
    source_updated_at
)
select
    rp.client_id,
    rp.location_id,
    rp.source_system_id,
    rp.guarantor_id,
    rp.dependent_id,
    rp.external_payment_id,
    rp.payment_amount,
    rp.payment_date,
    rp.payment_status,
    rp.source_updated_at
from resolved_payment rp
on conflict (client_id, source_system_id, external_payment_id)
    do update set
                  location_id = excluded.location_id,
                  guarantor_id = excluded.guarantor_id,
                  dependent_id = excluded.dependent_id,
                  payment_amount = excluded.payment_amount,
                  payment_date = excluded.payment_date,
                  payment_status = excluded.payment_status,
                  source_updated_at = excluded.source_updated_at,
                  updated_at = now()
where billing.payments.source_updated_at <= excluded.source_updated_at
returning
    payment_id,
    external_payment_id,
    payment_amount,
    payment_status,
    source_updated_at;