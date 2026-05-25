select
    payment_id,
    external_payment_id,
    payment_amount,
    payment_status,
    source_updated_at,
    created_at,
    updated_at
from billing.payments
where external_payment_id = 'DX-PAY-INGEST-0001';

select
    client_id,
    source_system_id,
    external_payment_id,
    count(*)
from billing.payments
where external_payment_id = 'DX-PAY-INGEST-0001'
group by client_id, source_system_id, external_payment_id;

-- Expected count: 1.
